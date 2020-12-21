/**
 * iblowatsports
 * Date: 19 December 2020
 * Purpose: Handle speaker stuff (not used atm, might delete)
 */


// //This script allows you to toggle music to play and stop.
// //Assign an AudioSource to a GameObject and attach an Audio Clip in the Audio Source. Attach this script to the GameObject.
// using System.Collections;
// using System.Collections.Generic;

// using UnityEngine;

// public class SpeakerScript : MonoBehaviour
// {
//     public AudioSource Source1;
//     AudioSource m_MyAudioSource;

//     ArrayList speakers = new ArrayList();

//     //Play the music
//     bool m_Play;
//     //Detect when you use the toggle, ensures music isn’t played multiple times
//     bool m_ToggleChange;
//     int speakerCount = 7;

//  const int FREQUENCY = 48000;
//     AudioClip mic;
//     int lastPos, pos;
 
//     // Use this for initialization
//     void Start () {
//         for(int i = 1; i <= speakerCount; i++){
//             speakers.Add(GameObject.Find("Speaker " + i).GetComponent<AudioSource>());
//         }
        
//         mic = Microphone.Start(null, true, 15, FREQUENCY);
 
//         m_MyAudioSource = GetComponent<AudioSource>();
//         m_MyAudioSource.clip = AudioClip.Create("test", 15 * FREQUENCY, 1, FREQUENCY, false);
//         m_MyAudioSource.loop = true;
 
//     }
   
//     // Update is called once per frame
//     void Update () {
//         m_MyAudioSource = GetComponent<AudioSource>();
//         if((pos = Microphone.GetPosition(null)) > 0){
//             if(lastPos > pos)    lastPos = 0;
 
//             if(pos - lastPos > 0){
//                 // Allocate the space for the sample.
//                 float[] sample = new float[(pos - lastPos) * 1];
 
//                 // Get the data from microphone.
//                 mic.GetData(sample, lastPos);
 
//                 // Put the data in the audio source.
//                 m_MyAudioSource.clip.SetData(sample, lastPos);
               
//                 if(!m_MyAudioSource.isPlaying)    m_MyAudioSource.Play();
 
//                 lastPos = pos;  
//             }
//         }
//     }
 
//     void OnDestroy(){
//         Microphone.End(null);
//     }
//     // void Start()
//     // {
//     //     //Fetch the AudioSource from the GameObject
//     //     m_MyAudioSource = GetComponent<AudioSource>();
//     //     m_MyAudioSource.clip = Microphone.Start("Line In", true, 10, 48000);
//     //     m_MyAudioSource.loop = true;
//     //     while(!(Microphone.GetPosition(null) > 0)){}
//     //     m_MyAudioSource.Play();
//     //     //Ensure the toggle is set to true for the music to play at start-up
//     //     //m_Play = true;
//     // }

//     // void Update()
//     // {
//     //     //Check to see if you just set the toggle to positive
//     //     // if (m_Play == true && m_ToggleChange == true)
//     //     // {
//     //     //     //Play the audio you attach to the AudioSource component
//     //     //     m_MyAudioSource.Play();
//     //     //     //Ensure audio doesn’t play more than once
//     //     //     m_ToggleChange = false;
//     //     // }
//     //     // //Check if you just set the toggle to false
//     //     // if (m_Play == false && m_ToggleChange == true)
//     //     // {
//     //     //     //Stop the audio
//     //     //     m_MyAudioSource.Stop();
//     //     //     //Ensure audio doesn’t play more than once
//     //     //     m_ToggleChange = false;
//     //     // }
//     // }

//     void OnGUI()
//     {
//         //Switch this toggle to activate and deactivate the parent GameObject
//         // m_Play = GUI.Toggle(new Rect(10, 10, 100, 30), m_Play, "Play Music");

//         // //Detect if there is a change with the toggle
//         // if (GUI.changed)
//         // {
//         //     //Change to true to show that there was just a change in the toggle state
//         //     m_ToggleChange = true;
//         // }
//     }
// }
