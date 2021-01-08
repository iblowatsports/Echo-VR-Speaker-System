using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;

public class SpatialPlayerListener : MonoBehaviour
{
    static string defaultPos = "{\"position\":[ 0,-5,-23], \"forward\":[0.0,0.0,1.0], \"left\":[0.0,0.0,0.0], \"up\":[0.9,0.0,0.0]}";
    GameObject playerObject;
    static string echoVRIP = "127.0.0.1";
    static string echoVRPort = "6721";
    static string url = "http://" + echoVRIP + ":" + echoVRPort + "/session";
    public Vector3 headForward;
    public Vector3 headUp;
    public Transform head;
    string tempPlayerName = "";
    float lastAPITime = 0.0f;
    bool isClientSpectator = false;
    // Start is called before the first frame update
    void Start()
    {
        playerObject = GameObject.Find("Player Listener");
    }

    void Update(){
        StartCoroutine(GetRequest());

    }
    IEnumerator GetRequest()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Request and wait for the desired page. 73
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                //Debug.Log(": Error: " + webRequest.error);
                try{
                Head defaultHeadPos = JsonUtility.FromJson<Head>(defaultPos);
                float[] playerHeadPosition = defaultHeadPos.position;
                float[] playerHeadForward = defaultHeadPos.forward;
                float[] playerHeadUp = defaultHeadPos.up;
                playerObject.transform.position = new Vector3(playerHeadPosition[2], playerHeadPosition[1], playerHeadPosition[0]);
                headUp = new Vector3(playerHeadUp[2], playerHeadUp[1], playerHeadUp[0]);
                headForward = new Vector3(playerHeadForward[2], playerHeadForward[1], playerHeadForward[0]);
                playerObject.transform.LookAt(headForward + transform.position, headUp);
                }catch(Exception e){
                    //Debug.Log(e);
                }
            }
            else
            {
                // /Debug.Log(":\nReceived API Frame ");
                string resp = webRequest.downloadHandler.text;
                bool found = false;
                try{
                    Frame apiFrame = JsonUtility.FromJson<Frame>(resp);
                    playerObject = GameObject.Find("Player Listener");
                    foreach(Team t in apiFrame.teams){
                        if(t.players != null){
                            foreach(Player p in t.players){
                                if(p.name == apiFrame.client_name){
                                    if(t.team == "SPECTATORS"){
                                        isClientSpectator = true;
                                    }else{
                                        found = true;
                                        isClientSpectator = false;
                                        float[] playerHeadPosition = p.head.position;
                                        float[] playerHeadForward = p.head.forward;
                                        float[] playerHeadUp = p.head.up;
                                        playerObject.transform.position = new Vector3(playerHeadPosition[2], playerHeadPosition[1], playerHeadPosition[0]);
                                        headUp = new Vector3(playerHeadUp[2], playerHeadUp[1], playerHeadUp[0]);
                                        headForward = new Vector3(playerHeadForward[2], playerHeadForward[1], playerHeadForward[0]);
                                        playerObject.transform.LookAt(headForward + transform.position, headUp);
                                    }
                                    
                                }else if(isClientSpectator){
                                    if(tempPlayerName.Length < 1){
                                        if(t.team != "SPECTATORS" && !found){
                                            found = true;
                                            tempPlayerName = p.name;
                                            float[] playerHeadPosition = p.head.position;
                                            float[] playerHeadForward = p.head.forward;
                                            float[] playerHeadUp = p.head.up;
                                            playerObject.transform.position = new Vector3(playerHeadPosition[2], playerHeadPosition[1], playerHeadPosition[0]);
                                            headUp = new Vector3(playerHeadUp[2], playerHeadUp[1], playerHeadUp[0]);
                                            headForward = new Vector3(playerHeadForward[2], playerHeadForward[1], playerHeadForward[0]);
                                            playerObject.transform.LookAt(headForward + transform.position, headUp);
                                        }
                                    }else if(p.name == tempPlayerName && t.team != "SPECTATORS"){
                                        found = true;
                                        tempPlayerName = p.name;
                                        float[] playerHeadPosition = p.head.position;
                                        float[] playerHeadForward = p.head.forward;
                                        float[] playerHeadUp = p.head.up;
                                        playerObject.transform.position = new Vector3(playerHeadPosition[2], playerHeadPosition[1], playerHeadPosition[0]);
                                        headUp = new Vector3(playerHeadUp[2], playerHeadUp[1], playerHeadUp[0]);
                                        headForward = new Vector3(playerHeadForward[2], playerHeadForward[1], playerHeadForward[0]);
                                        playerObject.transform.LookAt(headForward + transform.position, headUp);
                                    }
                                }
                            }
                        }
                    }
                    var currentFrameTime = Time.realtimeSinceStartup;
                    if((currentFrameTime - lastAPITime) != 0){
                        Debug.Log(String.Format("Frame receive rate: {0} hz", 1/(currentFrameTime - lastAPITime)));
                    }
                    lastAPITime = currentFrameTime;
                    if(!found && tempPlayerName.Length > 0){
                        tempPlayerName = "";
                    }
                }catch(Exception e){
                    //Debug.Log(e);
                    try{
                        Head defaultHeadPos = JsonUtility.FromJson<Head>(defaultPos);
                        float[] playerHeadPosition = defaultHeadPos.position;
                        float[] playerHeadForward = defaultHeadPos.forward;
                        float[] playerHeadUp = defaultHeadPos.up;
                        playerObject.transform.position = new Vector3(playerHeadPosition[2], playerHeadPosition[1], playerHeadPosition[0]);
                        headUp = new Vector3(playerHeadUp[2], playerHeadUp[1], playerHeadUp[0]);
                        headForward = new Vector3(playerHeadForward[2], playerHeadForward[1], playerHeadForward[0]);
                        playerObject.transform.LookAt(headForward + transform.position, headUp);
                    }
                    catch{

                    }
                }
            }
        }
    }
    // Update is called once per frame
    // bool UpdatePlayerPos()
    // {        
    //     using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
    //     {
    //         // Request and wait for the desired page.
    //         yield return webRequest.SendWebRequest();

    //         if (webRequest.isNetworkError)
    //         {
    //             Debug.Log(": Error: " + webRequest.error);
    //         }
    //         else
    //         {
    //             Debug.Log(":\nReceived API Frame ");
    //             string resp = webRequest.downloadHandler.text;
    //             try{
    //                 Frame foundFrame = JsonUtility.FromJson<Frame>(resp);
    //             }catch(Exception e){
    //                 Debug.Log(e);
    //             }
    //         }
    //     }
    //     return true;
    // }
}
