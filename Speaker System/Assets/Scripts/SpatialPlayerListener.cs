using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;

public class SpatialPlayerListener : MonoBehaviour
{
    static string defaultPos = "{\"position\":[ 0,0,-23], \"forward\":[0.0,0.0,1.0], \"left\":[0.0,0.0,0.0], \"up\":[0.9,0.0,0.0]}";
    GameObject playerObject;
    static string echoVRIP = "127.0.0.1";
    static string echoVRPort = "6721";
    static string url = "http://" + echoVRIP + ":" + echoVRPort + "/session";
    public Vector3 headForward;
    public bool goalScored = false;
    public Vector3 headUp;
    public Transform head;
    public bool isIgniteBotEmbedded = false;
    public bool isReverbMixChangeOn = true;
    public NetMQPoller poller;
    public SubscriberSocket subSocket;
    public string addr = "tcp://localhost:12345";

    float[] _playerHeadPosition;
    float[] _playerHeadForward;
    float[] _playerHeadUp;

    string tempPlayerName = "";
    float lastAPITime = 0.0f;
    bool isClientSpectator = false;
    public bool quitCalled = false;
    public bool hasCleanedUp = false;
    public bool speakersReady = false;
    // Start is called before the first frame update
    void Start()
    {
        SetDefaultListenerPosition();
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            Debug.Log("ARG " + i + ": " + args[i]);
            if (args[i].Contains("ignitebot"))
            {
                isIgniteBotEmbedded = true;
                break;
            }
        }
        playerObject = GameObject.Find("Player Listener");
        if (isIgniteBotEmbedded)
        {
            AsyncIO.ForceDotNet.Force();
            NetMQConfig.Cleanup();
            poller = new NetMQPoller();
            subSocket = new SubscriberSocket();

            subSocket.ReceiveReady += OnReceiveReady;
            subSocket.Options.ReceiveHighWatermark = 10;
            subSocket.Connect(addr);
            subSocket.Subscribe("RawFrame");
            subSocket.Subscribe("CloseApp");
            subSocket.Subscribe("MatchEvent");

            poller.Add(subSocket);
            poller.RunAsync();
        }
    }
    public void Cleanup()
    {
        if (isIgniteBotEmbedded && !hasCleanedUp)
        {
            poller.StopAsync();
            Thread.Sleep(10);
            subSocket.Dispose();
            poller.Dispose();
            NetMQConfig.Cleanup(false);
            hasCleanedUp = true;
        }
    }

    void OnReceiveReady(object sender, NetMQSocketEventArgs e)
    {

        if (!quitCalled)
        {
            var str = e.Socket.ReceiveFrameString();
            if (str == "CloseApp")
            {
                quitCalled = true;
            }else if(str == "MatchEvent"){
                string messageReceived = e.Socket.ReceiveFrameString();
                try{
                    MatchEvent eventMSG = JsonUtility.FromJson<MatchEvent>(messageReceived);
                    if(eventMSG.EventTypeName == "LeaveMatch"){
                        SetDefaultListenerPosition();
                    }else if(eventMSG.EventTypeName == "GoalScored"){
                        if(eventMSG.Data[0].Value == "True"){
                            goalScored = true;
                        }
                    }
                }catch{}
            }
            else if(str == "RawFrame")
            {
                string messageReceived = e.Socket.ReceiveFrameString();
                Console.WriteLine(messageReceived + "\n");
                Thread.Sleep(2);
                bool found = false;
                try
                {
                    Frame apiFrame = JsonUtility.FromJson<Frame>(messageReceived);
                    foreach (Team t in apiFrame.teams)
                    {
                        if (t.players != null)
                        {
                            foreach (Player p in t.players)
                            {
                                if (p.name == apiFrame.client_name)
                                {
                                    if (t.team == "SPECTATORS")
                                    {
                                        isClientSpectator = true;
                                    }
                                    else
                                    {
                                        found = true;
                                        isClientSpectator = false;
                                        _playerHeadPosition = p.head.position;
                                        _playerHeadForward = p.head.forward;
                                        _playerHeadUp = p.head.up;
                                    }

                                }
                                else if (isClientSpectator)
                                {
                                    if (tempPlayerName.Length < 1)
                                    {
                                        if (t.team != "SPECTATORS" && !found)
                                        {
                                            found = true;
                                            tempPlayerName = p.name;
                                            _playerHeadPosition = p.head.position;
                                            _playerHeadForward = p.head.forward;
                                            _playerHeadUp = p.head.up;
                                        }
                                    }
                                    else if (p.name == tempPlayerName && t.team != "SPECTATORS")
                                    {
                                        found = true;
                                        tempPlayerName = p.name;
                                        _playerHeadPosition = p.head.position;
                                        _playerHeadForward = p.head.forward;
                                        _playerHeadUp = p.head.up;
                                    }
                                }
                            }
                        }
                    }
                    if (!found && tempPlayerName.Length > 0)
                    {
                        tempPlayerName = "";
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        Head defaultHeadPos = JsonUtility.FromJson<Head>(defaultPos);
                        _playerHeadPosition = defaultHeadPos.position;
                        _playerHeadForward = defaultHeadPos.forward;
                        _playerHeadUp = defaultHeadPos.up;
                    }
                    catch
                    {

                    }
                }
            }
        }
    }

    void Update()
    {
        if (quitCalled)
        {
            Cleanup();
        }
        else
        {
            if(speakersReady){
                if (!isIgniteBotEmbedded)
                {
                    StartCoroutine(GetRequest());
                }

                playerObject.transform.position = new Vector3(_playerHeadPosition[2], _playerHeadPosition[1], _playerHeadPosition[0]);
                headUp = new Vector3(_playerHeadUp[2], _playerHeadUp[1], _playerHeadUp[0]);
                headForward = new Vector3(_playerHeadForward[2], _playerHeadForward[1], _playerHeadForward[0]);
                playerObject.transform.LookAt(headForward + transform.position, headUp);
            }
        }
    }

    void SetDefaultListenerPosition(){
        Head defaultHeadPos = JsonUtility.FromJson<Head>(defaultPos);
        _playerHeadPosition = defaultHeadPos.position;
        _playerHeadForward = defaultHeadPos.forward;
        _playerHeadUp = defaultHeadPos.up;
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
                try
                {
                    SetDefaultListenerPosition();
                }
                catch (Exception e)
                {
                    //Debug.Log(e);
                }
            }
            else
            {
                // /Debug.Log(":\nReceived API Frame ");
                string resp = webRequest.downloadHandler.text;
                bool found = false;
                try
                {
                    Frame apiFrame = JsonUtility.FromJson<Frame>(resp);
                    playerObject = GameObject.Find("Player Listener");
                    foreach (Team t in apiFrame.teams)
                    {
                        if (t.players != null)
                        {
                            foreach (Player p in t.players)
                            {
                                if (p.name == apiFrame.client_name)
                                {
                                    if (t.team == "SPECTATORS")
                                    {
                                        isClientSpectator = true;
                                    }
                                    else
                                    {
                                        found = true;
                                        isClientSpectator = false;
                                        _playerHeadPosition = p.head.position;
                                        _playerHeadForward = p.head.forward;
                                        _playerHeadUp = p.head.up;
                                    }

                                }
                                else if (isClientSpectator)
                                {
                                    if (tempPlayerName.Length < 1)
                                    {
                                        if (t.team != "SPECTATORS" && !found)
                                        {
                                            found = true;
                                            tempPlayerName = p.name;
                                            _playerHeadPosition = p.head.position;
                                            _playerHeadForward = p.head.forward;
                                            _playerHeadUp = p.head.up;
                                        }
                                    }
                                    else if (p.name == tempPlayerName && t.team != "SPECTATORS")
                                    {
                                        found = true;
                                        tempPlayerName = p.name;
                                        _playerHeadPosition = p.head.position;
                                        _playerHeadForward = p.head.forward;
                                        _playerHeadUp = p.head.up;
                                    }
                                }
                            }
                        }
                    }
                    var currentFrameTime = Time.realtimeSinceStartup;
                    if ((currentFrameTime - lastAPITime) != 0)
                    {
                        //Debug.Log(String.Format("Frame receive rate: {0} hz", 1 / (currentFrameTime - lastAPITime)));
                    }
                    lastAPITime = currentFrameTime;
                    if (!found && tempPlayerName.Length > 0)
                    {
                        tempPlayerName = "";
                    }
                }
                catch (Exception e)
                {
                    //Debug.Log(e);
                    try
                    {
                        SetDefaultListenerPosition();
                    }
                    catch
                    {

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
