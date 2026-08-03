
@echo off
cd ../../

call dotnet run --project Content.Packaging server --hybrid-acz --platform win-x64
call dotnet publish Egide.Bot -c Release -o release\bot

rem Content.Packaging always recreates release\SS14.Server_win-x64.zip from scratch,
rem so pack the bot and the server-only config (server_config.toml) on top of it.
powershell -NoProfile -ExecutionPolicy Bypass -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip=[System.IO.Compression.ZipFile]::Open((Resolve-Path 'release\SS14.Server_win-x64.zip').Path,'Update'); try { $botDir=(Resolve-Path 'release\bot').Path; Get-ChildItem -Recurse -File 'release\bot' | ForEach-Object { $rel=$_.FullName.Substring($botDir.Length+1).Replace('\','/'); [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$_.FullName,'bot/'+$rel) | Out-Null }; [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,'bin\Content.Server\server_config.toml','server_config.toml') | Out-Null } finally { $zip.Dispose() }"

pause
