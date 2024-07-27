# Project-W

## monogame-vscode-boilerplate

Use the MonoGame C# configuration for Visual Studio Code
See https://github.com/rewrking/monogame-vscode-boilerplate for instructions and downloads

## Setting Up Mac Environment:
- get both x64 and ARM .net (not optional)
- follow this closely, you need all of the vs code extensions (on vscode):
  - https://docs.monogame.net/articles/getting_started/1_setting_up_your_development_environment_unix.html
- brew install freetype
- brew install freeimage

You need to make 2 files for the the 2 previous commands
- for freetype on mac, first change: 
  - /opt/homebrew/lib/libfreetype.6.dylib -> 
  - /opt/homebrew/lib/libfreetype6.dylib
  - then run
  - sudo ln -s /opt/homebrew/lib/libfreetype6.dylib /usr/local/lib/libfreetype6
  - or
  - sudo ln -s /opt/homebrew/Cellar/freetype/2.13.2/lib/libfreetype.6.dylib /usr/local/lib/libfreetype6
  - based on location
- for freeimage on mac, run:
  - sudo ln -s /opt/homebrew/Cellar/freeimage/3.18.0/lib/libfreeimage.dylib /usr/local/lib/libfreeimage
- command to get editor open:
  - dotnet mgcb-editor ./Content/Content.mgcb (doesn't seem to be working rn with arm over x64)
- use content.mgcb, right click, and click in mgcb editor
- launch game (for now) by right clicking project, then go to debug, then start a new instance in solution explorer

## Steamworks Installation:
**All Platforms**
  - Run command: dotnet add package Steamworks.NET --version 20.2.0
  - Download Steamworks.NET Standalone Zip from https://github.com/rlabrecque/Steamworks.NET/releases
  - Copy Steamworks.NET.dll to output folder (root/bin/Debug/net6.0)
  - Create file there called steam_appid.txt with 480 in the file. (480 is Spacewar game id that is used for testing) 
**FOR Windows**
 - Copy steam_api.dll/steam_api64.dll to output folder
**FOR MAC**
  - Copy steam_api.bundle to output folder
  - Copy Steamworks.NET.dll.config to output folder

Documentation for how to use Steamworks.NET found here: https://steamworks.github.io/

## Project Structure

The Monogame repository follows a specific folder structure to organize its components and resources. Here is an overview of the main folders:

### Components
The `Components` folder contains two subfolders:
- `UI`: This folder holds all the user interface components.
- `Entities`: This folder contains the game entity components.

### Debugging
The `Debugging` folder is dedicated to in-game console support and FPS status.

### Engine
The `Engine` folder is responsible for handling mechanics, physics, rules, and utilities.

### Game State
The `GameState` folder stores all the game state components.

### Graphics
The `Graphics` folder is used for defining sprite sheets and text elements.

### Managers
The `Managers` folder in the Monogame repository contains various classes responsible for managing different aspects of the game. Here is an overview of the classes present in the `Managers` folder:

- `DrawManager`: This class handles the drawing and rendering of game objects and graphics on the screen.

- `GamePadManager`: This class provides functionality for handling input from gamepad controllers.

- `GameStateManager`: This class manages the different states of the game, such as the main menu, gameplay, pause menu, etc.

- `KeyboardManager`: This class handles keyboard input and provides methods for detecting key presses and releases.

- `MouseManager`: This class manages mouse input and provides methods for tracking mouse movement and button clicks.

- `SceneManager`: This class is responsible for managing different game scenes, such as the main menu, level selection, etc.

- `SettingsManager`: This class is responsible for managing and handling in-game settings, such as graphics options, audio settings, and control configurations.

- `SoundManager`: This class handles the playback of sound effects and background music in the game.

- `SteamworksManager`: This class provides integration with the Steamworks API for features like achievements, leaderboards, and multiplayer.

- `TextManager`: This class handles the rendering and display of text elements in the game.

- `UIManager`: This class manages the user interface components, such as buttons, menus, and HUD elements.

### Scenes
The `Scenes` folder contains all the individual game scenes.

### Settings
The `Settings` folder is responsible for managing in-game settings.