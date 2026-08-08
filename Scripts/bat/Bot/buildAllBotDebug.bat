@echo off
cd ../../../

call git submodule update --init --recursive
call dotnet build -c Debug
call dotnet build Egide.Bot -c Debug

pause
