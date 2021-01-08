# Echo-VR-Speaker-System

This is a system to take any audio stream (usually music) and make it sound like it is being played from "speakers" in a stadium outside of the Arena when you are playing. This is positional audio that adds realistic echo/reverberations as well as taking into account the geometry of the Arena itself so that it feels as though you are hearing your music from a stadium surrounding you.


## This project requires Virtual Audio Cable
  * [Install lite (free) version here](https://software.muzychenko.net/freeware/vac464lite.zip)
  * After installing VAC, make sure to restart your PC and to reset your default audio input and output devices in the Windows settings (Windows will automatically set Virtual Audio Cable as the default input and output device at install time, which you don't want)
  
 ## Setup
 
 * Go to **releases** and download the latest Zip, unzip the files to wherever you want the folder
 * In the Windows settings page ["App volume and device preferences"](ms-settings:apps-volume), set the **output** device to "Line 1 (Virtual Audio Cable)" for the music app/browser window with the sound that you want to be played via the Arena speakers
 * In the in-game settings for Echo VR, make sure that "Enable API Access" is set to "Enabled"
 * Run Echo Speaker System.exe from the folder you unzipped it to whenever you want the audio to be played via the "speakers" 
