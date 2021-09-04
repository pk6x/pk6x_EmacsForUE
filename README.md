# Emacs Source Code Accessor for Unreal Editor

## Synopsis

**Emacs Source Code Accessor** is a plug-in for Unreal Editor that adds Emacs to the list of source code editors.

This is a **work-in-progress** project.

### Installation

1. Clone the repository to the `Plugins` directory of your Unreal Engine project (create the directory if it does not exist). Make sure you have no Unreal Editor opened.
2. Open the project in the Unreal Editor.
3. The Editor will ask you to rebuild your project modules. Agree with the request.
4. Restart Unreal Editor.
5. The Editor should have 'Emacs' item in the list of the source code editors now.
6. Select 'Emacs' and restart the Editor for the last time.
7. Done.

#### Optional Patch for Unreal Build Tool

This section is for those who use [lsp](https://emacs-lsp.github.io/lsp-mode/), [rtags](https://github.com/Andersbakken/rtags), or any other autocompletion package that requires a Clang compilation database.

You can apply a patch to the UnrealBuildTool source code which generates a Clang compilation database (`compile_commands.json`) in the project root each time Unreal Editor refreshes the project.

1. Download the [patch](Source/UBT/UBT_UnrealEmacs.diff) to the Engine root directory.
2. Change directory to the Engine root directory.
3. Apply the patch: `patch -p1 < UBT_UnrealEmacs.diff`
4. Rebuild the UnrealBuildTool.

##### Rebuilding UnrealBuildTool on GNU/Linux

```shell
$ source Engine/Build/BatchFiles/Linux/SetupEnvironment.sh 
$ xbuild Engine/Source/Programs/UnrealBuildTool/UnrealBuildTool.csproj
```

##### Rebuilding UnrealBuildTool on macOS

```shell
$ bash
bash-3.2$ source Engine/Build/BatchFiles/Mac/SetupEnvironment.sh 
bash-3.2$ xbuild Engine/Source/Programs/UnrealBuildTool/UnrealBuildTool.csproj
bash-3.2$ exit
```

##### Rebuilding UnrealBuildTool on Windows

Go to the Start menu and enter "dev" or "developer command prompt".
This should bring up a list of installed apps that match your search pattern.
Select "Developer Command Prompt".
If it is not there, then you can find it manually at `<visualstudio installation folder>\<version>\Common7\Tools\LaunchDevCmd.bat`.
You also must have the .NET development tools and .NET framework target packs installed.

Issue the following command:

```shell
msbuild Engine/Source/Programs/UnrealBuildTool/UnrealBuildTool.csproj
```

### Screenshots

<details><summary>Editor Preferences</summary>
![Editor Preferences/General/Source Code](PlugInScreenShots/editor-preferences-general-source-code.png "Editor Preferences")
</details>
<details><summary>Menu File/Open Emacs</summary>
![Menu File/Open Emacs](PlugInScreenShots/menu-file-open-in-emacs.png "Menu File/Open Emacs")
</details>
<details><summary>Emacs Integration enabled in the Editor Plugins</summary>
![Emacs Integration enabled in the Editor Plugins](PlugInScreenShots/list-of-plug-ins.png "Emacs Integration enabled in the Editor Plugins")
</details>

### Usage

At the moment, I tested the plug-in on macOS only. It has no platform-specific code except of the hardcoded path to the
`emacs` and `emacsclient` commands. I plan making those configurable and also test them on GNU/Linux and Windows.

Meanwhile, you can change the path to these commands directly in the plug-in source code as a workaround if defaults
don't work for you.

#### Emacs Server

To have the best experience, start an [Emacs server](https://www.gnu.org/software/emacs/manual/html_node/emacs/Emacs-Server.html).
The plug-in will use `emacsclient` when the server is available and open source code files in the server's frame.
Otherwise, the plug-in fallbacks to `emacs` and uses a new instance of the Emacs each time Unreal Engine asks it to open
a source file.

## Projectile Unreal

You may want to try another work-in-progress project of mine: [Projectile Unreal](https://gitlab.com/manenko/projectile-unreal)
which is a minor mode for working with Unreal Engine projects in GNU Emacs.

## License

MIT
