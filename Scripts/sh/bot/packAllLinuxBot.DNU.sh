#!/usr/bin/env bash
set -e

cd ..
cd ..

git submodule update --init --recursive

dotnet build -c Release

dotnet run --project Content.Packaging server --hybrid-acz --platform linux-x64

dotnet publish Egide.Bot -c Release -o release/bot

# Content.Packaging always recreates the server zip, so there are no stale
# entries to remove: add the bot and the server-only config (server_config.toml).
if command -v python3 >/dev/null 2>&1; then
    python3 - "$(pwd)/release" "$(pwd)/bin/Content.Server/server_config.toml" <<'PYEOF'
import os
import sys
import zipfile

release_dir = sys.argv[1]
config_path = sys.argv[2]
zip_path = os.path.join(release_dir, "SS14.Server_linux-x64.zip")
bot_dir = os.path.join(release_dir, "bot")

with zipfile.ZipFile(zip_path, "a") as zip_file:
    for root, _, files in os.walk(bot_dir):
        for name in files:
            full = os.path.join(root, name)
            rel = os.path.relpath(full, bot_dir).replace(os.sep, "/")
            zip_file.write(full, "bot/" + rel)
    zip_file.write(config_path, "server_config.toml")
PYEOF
else
    echo "Warning: python3 not found, Egide.Bot and server_config.toml were not added to the server zip"
fi
