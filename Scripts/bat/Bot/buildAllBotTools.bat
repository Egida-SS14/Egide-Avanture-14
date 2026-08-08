@echo off
cd ../../../

call git submodule update --init --recursive
call dotnet build -c Tools
call dotnet build Egide.Bot -c Tools

pause
