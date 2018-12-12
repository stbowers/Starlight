# Starlight
Game written in C# 7, for CS 3020 final

Built for .NET Core 2.1

## Dependencies:
- VulkanCore (NuGet - Vulkan bindings for C#, https://github.com/discosultan/VulkanCore)
- GLFW (No NuGet package, just the shared library is needed and placed in ./lib64 - C#'s native code interface is used to call the native functions, https://www.glfw.org)

## Notes:
- The game will only run on Linux and Windows for now, since Vulkan doesn't work on macOS without a translation layer like MoltenVK, which doesn't work with the package I'm using for Vulkan bindings (VulkanCore).
- The game was built for .NET Core instead of for .NET Framework, for maximum compatibility since I was developing on Linux. There shouldn't be any problem with moving it over to .NET Framework 4.5 if you're having trouble compiling, but for best results you may need to download and install the .NET Core SDK
- The game looks for the following folders on startup (they are located in the root of this project, so you may need to move them to the bin directory):
```
./assets
./shaders
./lib64
```
- The StarlightServer project has all the server code, and should be run first. The StarlightGame project has the game code, and will need the folders listed above present in it's working directory when run.
- To start a new game, press the "Host Game" button on one instance of the game. It should connect to the server on localhost:5001, and give you a game ID to join. On another instance of the game, click on "Join Game" and then type into the console "join [gameID]" where [gameID] is the id that was shown on the host game screen of the other instance. You should see the game ID change on the second instance, after which you can press "Join Game", and then "Start Game" on the first instance.