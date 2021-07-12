using System.Linq;
using UnityEngine;
 using UnityEngine.UI;
 
 public class SecondsSliderText : MonoBehaviour {
 
     Text textComponent;
     private string textValue = "23s";
 
     void Start() {
         textComponent = GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "GoalHornTimeTextVal").GetComponent<Text>();
         SetSliderValue(PlayerPrefs.GetFloat("GoalHornMaxDuration", 23f));
     }
    
 
     public void SetSliderValue(float sliderValue) {
         textValue = sliderValue.ToString() + "s";
         if(textComponent != null){
            textComponent.text = textValue;
         }
     }
 }