# .NET 10 SDK on Debian, maintained by the Dev Containers team.
FROM mcr.microsoft.com/devcontainers/dotnet:2-10.0

# Semgrep is the lab's static-analysis step (see workshop.html). Installed
# with pipx into its own venv so it never collides with the system Python
# (Debian marks that one externally managed — a plain `pip install` fails).
ENV PIPX_HOME=/usr/local/pipx \
    PIPX_BIN_DIR=/usr/local/bin

# The base image's Yarn apt source is signed by /usr/share/keyrings/yarn-archive-keyring.gpg,
# whose signing subkeys expired in January 2026. Yarn now signs with 62D54FD4003F6525,
# so apt-get update fails with NO_PUBKEY until that keyring is refreshed. Fetch Yarn's
# current pubkey at build time so a later rotation is picked up on rebuild.
# https://github.com/devcontainers/images/issues/1752
RUN curl -fsSL https://dl.yarnpkg.com/debian/pubkey.gpg \
        | gpg --batch --yes --dearmor -o /usr/share/keyrings/yarn-archive-keyring.gpg \
    && apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends pipx \
    && rm -rf /var/lib/apt/lists/* \
    # Pinned so every attendee's scan produces the same findings as
    # everyone else in the room. Bump deliberately, then re-run the scan.
    && pipx install semgrep==1.178.0

# Mount point for the claude-code-config volume (see devcontainer.json). A new
# named volume copies this directory's owner, so it must already belong to
# vscode, or Claude Code can't write its login and settings there.
RUN mkdir -p /home/vscode/.claude && chown vscode:vscode /home/vscode/.claude
