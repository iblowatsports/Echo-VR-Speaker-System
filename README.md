# Echo-VR-Speaker-System

This is a system to take any audio stream (usually music) and make it sound like it is being played from "speakers" in a stadium outside of the Arena when you are playing. This is positional audio that adds realistic echo/reverberations as well as taking into account the geometry of the Arena itself so that it feels as though you are hearing your music from a stadium surrounding you.

![Speaker System App](https://github.com/iblowatsports/Echo-VR-Speaker-System/blob/main/EchoSpeakerSystem.png?raw=true)

  
 ## Setup
 
 * Go to **[releases](https://github.com/iblowatsports/Echo-VR-Speaker-System/releases/latest)** and download the **Installer exe. Run this as administrator to install Echo Speaker System** and follow any UI prompts for Virtual Audio Cable installation if required (restart if needed as well)
 * In the in-game settings for Echo VR, make sure that "Enable API Access" is set to "Enabled"
 * Run Echo Speaker System
   * Select the application you would like played via the "speakers" (you may have to hit the refresh app list button if it does not show up). This will switch the selected app to be played via another input device. 
* Any sound played from the selected application will now go through the virtual speakers surrounding the arena. Once you close the Echo Speaker System exe, the application you had selected will be switched back to the audio device it originally was using. Any time the Echo Speaker System exe is run in the future, it will attempt to automatically find and use the Application you had previously used with the Speaker System
