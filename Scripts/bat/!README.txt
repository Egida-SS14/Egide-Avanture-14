buildAllDebug
    Builds all projects with debug configuration
buildAllRelease
    Builds all projects with release configuration
buildAllTools
    Builds all projects with tools configuration
buildServerRelease
    Builds the server (Content.Goobstation.Server) in release configuration without the bot
PackAllWinBot
    Packages the server for win-x64, publishes Egide.Bot into release\bot and adds it to the server zip
    under bot\, plus the server-only config (server_config.toml)

The debug vs release build is simply what people develop in vs the actual server.
The release build contains various optimizations, while the debug build contains debugging tools.
If you're mapping, use the release or tools build as it will run smoother with less crashes.


runQuickAll
    Runs the client and server without building
runQuickClient
    Runs the client without building
runQuickServer
    Runs the server without building

runTests
    Runs the unit tests, makes sure various C# systems work as intended
runTestsIntegration
    Runs the integration tests, makes sure various C# systems work as intended
runTestsYAML
    Runs the YAML linter and finds issues with the YAML files that you probably wouldn't otherwise

Scripts for building the assembly with a bot are located in the bot/ directory

------!!!------
DO NOT USE PackAllWin!!!
------!!!------
