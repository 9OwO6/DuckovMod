# BetterThrowingSystem

A quality-of-life gameplay mod for **Escape from Duckov** that improves throwable item handling with faster access, smoother switching, configurable behaviors, and expanded utility features.

## Overview

BetterThrowingSystem is a custom mod built for *Escape from Duckov* to make throwable item usage more efficient and intuitive during gameplay.

The original goal of the mod was to improve grenade and consumable access by introducing a dedicated throwable workflow. Over time, the project evolved into a broader gameplay enhancement mod with configurable hotkey behaviors, improved inventory logic, mod settings support, and a simple radial selector for throwable items.

This project was developed and iteratively refined based on real player feedback, testing limitations, and community requests.

## Features

### Core Features
- **Throwable inventory support**  
  Allows players to carry and manage up to 5 throwable items more conveniently.

- **Fast switching with hotkeys**  
  Press **G** to quickly switch to throwable items from equipped gear.

- **Automatic scanning**  
  Automatically detects throwable items and food items from the player inventory.

### Newer Updates
- **Mod Settings UI support**  
  Added an in-game settings interface through a separate Mod Settings dependency.

- **Two throw modes**
  - **Press G to Equip**: original/default behavior
  - **Press G to Throw**: directly throws toward the mouse target area

- **Auto switch-back after throw**  
  Improves flow by automatically returning to the weapon after throwing.

- **Throwable stacking**  
  Enables stacking for throwable items to reduce inventory pressure.

- **Faster throw charge option**  
  Optional setting to speed up throwable charge time.

- **Improved item detection logic**  
  Refined recognition rules and excluded incorrectly detected items.

- **Simple throwable radial menu**  
  Added an early radial selector implementation for throwable selection.

## Why I Built This

I wanted to improve the feel of throwable usage in *Escape from Duckov*. The default workflow felt restrictive during active gameplay, especially when switching between weapons and throwable items.

The mod started as a focused improvement for throwable access, but later expanded after community feedback showed demand for:
- direct throw behavior
- configurable settings
- smoother weapon/throwable transitions
- better inventory handling

## Development Notes

This mod was built in **C#** and designed around the game's modding workflow and available APIs. Some systems required practical adaptation and ongoing adjustment based on in-game item types, inventory behavior, and compatibility constraints.

### Build Setup
Set the `DuckovPath` variable in `BetterThrowingSystem.csproj` to point to the game installation directory.

You can configure it by:
- editing project properties in Visual Studio and adding a user variable
- setting an environment variable before build
- hardcoding the path in the `.csproj` file (not recommended for release)

### Build Command
```bash
dotnet build BetterThrowingSystem.csproj
