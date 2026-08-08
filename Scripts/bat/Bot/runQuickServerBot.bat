@echo off
cd ../../../

start "Egide.Bot" /D "Egide.Bot" dotnet run --no-build
call dotnet run --project Content.Server --no-build %*

pause
