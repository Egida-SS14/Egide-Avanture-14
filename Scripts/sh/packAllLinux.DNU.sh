#!/usr/bin/env bash
set -e

cd ..
cd ..

git submodule update --init --recursive

dotnet build -c Release

dotnet run --project Content.Packaging server --hybrid-acz --platform linux
