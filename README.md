# Echo-VR-Speaker-System

This is a system to take any audio stream (usually music) and make it sound like it is being played from "speakers" in a stadium outside of the Arena when you are playing. This is positional audio that adds realistic echo/reverberations as well as taking into account the geometry of the Arena itself so that it feels as though you are hearing your music from a stadium surrounding you.

![Speaker System App](https://github.com/iblowatsports/Echo-VR-Speaker-System/blob/main/EchoSpeakerSystem.png?raw=true)

  
 ## Setup
 There are two methods to install and use Echo Speaker System: via Ignite Bot or as a standalone app. Using Echo Speaker System from Ignite Bot is **required for Quest users** and is also the only way to have custom goal horn support within Echo Speaker System.
 
 ### Ignite Bot
 * Go to https://ignitevr.gg/ignitebot download, and install the latest Ignite Bot
 * Run Ignite Bot and click on the "Speaker System" tab. Click "Install Echo Speaker System" and follow the installation prompts
 * To use Echo Speaker System, run Ignite Bot and click "Start Speaker System" from within the "Speaker System" tab
 
 ### Standalone
 * Go to **[releases](https://github.com/iblowatsports/Echo-VR-Speaker-System/releases/latest)** and download the **Installer exe. Run this as administrator to install Echo Speaker System** and follow any UI prompts for Virtual Audio Cable installation if required (restart if needed as well)
 * In the in-game settings for Echo VR, make sure that "Enable API Access" is set to "Enabled"
 * Run Echo Speaker System
   * Select the application you would like played via the "speakers" (you may have to hit the refresh app list button if it does not show up). This will switch the selected app to be played via another input device. 
* Any sound played from the selected application will now go through the virtual speakers surrounding the arena. Once you close the Echo Speaker System exe, the application you had selected will be switched back to the audio device it originally was using. Any time the Echo Speaker System exe is run in the future, it will attempt to automatically find and use the Application you had previously used with the Speaker System

## Note for Quest Users
In order to use this on Quest, you **must** install Ignite Bot and use Echo Speaker System from within Ignite Bot, as Ignite Bot handles finding and getting the API data from the Quest over the network. Also, in order to use this on Quest, you must have some way to get the audio from your PC to your Quest (ex: using a headset that supports wireless audio from your PC and wired audio from your Quest at the same time). **This app will not work if you do not have a way to listen to audio from your PC while playing on your Quest**
