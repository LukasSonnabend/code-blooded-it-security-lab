# Code-Blooded Lab

A hands-on .NET security lab built around **Aviato**, a deliberately vulnerable web shop.
In groups you scan the app with Semgrep, let your coding agent fix the findings, and then
put the agent's work on trial: prove the fix works and read the diff to find what it missed.
Part 2 is a supply-chain attack through dependency injection.

> ⚠️ The app is intentionally insecure. Run it locally only or inside a container, and never deploy it.

## Start here

Open **`workshop.html`** in your browser. It is your companion for the whole session:
setup, timer, progress tracker, the brief for each OWASP category, and the Part 2 exercise.

## Getting set up

### Option A: dev container (recommended)

The dev container ships the .NET 10 SDK, the pinned Semgrep version and the VS Code
extensions the lab uses (C# Dev Kit, REST Client, Semgrep), plus the Claude Code CLI, so you
don't install anything else. Claude Code keeps its login in a volume, so you only sign in once,
even after a rebuild.

1. Install and start a container engine: [Docker Desktop](https://www.docker.com/products/docker-desktop/),
   or Podman (see below).
2. Install [VS Code](https://code.visualstudio.com/) and the
   [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
   extension.
3. Clone this repo and open the folder in VS Code.
4. Click **Reopen in Container** in the pop-up at the bottom right. If you miss it, press
   `F1` (or `Ctrl/Cmd+Shift+P`) and run **Dev Containers: Reopen in Container**.
5. Wait for the first build to finish. This takes a few minutes the first time, and later
   starts are fast. The packages are restored automatically.
6. The container then **starts the API for you** (Part 1) in its own terminal. Keep that
   terminal visible: it shows the API log you need for the A09 exercise.
7. Check it works: open `http://localhost:5218`, and in a new terminal
   (**Terminal → New Terminal**) run `dotnet --version` and `semgrep --version`.

Ports 5218 (API) and 5030 (Part 2 listener) are forwarded, so `http://localhost:5218`
works from your normal browser. To stop the API, press `Ctrl+C` in its terminal or close
that terminal (trash-can icon). If something breaks, run **Dev Containers: Rebuild Container**.

#### Which app the container starts

The container starts one app every time it starts. The last line of
`.devcontainer/active-launch` decides which one; the default is **Part 1**.

| Value | Starts | Port |
|-------|--------|------|
| `part1` | Aviato.API with the real payment service | 5218 |
| `part2` | Aviato.API with the malicious override | 5218 |
| `listener` | MaliciousListener, the Part 2 attacker | 5030 |
| `off` | Nothing | n/a |

To switch, do one of these:

- **Run the task:** Command Palette → **Tasks: Run Task** → **Start launch config**, then pick
  an entry. It stops the running app, starts your choice right away, and writes it to
  `active-launch`, so the next container start uses it too.
- **Edit the file:** change the last line of `active-launch` to a value from the table, then
  restart the container.

Part 1 and Part 2 share port 5218, so starting one stops the other. The listener can run next
to either. For Part 2, switch the container to `part2` and start the listener from
**Run and Debug**; starting it with the task would make the listener the container's app.

**Run and Debug** stops the container's copy of the app it is about to launch, so the port is
free. It does not change `active-launch`.

The task edits `active-launch`, which is tracked in git. Set it back to `part1` before you
commit, so your diff only shows the agent's fix.

**Using rootless Podman instead of Docker:** set `dev.containers.dockerPath` to `podman` in
the VS Code settings and pick **Aviato Security Lab (rootless Podman)** when you reopen in
the container (the default config leaves the workspace root-owned there, so `dotnet restore`
fails). The Podman machine must be rootless too: if
`podman machine inspect --format '{{.Rootful}}'` prints `true`, run
`podman machine stop && podman machine set --rootful=false && podman machine start` and
rebuild the container, otherwise the restore fails with "Permission denied".

### Option B: local install

Install the .NET 10 SDK, Semgrep, and VS Code with the C# Dev Kit and REST Client
extensions, then start the API yourself:

```bash
dotnet run --project Aviato.API          # API on http://localhost:5218
```

### Scan

In either setup:

```bash
semgrep --config semgrep-rules/ Aviato.API/ Aviato.Infrastructure/
```

The database is re-seeded on every start. If a destructive test wrecks the data, click
**Reset application** in the sidebar of `workshop.html` (or restart the API).

> Start the API from a shell **without personal API keys or secrets exported**. One of the
> vulnerable endpoints dumps environment variables.

## What's in the repo

| Path | What it is |
|------|------------|
| `workshop.html` | The student companion page |
| `Aviato.API/`, `Aviato.Core/`, `Aviato.Infrastructure/` | The vulnerable app |
| `Aviato.API/http/` | Runnable attack requests (VS Code REST Client), one file per category |
| `semgrep-rules/` | Custom Semgrep rules, one file per category |
| `Aviato.Dependencies/` | Part 2: a "harmless" dependency |
| `MaliciousListener/` | Part 2: the attacker's server |
| `.devcontainer/` | .NET 10, pinned Semgrep and Claude Code |
| `.vscode/` | Launch profiles for Part 1 and Part 2 |

Part 2 is off by default. Your presenter will tell you when to switch it on; the steps are in
`workshop.html`.
