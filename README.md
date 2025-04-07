# SleddersTeleporter
Teleporter mod for Sledders. Jump anywhere in the level instantly using the game's built in map, or a text based coordinate input.

The same teleporter is also featured in Ori0n1's Sled Tuner: ![SledTuner](https://github.com/0ri0n1/SledTuner2.0-with-GUI/tree/master "SledTuner")

This repo is to serve the standalone updates to the teleporter originally developed by me.

The mod can be freely used and updated as you wish. All I ask is that you credit me in some way :)

## Installation
Download and install Melonloader for sledders: ![MelonLoader](https://melonwiki.xyz/ "melonwiki.xyz")

Download the SleddersTeleporter.dll and place it in your mods folder (Ex. C:\Program Files (x86)\Steam\steamapps\common\Sledders\Mods)

## Riding Behavior
Toggles coordinate menu. You can view you current position and type in new coordinates, then click teleport
![Riding View](TeleporterRiding.png?raw=true "Teleporter Riding UI")
## Map View Behavior
Open the game's map, then use the teleport keybind to instantly teleport to your cursor's location on the map
![Map View](TeleporterMap.png?raw=true "Teleporter Map UI")
## Default controls
### Keyboard
* T

### Controller
* Left Stick (click)

## Updating controls
On first launch, the mod will create a TeleporterControls.cfg file in your mods directory with the following contents:
```
keyboard=T
controller=JoystickButton8
```

Open this file in a text editor (Notepad, for example) and replace the values with your desired control.

![ControllerLayout](unityControllerLayout.jpeg?raw=true "Unity Controller Layout")

For controller input, use this diagram to determine the correct value for your button. Right now, only buttons are supported. Anything with an "axis" is unsupported as an input.

For example, clicking the left stick is mapped to JoystickButton8. If you would like to change it to right bumper, you would use JoystickButton5
