@echo off
cd ../../

call git submodule update --init --recursive
call dotnet build Content.Goobstation.Server -c Release
call dotnet build Egide.Bot -c Release

pause
