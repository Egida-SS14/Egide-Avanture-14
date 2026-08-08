
@echo off
cd ../../

call dotnet run --project Content.Packaging server --hybrid-acz --platform win-x64

pause