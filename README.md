# Emacs Source Code Accessor for Unreal Editor

## Synopsis

**Emacs Source Code Accessor** is a plug-in for Unreal Editor that adds Emacs to the list of source code editors.

This is a **work-in-progress** project.

### Installation

1. Clone the repository to the `Plugins` directory of your Unreal Engine project (create the directory if it does not exist). Make sure you have no Unreal Editor opened.
2. Open the project in the Unreal Editor.
3. The Editor will ask you to rebuild your project modules. Agree with the request.
4. The Editor should have `Emacs` item in the list of the source code editors.
5. Select `Emacs`. Optionally, set it as default.
6. Restart the Editor.
7. Done.

#### Optional Patch for Unreal Build Tool

This section is for those who use [lsp](https://emacs-lsp.github.io/lsp-mode/), [rtags](https://github.com/Andersbakken/rtags), or any other autocompletion package that requires a Clang compilation database.

You can apply a patch to the UnrealBuildTool source code which generates a Clang compilation database (`compile_commands.json`) in the project root each time Unreal Editor refreshes the project.

1. Download the [patch](Source/UBT/UBT_UnrealEmacs.diff) to the Engine root directory.
2. Change directory to the Engine root directory.
3. Apply the patch: `patch -p1 < UBT_UnrealEmacs.diff`
4. Rebuild the UnrealBuildTool.

##### Rebuilding UnrealBuildTool on GNU/Linux

Make sure the source files are writeable.
You may need to change access permissions to the Engine root directory and its contents recursively.

Issue the following commands:

```shell
$ source Engine/Build/BatchFiles/Linux/SetupEnvironment.sh 
$ xbuild Engine/Source/Programs/UnrealBuildTool/UnrealBuildTool.csproj
```

##### Rebuilding UnrealBuildTool on macOS

Make sure the source files are writeable.
You may need to change access permissions to the Engine root directory and its contents recursively.

Issue the following commands:

```shell
$ bash
bash-3.2$ source Engine/Build/BatchFiles/Mac/SetupEnvironment.sh 
bash-3.2$ xbuild Engine/Source/Programs/UnrealBuildTool/UnrealBuildTool.csproj
bash-3.2$ exit
```

##### Rebuilding UnrealBuildTool on Windows

Make sure the source files are writeable. You may need to clear 'Read-only' flag
on the Engine root directory and its content recursively.

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

In the Unreal Editor:

- Use `File/Open Emacs` to open the project in Emacs.
- Use `File/New C++ Class...` to create a new C++ class and open its files in Emacs.
- Use `File/Refresh Emacs Project` to regenerate the Clang compilation database. Use should patch the UnrealBuildTool as described in this document to have access to this feature.

The plug-in uses `emacsclient` (`emacsclientw` on Windows) to open source code files.
If there is no [Emacs server](https://www.gnu.org/software/emacs/manual/html_node/emacs/Emacs-Server.html) running, it starts a server and then open the files.

#### Specifying Emacs Client Location

The plug-in should work without any additional configuration on GNU/Linux, macOS, and Windows.
By default it searches for Emacs client program in the following locations:

- GNU/Linux: `/usr/local/bin/emacsclient`.
- macOS: `/usr/local/bin/emacsclient`.
- Windows: `C:/Program Files/Emacs/x86_64/bin/emacsclientw.exe`.

If you have Emacs installed in a different location, then set `UNREAL_EMACS_EMACSCLIENT_PATH` environment variable to a full path to the Emacs client command.

## Projectile Unreal

You may want to try another work-in-progress project of mine: [Projectile Unreal](https://gitlab.com/manenko/projectile-unreal)
which is a minor mode for working with Unreal Engine projects in GNU Emacs.

## License

MIT
