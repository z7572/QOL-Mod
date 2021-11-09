# QOL-Mod
A mod that offers quality-of-life improvements and additions to [Stick Fight: The Game](https://store.steampowered.com/app/674940/Stick_Fight_The_Game/).<br/>
This is accomplished through a GUI menu but alternative chat commands are listed below.<br/>
To open the menu, use the keybind: <kbd>LeftShift</kbd> + <kbd>F1</kbd><br/>

A previous message system allows you to use the <kbd>↑</kbd> & <kbd>↓</kbd> keys to easily return to your previous messages.<br/>
There is a maximum of **``20``** messages stored before they start being overwritten.<br/>

The mod is a plugin for BepInEx which is required to load it. Everything is patched at runtime.<br/>
Scroll to the bottom of this README for a video of a general overview of this mod.

## Installation

To use the mod, here are the steps required:<br/> 
  1)  Download [BepInEx](https://github.com/BepInEx/BepInEx/releases), grab the lastest release of version **``5.4``** (**must be 32-bit, x86**).
  2)  Extract the newly downloaded zip into the ``StickFightTheGame`` folder.
  3)  Launch the game and then exit (BepInEx will have generated new files and folders).
  4)  Download the latest version of the QOL mod from the releases section.
  5)  Put the mod into the now generated ``BepInEx/plugins`` folder for BepInEx to load.
  6)  Start the game, join a lobby, and enjoy!

## Caveats

The following are some general things to take note of:
  - Both the ``/private`` & ``/public`` commands require you to be the host in order to function.
  - The ``/rich`` command only enables rich text for you, and anyone else using the mod.
  - The auto-translation feature uses the Google Translate API and has a rate-limit of **``100``** requests per hour.
  - The ``ツ`` character outputted by the ``/shrug`` command shows up as invalid (�) ingame.

## GUI Menu

The menu is the primary way to use and enable/disable features.<br/>
It can be opened with the keybind: <kbd>LeftShift</kbd> + <kbd>F1</kbd><br/>
An image below shows a visual overview:<br/>
![Image of QOL Menu](https://i.ibb.co/LhWr9hV/QOL-MENU-cropped.png)<br/>
Alternative chat commands are listed directly below.
## Chat Commands

Command | Description
--------- | -----------
**Usage:**		| ```/<command_name> [<additional parameter>]```
/gg		| Enables automatic sending of "gg" upon death of mod user.
/shrug ```[<message>]```		| Appends ¯\\\_(ツ)\_/¯ to the end of the typed message.
/rich		| Enables rich text for chat (**visible to mod user only**).
/private		| Privates the current lobby (**must be host**).
/public		| Opens the current lobby to the public (**must be host**).
/uncensor		| Disables chat censorship.
/hp	```[<target_color>]```	| Outputs the percent based health of the target color to chat. Leave as ``/hp`` to always get your own.
/uwu		| *uwuifies* any message you send.
/invite		| Generates a "join game" link and copies it to clipboard.
/translate		| Enables auto-translation for messages from others to English.

## Using The Config

A configuration file named ``monky.plugins.QOL.cfg`` can be found under ``BepInEx\config``.<br/>
Please note that you ___must run the mod at least once___ for it to be generated.<br/>
You can currently use it to set certain features to be enabled on startup.<br/>
Example: 
```cfg
## Enable rich text for chat on startup?
# Setting type: Boolean
# Default value: false
RichTextInChat = true
```
Changing ``RichTextInChat = false`` to ``RichTextInChat = true`` will enable it on startup without the need for doing ``/rich`` to enable it.<br/>

Another important option to mention for the config is the ability to specify an API key for Google Translate.<br/>
In doing so, this will allow you to bypass the rate-limit that comes normally.<br/> 
**You are responsible for creating the key, and any potential charges accrued.**<br/>
Instructions & documentation for all of that can be found [here](https://cloud.google.com/translate).<br/>

Updating the mod ***does not*** require you to delete the config.

## QOL Mod Overview

Video coming soon!
