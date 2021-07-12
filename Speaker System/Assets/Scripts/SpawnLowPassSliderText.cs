using System.Linq;
using UnityEngine;
 using UnityEngine.UI;
 
 public class SpawnLowPassSliderText : MonoBehaviour {
 
     Text spawnRoomLowPassFloorTextComponenet;
     private string spawnRoomLowPassFloortextValue = "2000";
 
     void Start() {
         spawnRoomLowPassFloorTextComponenet = GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "SpawnRoomLowPassFloorTextVal").GetComponent<Text>();
         
         SetSpawnRoomLowPassFloorSliderValue((PlayerPrefs.GetFloat("spawnRoomLowPassFloor", 2000f)/200));
     }
    
     public void SetSpawnRoomLowPassFloorSliderValue(float sliderValue) {
         if(sliderValue > 0f){
            spawnRoomLowPassFloortextValue = ((sliderValue * 200f)).ToString();
        }else{
            spawnRoomLowPassFloortextValue = "Off";
        }
         if(spawnRoomLowPassFloorTextComponenet != null){
            spawnRoomLowPassFloorTextComponenet.text = spawnRoomLowPassFloortextValue;
         }
     }
 }
 