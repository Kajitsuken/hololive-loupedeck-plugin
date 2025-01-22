# Hololive Loupedeck Plugin

A Loupedeck plugin that provides quick access to live and upcoming Hololive streams through a dynamic folder interface.

## Features

- Shows both live and upcoming (up to 1h) Hololive streams in a dynamic folder
- Opens the respective stream on Youtube when the button is pressed
- Streams are sorted by when they started, having newer streams appear at the top
- Buttons use the Youtube profile pictures of the talents, which are automatically downloaded
- Upcoming streams appear as greyscale images
- Auto-refreshes stream data every minute while folder is open
- Configurable settings to hide Holostars and guest appearance streams

## Compatibility

The plugin is only compatible with Windows OS (tested on Windows 10).
I've only tested this plugin on a Loupedeck Live, as I don't own any of the other devices.
As such, I have no idea if this will work on any of their other devices.

## Installation

1. Download the ZIP archive from the latest release
2. Extract the archive
3. Move the HoloPlugin folder to `%LOCALAPPDATA%\Logi\LogiPluginService\Plugins`
4. Move the `config.json` file into the plugin's data directory at `%LOCALAPPDATA%\Logi\LogiPluginService\PluginData\Hololive` (create the folder if not already present)
5. Log into [Holodex](https://holodex.net/), go to your account settings and generate an API Key
6. Replace the `api-key` placeholder in `config.json` with your generated Holodex API key
7. Adjust the other values to your liking. Possible values are always either `true` or `false`.
8. Restart your Loupedeck
9. If you can't see the plugin in the list of available ones, go to _Show and hide plugins_ and enable it there
10. Assign the folder to any button you like

## Disclaimer

Given that this is the only C# project I've ever worked on, I've probably violated various coding style guidelines for the language.
Please have mercy (or open a PR to improve whatever mess I made).

I would like to also put out there, that the documentation for developing Loupedeck plugins is very lacking at best.
One can only hope they improve on this in the future.

## Issues and improvements

I would've liked to not use a JSON file for providing the token and options, but I couldn't find a better way to allow users to provide these via the Loupedeck app.
If anyone knows if this is possible for a dynamic folder and how to do it, feel free to open a PR.

The navigation buttons also sometimes don't show up when there are multiple pages.
I can only assume this to be a bug of Loupedeck themselves and haven't really found any way to work around it.
If the navigation buttons are missing, closing and opening the folder will make them appear.
