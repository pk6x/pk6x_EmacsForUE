# Emacs Source Code Accessor for Unreal Editor

## Synopsis

**Emacs Source Code Accessor** is a plug-in for Unreal Editor that adds Emacs to the list of source code editors.

This is a **work-in-progress** project.

### Installation

1. Clone the repository to the `Plugins` directory of your Unreal Engine project (create the directory if it does not exist). Make sure you have no Unreal Editor opened.
2. Open the project in the Unreal Editor.
3. The Editor will ask you to rebuild your project modules. Allow it to rebuild it.
4. Restart Unreal Editor.
5. Now the Editor should have 'Emacs' item in the list of the source code editors.
6. Select 'Emacs' and restart the Editor for the last time.
7. Done.

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
