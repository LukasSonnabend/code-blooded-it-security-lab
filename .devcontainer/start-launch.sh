#!/bin/bash
# Start the launch configuration named in .devcontainer/active-launch.
# The names match .vscode/launch.json; each one runs the same
# `dotnet run --launch-profile` that configuration would debug.
#
# Part 1 and Part 2 both bind :5218, so starting one stops the other.
# The listener binds :5030 and can stay running next to either API.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
STATE_DIR="${XDG_CACHE_HOME:-$HOME/.cache}/aviato-launch"
mkdir -p "$STATE_DIR"

# Remember an explicit choice (task prompt or AVIATO_LAUNCH) so the next
# container start uses it. Comment lines in the file are left in place.
persist_selection() {
  local value="$1"
  local file="$ROOT/.devcontainer/active-launch"
  local tmp
  tmp="$(mktemp)"
  if [[ -f "$file" ]]; then
    awk -v v="$value" '
      /^[[:space:]]*#/ || /^[[:space:]]*$/ { print; next }
      !done { print v; done = 1; next }
      { next }
    ' "$file" > "$tmp"
  fi
  if ! grep -qvE '^[[:space:]]*(#|$)' "$tmp"; then
    printf '%s\n' "$value" >> "$tmp"
  fi
  mv "$tmp" "$file"
}

read_selection() {
  if [[ -n "${1:-}" ]]; then
    printf '%s' "$1"
    return
  fi
  if [[ -n "${AVIATO_LAUNCH:-}" ]]; then
    printf '%s' "$AVIATO_LAUNCH"
    return
  fi

  local file="$ROOT/.devcontainer/active-launch"
  if [[ ! -f "$file" ]]; then
    printf '%s' "part1"
    return
  fi

  local line
  line="$(grep -vE '^[[:space:]]*(#|$)' "$file" | head -n 1 | tr -d '\r' || true)"
  # Trim surrounding whitespace.
  line="${line#"${line%%[![:space:]]*}"}"
  line="${line%"${line##*[![:space:]]}"}"
  if [[ -z "$line" ]]; then
    line="part1"
  fi
  printf '%s' "$line"
}

# Stop the process group led by $1: TERM first, KILL if it is still there
# after three seconds. The pid is the group leader (set -m), so this also
# stops the app that `dotnet run` spawns.
terminate_group() {
  local pid="$1"
  [[ -n "$pid" ]] || return 0
  if ! kill -0 "$pid" 2>/dev/null; then
    return 0
  fi

  kill -TERM -- "-$pid" 2>/dev/null || kill -TERM "$pid" 2>/dev/null || true
  local _
  for _ in 1 2 3 4 5 6 7 8 9 10; do
    kill -0 "$pid" 2>/dev/null || return 0
    sleep 0.3
  done
  kill -KILL -- "-$pid" 2>/dev/null || kill -KILL "$pid" 2>/dev/null || true
}

stop_role() {
  local role="$1"
  local pidfile="$STATE_DIR/$role.pid"
  [[ -f "$pidfile" ]] || return 0

  local pid
  pid="$(cat "$pidfile")"
  rm -f "$pidfile"
  terminate_group "$pid"
}

# The app runs in its own process group, so closing the terminal (SIGHUP) or
# pressing Ctrl+C only reaches this script. Take the app down with it, or it
# keeps :5218 and Run and Debug cannot bind the port. Only our own child is
# stopped: a newer start-launch.sh may already own the pid file.
CHILD_PID=""
CHILD_PIDFILE=""
cleanup() {
  local pid="$CHILD_PID"
  CHILD_PID=""
  [[ -n "$pid" ]] || return 0
  terminate_group "$pid"
  if [[ -f "$CHILD_PIDFILE" && "$(cat "$CHILD_PIDFILE")" == "$pid" ]]; then
    rm -f "$CHILD_PIDFILE"
  fi
}
trap cleanup EXIT
trap 'exit 129' HUP
trap 'exit 130' INT
trap 'exit 143' TERM

start_role() {
  local role="$1"
  local project="$2"
  local profile="$3"
  local label="$4"

  # Loop so the workshop "Reset application" button can restart this process.
  # The API writes $STATE_DIR/$role.restart and exits; we start the same
  # launch profile again, which re-seeds the in-memory database.
  while true; do
    exec 9>"$STATE_DIR/lock"
    flock 9
    stop_role "$role"
    rm -f "$STATE_DIR/$role.restart"

    echo "Starting ${label}"
    echo "  dotnet run --project ${project} --launch-profile ${profile}"

    set -m
    (
      cd "$ROOT/$project"
      export AVIATO_SUPERVISED=1
      export AVIATO_ROLE="$role"
      export AVIATO_STATE_DIR="$STATE_DIR"
      exec dotnet run --launch-profile "$profile" --project "$ROOT/$project"
    ) &
    local pid=$!
    CHILD_PID="$pid"
    CHILD_PIDFILE="$STATE_DIR/$role.pid"
    echo "$pid" > "$CHILD_PIDFILE"
    flock -u 9
    exec 9>&-

    wait "$pid" || true
    CHILD_PID=""

    if [[ -f "$STATE_DIR/$role.restart" ]]; then
      echo "Reset requested — restarting ${label}"
      continue
    fi

    echo "${label} exited."
    break
  done
}

# `--stop api|listener` frees that role's port once, without changing
# .devcontainer/active-launch. Run and Debug calls it before launching: the
# app the container started outlives its terminal, because closing that
# terminal only ends the exec client on the host. Does nothing when no
# supervised app is running, which is also the case outside the container.
if [[ "${1:-}" == "--stop" ]]; then
  role="${2:?usage: start-launch.sh --stop api|listener}"
  [[ -f "$STATE_DIR/$role.pid" ]] || exit 0
  exec 9>"$STATE_DIR/lock"
  flock 9
  stop_role "$role"
  echo "Stopped the dev-container $role process so the debugger can bind its port."
  exit 0
fi

if [[ -n "${1:-}" || -n "${AVIATO_LAUNCH:-}" ]]; then
  persist_selection "${1:-${AVIATO_LAUNCH}}"
fi
selection="$(read_selection "${1:-}")"

case "$selection" in
  part1 | "Aviato.API · Part 1 (real payment service)")
    start_role api Aviato.API http "Aviato.API · Part 1 (real payment service)"
    ;;
  part2 | "Aviato.API · Part 2 (malicious override)")
    start_role api Aviato.API http-part2 "Aviato.API · Part 2 (malicious override)"
    ;;
  listener | "MaliciousListener (Part 2 attacker)")
    start_role listener MaliciousListener http "MaliciousListener (Part 2 attacker)"
    ;;
  off | none | stop)
    exec 9>"$STATE_DIR/lock"
    flock 9
    stop_role api
    stop_role listener
    echo "Stopped dev-container launch processes."
    ;;
  *)
    echo "Unknown launch config: ${selection}" >&2
    echo "Set .devcontainer/active-launch to one of:" >&2
    echo "  part1      Aviato.API · Part 1 (real payment service)" >&2
    echo "  part2      Aviato.API · Part 2 (malicious override)" >&2
    echo "  listener   MaliciousListener (Part 2 attacker)" >&2
    echo "  off        start nothing" >&2
    exit 1
    ;;
esac
