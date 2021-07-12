using System.Linq;
using UnityEngine;
 using UnityEngine.UI;
 
 public class SpawnVolFloorSliderText : MonoBehaviour {
 
     Text spawnRoomVolFloorTextComponenet;
     private string spawnRoomVolFloortextValue = "0.5x";
 
     void Start() {
         spawnRoomVolFloorTextComponenet = GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "SpawnRoomVolumeFloorTextVal").GetComponent<Text>();

        SetSpawnRoomVolFloorSliderValue((PlayerPrefs.GetFloat("SpawnRoomVolumeFloor", 0.5f)/0.05f));

     }
    

     public void SetSpawnRoomVolFloorSliderValue(float sliderValue) {
         spawnRoomVolFloortextValue = ((sliderValue * 0.05f)).ToString() + "x";
         if(spawnRoomVolFloorTextComponenet != null){
            spawnRoomVolFloorTextComponenet.text = spawnRoomVolFloortextValue;
         }
     }
 }
 