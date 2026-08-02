#!/usr/bin/env bash
set -e

if [ "$(dirname $0)" != "." ]; then
    cd "$(dirname $0)"
fi

cd ../../

git submodule update --init --recursive

dotnet build -c Release

dotnet run --project Content.Packaging server --hybrid-acz --platform linux
