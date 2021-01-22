# Echo-VR-Speaker-System

This is a system to take any audio stream (usually music) and make it sound like it is being played from "speakers" in a stadium outside of the Arena when you are playing. This is positional audio that adds realistic echo/reverberations as well as taking into account the geometry of the Arena itself so that it feels as though you are hearing your music from a stadium surrounding you.


## This project requires Virtual Audio Cable
  * [Install lite (free) version here](https://software.muzychenko.net/freeware/vac464lite.zip)
  * After installing VAC, make sure to restart your PC and to reset your default audio input and output devices in the Windows settings (Windows will automatically set Virtual Audio Cable as the default input and output device at install time, which you don't want)
  
 ## Setup
 
 * Go to **[releases](https://github.com/iblowatsports/Echo-VR-Speaker-System/releases/latest)** and download the latest Zip, unzip the files to wherever you want the folder
 * In the in-game settings for Echo VR, make sure that "Enable API Access" is set to "Enabled"
 * Run Echo Speaker System.exe from the folder you unzipped it to, when it launches: 
   * Unless you have a specific use case, set/leave the Audio Input selection on "Line 1(Virtual Audio Cable). This is used as the audio input device for the speakers
   * Select the application you would like played via the "speakers" (you may have to hit the refresh app list button if it does not show up). This will switch the selected app to be played via another input device. 
* Any sound played from the selected application will now go through the virtual speakers surrounding the arena. Once you close the Echo Speaker System exe, the application you had selected will be switched back to the audio device it originally was using. Any time the Echo Speaker System exe is run in the future, it will attempt to automatically find and use the Application you had previously used with the Speaker System
