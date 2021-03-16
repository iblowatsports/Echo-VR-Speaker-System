/**
 * iblowatsports
 * Date: 19 December 2020
 * Purpose: Run arena music process
 */

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.IO.Compression;
using System.Text;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using SteamAudio;
using System.Net;
using System.ComponentModel;

public class SpeakersStart : MonoBehaviour
{
    public string VERSION_TAGNAME = "v0.4.3";

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();

    [DllImport("WinAudioDLL", CallingConvention = CallingConvention.Cdecl)]
    private static extern void GetAudioEndpointInfo(StringBuilder str, int len);

    public AudioEndpoints GetAudioInfo()
    {
        StringBuilder str = new StringBuilder(16000);

        GetAudioEndpointInfo(str, 16000);

        return JsonUtility.FromJson<AudioEndpoints>(str.ToString());
    }
    public static System.IntPtr GetWindowHandle()
    {
        return GetActiveWindow();
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern int MessageBox(IntPtr hwnd, String lpText, String lpCaption, uint uType);

    /// <summary>
    /// Shows Error alert box with OK button.
    /// </summary>
    /// <param name="text">Main alert text / content.</param>
    /// <param name="caption">Message box title.</param>
    public static void Error(string text, string caption)
    {
        try
        {
            MessageBox(GetWindowHandle(), text, caption, (uint)(0x00000000L | 0x00000010L));
        }
        catch { }
    }

    bool isAppListRefreshing = false;
    bool isGoalHornFileValid = false;
    public AudioSource Source1;
    string inputName = "";
    bool isGoalHornEnabled = true;
    bool isFirstAppInit = true;
    string appExeName = "";
    string originalAppEndpoint = "";
    bool wasAppEndpointChanged = false;
    public bool hasCleanedUp = false;
    bool goalHornPlaying = false;

    Toggle goalHornToggle;
    List<AppEndpoint> originalAppEndpoints = new List<AppEndpoint>();
    AudioSource masterSpeaker;
    List<AudioSource> speakers = new List<AudioSource>();
    Dictionary<string, float> speakerDelays = new Dictionary<string, float>();
    Dictionary<string, AudioEchoFilter> speakerEchos = new Dictionary<string, AudioEchoFilter>();
    Dictionary<string, AudioReverbFilter> speakerReverbs = new Dictionary<string, AudioReverbFilter>();

    AudioEchoFilter masterSpeakerEcho;
    Dictionary<string, SteamAudioSource> steamAudioSpeakers = new Dictionary<string, SteamAudioSource>();
    public float speedOfSoundMultiplier = 1.39f;
    public float reverbLevel = 1.0f;
    int previousTimeSamples = 0;
    string latestReleaseURL = "";
    string latestReleaseVer = "";
    AudioReverbPreset globalReverbPreset, spawnRoomReverbPreset;
    int speakerCount = 18;
    bool reverseLoopOrder;
    float listenerVolume, goalHornClipVolMult = 0.0f;
    float goalHornVolumeUserMult = 1.1f;
    Slider goalHornVolMultSlider, goalHornTimeSlider;
    PlayerListener playerListener;
    SteamAudioListener steamAudioListener;
    AudioListener playerAudioListener;
    AudioLowPassFilter playerListernerLowPass;
    Dropdown.OptionData AudioInputData, AppSelectionData, ReverbPresetData;
    AudioEndpoints audioEndpointsJson = null;
    List<Dropdown.OptionData> AudioInputMessages = new List<Dropdown.OptionData>();
    List<Dropdown.OptionData> ReverbPresetMessages = new List<Dropdown.OptionData>();
    List<Dropdown.OptionData> AppSelectionMessages = new List<Dropdown.OptionData>();
    Dropdown AudioInputDropdown, AppSelectionDropdown, ReverbPresetDropdown, SpawnRoomReverbPresetDropdown;
    DropdownMouseOver AppSelectionDropdownMouseOver;
    int AudioInputIndex, AppSelectionIndex, ReverbPresetIndex;
    GameObject VACDownloadBtnGameObject, UpdateDownloadBtnGameObject;
    Button MSSettingsBtn, VACDownloadBtn, UpdateDownloadBtn, RefreshAppListBtn;
    string VACInputName = "(Virtual Audio Cable)";
    const int FREQUENCY = 48000;
    AudioClip masterClip, goalHornClip;
    float averageGoalHornLoudness, averageMusicLoudness, musicLoudnessAcc = 0.0f;
    long musicLoudnessCount = 0;
    int lastPos, pos;
    int loops;
    bool respawnResetDone;
    bool isReady,clipZeroed,isNewUpdate = false;
    public static string updateFileName = "";
    float goalHornMaxDuration = 23f;
    bool inSpawnRoom = false;

    // Use this for initialization
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        var config = AudioSettings.GetConfiguration();
        AudioSettings.Reset(config);
        MSSettingsBtn = GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "OpenMSAppAudioSettingsBtn").GetComponent<Button>();
        goalHornVolMultSlider = GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "GoalHornVolMultSlider").GetComponent<Slider>();
        goalHornTimeSlider = GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "GoalHornTimeSlider").GetComponent<Slider>();
        isNewUpdate = PlayerPrefs.GetString("RunningVersion", "") != VERSION_TAGNAME;
        globalReverbPreset = (AudioReverbPreset)PlayerPrefs.GetInt("GlobalReverbPreset", (int)AudioReverbPreset.Arena);
        spawnRoomReverbPreset = (AudioReverbPreset)PlayerPrefs.GetInt("SpawnRoomReverbPreset", (int)AudioReverbPreset.Arena);
        goalHornVolumeUserMult = PlayerPrefs.GetFloat("GoalHornVolumeUserMult", 1.1f);
        goalHornMaxDuration = PlayerPrefs.GetFloat("GoalHornMaxDuration", 23f);
        goalHornTimeSlider.value = goalHornMaxDuration;
        goalHornVolMultSlider.value = (goalHornVolumeUserMult - 0.5f)/0.05f;
        StartCoroutine(GetWhatsNew());
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Contains("selectinput"))
            {
                ShowHideInputDropdown(true);
                break;
            }
            if (args[i].Contains("reset"))
            {
                PlayerPrefs.DeleteAll();
                break;
            }
        }
        goalHornToggle = GameObject.Find("GoalHornToggle").GetComponent<Toggle>();
        isGoalHornEnabled = PlayerPrefs.GetInt("GoalHornEnabled", 0) == 1;
        goalHornToggle.isOn = isGoalHornEnabled;
        goalHornToggle.onValueChanged.AddListener(delegate
        {
            GoalHornToggleValueChanged(goalHornToggle);
        });
        StartCoroutine(TryLoadGoalHornWav(isGoalHornEnabled));
        
        inputName = PlayerPrefs.GetString("InputName", "Line 1 (Virtual Audio Cable)");
        appExeName = PlayerPrefs.GetString("AppSourceName", "");
        AudioInputDropdown = GameObject.Find("AudioSourceDropdown").GetComponent<Dropdown>();
        if(inputName != "Line 1 (Virtual Audio Cable)"){
            ShowHideInputDropdown(true);
        }else{
            ShowHideInputDropdown(false);
        }
        AudioInputDropdown.ClearOptions();
        AppSelectionDropdown = GameObject.Find("AppSelectionDropdown").GetComponent<Dropdown>();
        ReverbPresetDropdown = GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "ReverbPresetDropdown").GetComponent<Dropdown>();
        SpawnRoomReverbPresetDropdown = GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "SpawnRoomReverbPresetDropdown").GetComponent<Dropdown>();
        VACDownloadBtnGameObject = GameObject.Find("OpenVACDownloadBtn");
        VACDownloadBtn = VACDownloadBtnGameObject.GetComponent<Button>();
        VACDownloadBtnGameObject.SetActive(false);
        UpdateDownloadBtnGameObject = GameObject.Find("DownloadUpdateBtn");
        UpdateDownloadBtn = UpdateDownloadBtnGameObject.GetComponent<Button>();
        UpdateDownloadBtnGameObject.SetActive(false);

        RefreshAppListBtn = GameObject.Find("RefreshAppListBtn").GetComponent<Button>();

        MSSettingsBtn.onClick.AddListener(delegate
        {
            OpenWinAppAudioSettings();
        });

        RefreshAppListBtn.onClick.AddListener(delegate
        {
            refreshAppList();
        });
        AppSelectionDropdownMouseOver = AppSelectionDropdown.GetComponent<DropdownMouseOver>();
        GameObject playerObject = GameObject.Find("Player Listener");
        playerListener = playerObject.GetComponent<PlayerListener>();
        
        //TODO: replace
        if (1 == 1)
        {
            StartCoroutine(GetLatestVer());
            isGoalHornEnabled = false;
            GameObject.Find("GoalHornToggle").SetActive(false);
            if (goalHornClip != null)
            {
                goalHornClip = null;
            }
        }
        playerAudioListener = playerObject.GetComponent<AudioListener>();
        steamAudioListener = playerAudioListener.GetComponent<SteamAudioListener>();
        playerListernerLowPass = playerAudioListener.GetComponent<AudioLowPassFilter>();
        masterSpeaker = GameObject.Find("MasterSpeaker").GetComponent<AudioSource>();
        // float mRandom = UnityEngine.Random.Range(0.92f,1.08f);
        // masterSpeaker.volume *= mRandom;
        // speakerDelays.Add(masterSpeaker.name, mRandom);
        // speakerEchos.Add(masterSpeaker.name, masterSpeaker.GetComponent<AudioEchoFilter>());
        for (int i = 1; i <= speakerCount; i++)
        {
            AudioSource speaker = GameObject.Find("Speaker " + i).GetComponent<AudioSource>();
            speaker.mute = true;
            speakers.Add(speaker);
            float random = UnityEngine.Random.Range(0.92f, 1.08f);
            speaker.volume *= UnityEngine.Random.Range(0.89f, 0.999f);
            speakerDelays.Add(speaker.name, UnityEngine.Random.Range(0.92f, 1.08f));
            speakerEchos.Add(speaker.name, speaker.GetComponent<AudioEchoFilter>());
        }
        for (int i = 1; i <= speakerCount; i++)
        {
            steamAudioSpeakers.Add("Speaker " + i, GameObject.Find("Speaker " + i).GetComponent<SteamAudioSource>());
            speakerReverbs.Add("Speaker " + i, GameObject.Find("Speaker " + i).GetComponent<AudioReverbFilter>());
        }
        foreach (var steamSourcePair in steamAudioSpeakers)
        {
            steamSourcePair.Value.reflections = false;
            float rand = UnityEngine.Random.Range(0.85f, 1.15f);
            steamSourcePair.Value.dipolePower *= rand;
            rand = ((rand - 1.0f) * -1f) + 1.0f;
            GameObject.Find(steamSourcePair.Key).GetComponent<AudioDistortionFilter>().distortionLevel *= rand;
            steamSourcePair.Value.dipoleWeight *= UnityEngine.Random.Range(0.85f, 1.15f);
        }
        foreach (AudioReverbFilter reverb in speakerReverbs.Values)
        {
            reverb.enabled = true;
        }
        bool defaultFound = false;
        bool VACFound = false;
        foreach (var device in Microphone.devices)
        {
            AudioInputData = new Dropdown.OptionData();
            AudioInputData.text = device;
            AudioInputMessages.Add(AudioInputData);
            AudioInputDropdown.options.Add(AudioInputData);
            AudioInputIndex = AudioInputMessages.Count - 1;
            if (device == inputName)
            {
                defaultFound = true;
                AudioInputDropdown.value = AudioInputIndex;
            }
            if (device.Contains(VACInputName))
            {
                VACFound = true;
            }
            UnityEngine.Debug.Log("Name: " + device);
        }
        AudioInputDropdown.onValueChanged.AddListener(delegate
        {
            InputDropdownValueChanged(AudioInputDropdown);
        });
        
        foreach (AudioReverbPreset preset in (AudioReverbPreset[]) Enum.GetValues(typeof(AudioReverbPreset)))
        {
            if (preset != AudioReverbPreset.User)
            {
                ReverbPresetData = new Dropdown.OptionData();
                ReverbPresetData.text = preset.ToString();
                ReverbPresetMessages.Add(ReverbPresetData);
                ReverbPresetDropdown.options.Add(ReverbPresetData);
                SpawnRoomReverbPresetDropdown.options.Add(ReverbPresetData);
                ReverbPresetIndex = ReverbPresetMessages.Count - 1;
                if (preset == globalReverbPreset)
                {
                    ReverbPresetDropdown.value = ReverbPresetIndex;
                }
                if(preset == spawnRoomReverbPreset){
                    SpawnRoomReverbPresetDropdown.value = ReverbPresetIndex;
                }
            }
        }
        ReverbPresetDropdown.onValueChanged.AddListener(delegate
        {
            ReverbPresetDropdownValueChanged(ReverbPresetDropdown);
        });
        SpawnRoomReverbPresetDropdown.onValueChanged.AddListener(delegate
        {
            SpawnRoomReverbPresetDropdownValueChanged(SpawnRoomReverbPresetDropdown);
        });

        AppSelectionDropdown.onValueChanged.AddListener(delegate
        {
            AppSelectionDropdownValueChanged(AppSelectionDropdown);
        });
        refreshAppList();
        if (!defaultFound && !VACFound)
        {
            ShowHideInputDropdown(true);
            Error("Couldn't find audio device " + inputName + ". Make sure to install Virtual Audio Cable and set the output device of your music source to " + inputName + " in the Windows settings under 'App Volume and Device Settings'.", "Error");
            VACDownloadBtnGameObject.SetActive(true);
            VACDownloadBtn.onClick.AddListener(delegate
            {
                OpenVACDownload();
            });
        }

        sourceInit();
    }

    void GoalHornToggleValueChanged(Toggle change)
    {
        //isGoalHornEnabled = goalHornToggle.isOn;
        if(goalHornToggle.isOn){
            StartCoroutine(TryLoadGoalHornWav(true));
        }else{
            isGoalHornEnabled = false;
            goalHornClip = null;
        }
        PlayerPrefs.SetInt("GoalHornEnabled", isGoalHornEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
    IEnumerator TryLoadGoalHornWav(bool enableIfValid)
    {
        string goalHornFilePath = "";
        isGoalHornEnabled = false;
        goalHornFilePath = Application.streamingAssetsPath + "\\..\\..\\GoalHorn.wav";
        string url = string.Format("file://{0}", goalHornFilePath);
        if (File.Exists(goalHornFilePath))
        {
            WWW www = new WWW(url);

            yield return www;
            try
            {
                goalHornClip = www.GetAudioClip(false, false);
                if (goalHornClip != null && goalHornClip.length > 0f)
                {
                    isGoalHornFileValid = true;
                    if(enableIfValid){
                        isGoalHornEnabled = true;
                        PlayerPrefs.SetInt("GoalHornEnabled", isGoalHornEnabled ? 1 : 0);
                        PlayerPrefs.Save();
                    }
                    float clipLoudness = 0.0f;
                    float[] sample = new float[(goalHornClip.samples) * goalHornClip.channels];
                    goalHornClip.GetData(sample, 0);
                    for (int i = 0; i < sample.Length; i++)
                    {
                        clipLoudness += Mathf.Abs(sample[i]);

                    }
                    UnityEngine.Debug.Log("GOALHORN: " + ((float)clipLoudness / (float)sample.Length).ToString());
                    averageGoalHornLoudness = (float)clipLoudness / (float)sample.Length;
                    UnityEngine.Debug.Log((float)clipLoudness / (float)sample.Length);

                }
            }
            catch
            {
                isGoalHornFileValid = false;
                goalHornToggle.enabled = false;
                isGoalHornEnabled = enableIfValid ? false : isGoalHornEnabled;
                goalHornToggle.isOn = false;
                goalHornToggle.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "GoalHornToggleBackground").GetComponent<Image>().color = new Color32(152, 147, 147, 255);
                goalHornToggle.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "GoalHornToggleLabel").GetComponent<Text>().color = new Color32(152, 147, 147, 255);
                GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "goalHornTooltipText").GetComponent<Text>().text = "C:\\Program Files(x86)\\Echo Speaker System\\GoalHorn.wav was not found/is invalid!";
            }
        }
        else
        {
            isGoalHornFileValid = false;
            goalHornToggle.enabled = false;
            isGoalHornEnabled = enableIfValid ? false : isGoalHornEnabled;
            goalHornToggle.isOn = false;
            var bg = goalHornToggle.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "GoalHornToggleBackground").GetComponent<Image>();
            bg.color = new Color32(152, 147, 147, 255);
            goalHornToggle.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "GoalHornToggleBackground").GetComponent<Image>().color = new Color32(152, 147, 147, 255);
            goalHornToggle.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "GoalHornToggleLabel").GetComponent<Text>().color = new Color32(152, 147, 147, 255);
            GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "goalHornTooltipText").GetComponent<Text>().text = "C:\\Program Files(x86)\\Echo Speaker System\\GoalHorn.wav was not found/is invalid!";
        }
    }

    void ShowHideInputDropdown(bool shouldShow)
    {
        if (!shouldShow)
        {
            AudioInputDropdown.enabled = false;
            AudioInputDropdown.interactable = false;
            GameObject.Find("AudioSourceDropdownLabel").GetComponent<Text>().enabled = false;
            GameObject.Find("AudioSourceArrow").GetComponent<Image>().enabled = false;
            AudioInputDropdown.image.enabled = false;
        }
        else
        {
            AudioInputDropdown.enabled = true;
            AudioInputDropdown.interactable = true;
            GameObject.Find("AudioSourceDropdownLabel").GetComponent<Text>().enabled = true;
            GameObject.Find("AudioSourceArrow").GetComponent<Image>().enabled = true;
            AudioInputDropdown.image.enabled = true;
        }
    }

    void sourceInit()
    {
        isReady = false;
        masterSpeaker.clip = null;
        if (masterSpeaker.isPlaying)
        {
            masterSpeaker.Stop();
        }
        foreach (AudioSource aSource in speakers)
        {
            aSource.clip = null;
            if (aSource.isPlaying)
            {
                aSource.Stop();
            }
        }
        masterClip = Microphone.Start(inputName, true, 300, FREQUENCY);
        reverseLoopOrder = false;
        loops = 0;
        //masterClip = AudioClip.Create("test", 300 * FREQUENCY, 1, FREQUENCY, false);
        while (!(Microphone.GetPosition(inputName) > 0)) { }
        masterSpeaker.clip = masterClip;
        masterSpeaker.loop = true;
        masterSpeaker.dopplerLevel = 0f;
        foreach (AudioSource aSource in speakers)
        {
            aSource.dopplerLevel = 0.0f;
            aSource.clip = masterClip;
            aSource.loop = true;
        }
        StartCoroutine(SyncSourcesInit());
    }

    void refreshAppList()
    {
        isAppListRefreshing = true;
        bool pastSelectedAppFound = false;
        audioEndpointsJson = GetAudioInfo();
        AppSelectionDropdown.ClearOptions();
        AppSelectionMessages = new List<Dropdown.OptionData>();
        AppSelectionIndex = 0;
        var appDropdownLabel = new Dropdown.OptionData("Select App to Be Played");
        AppSelectionDropdown.options.Insert(0, appDropdownLabel);
        AppSelectionMessages.Add(appDropdownLabel);
        AppSelectionDropdown.captionText.text = "Select App to Be Played";
        foreach (var endpoint in audioEndpointsJson.endpoints)
        {
            foreach (var session in endpoint.sessions)
            {
                if (!AppSelectionMessages.Any(m => m.text == session.exeName) && session.exeName != "Echo Speaker System" && session.exeName != "NVIDIA RTX Voice")
                {
                    AppSelectionData = new Dropdown.OptionData();
                    AppSelectionData.text = session.exeName;
                    AppSelectionMessages.Add(AppSelectionData);
                    AppSelectionDropdown.options.Add(AppSelectionData);
                    AppSelectionIndex = AppSelectionMessages.Count - 1;
                    if (session.exeName == appExeName)
                    {
                        pastSelectedAppFound = true;
                        AppSelectionDropdown.value = AppSelectionIndex;
                    }
                }
            }
        }
        if (!pastSelectedAppFound)
        {
            AppSelectionDropdown.value = 0;
        }
        AppSelectionDropdown.enabled = false;
        AppSelectionDropdown.enabled = true;
        AppSelectionDropdown.RefreshShownValue();
        isAppListRefreshing = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //m_MyAudioSource = GetComponent<AudioSource>();
        //TODO: replace
        if (1 == 0)
        {
            Cleanup();
        }
        else
        {
            if (!hasCleanedUp && (pos = Microphone.GetPosition(inputName)) > (FREQUENCY / 2))
            {
                if (lastPos > pos) lastPos = 0;
                if (pos - lastPos > 0)
                {
                    // Allocate the space for the sample.
                    if (!goalHornPlaying && loops % 100 == 1)
                    {
                        float[] sample = new float[(pos - lastPos) * 1];

                        // Get the data from microphone.
                        masterClip.GetData(sample, lastPos);
                        float clipLoudness = 0f;
                        float highest = 0.0f;
                        int zeroCount = 0;
                        for (int i = 0; i < sample.Length; i++)
                        {
                            clipLoudness += Mathf.Abs(sample[i]);
                            if (sample[i] == 0.0f)
                            {
                                zeroCount++;
                            }
                            // if (sample[i] > highest)
                            // {
                            //     highest = sample[i];
                            // }
                            // sample[i] = sample[i] * 1.95f;
                            // if (sample[i] > 1.0f)
                            // {
                            //     UnityEngine.Debug.Log(sample[i]);
                            //     sample[i] = 1.0f;
                            // }
                        }
                        if (((float)zeroCount / (float)sample.Length) > 0.65f )
                        {
                            //UnityEngine.Debug.Log((float)zeroCount/(float)sample.Length);
                            // for (int i = 0; i < sample.Length; i++)
                            // {
                            //     sample[i] = 0.0f;
                            // }
                            // masterClip.SetData(sample, lastPos);
                            if(!clipZeroed){
                                StartCoroutine(StartFade(0.5f, 0));
                                clipZeroed = true;
                            }else{
                                StartCoroutine(StartFade(0.5f, 1));
                                clipZeroed = false;
                            }
                        }
                        else
                        {
                            if(clipZeroed){
                                StartCoroutine(StartFade(0.5f, 1));
                                clipZeroed = false;
                            }
                            //UnityEngine.Debug.Log(averageMusicLoudness);
                            float avgSampleLoudness = ((float)clipLoudness / (float)sample.Length);
                            if ((musicLoudnessAcc + avgSampleLoudness < float.MaxValue) && musicLoudnessCount + 1 < long.MaxValue)
                            {
                                musicLoudnessAcc += avgSampleLoudness;
                                musicLoudnessCount++;
                            }
                            else
                            {
                                musicLoudnessAcc = averageMusicLoudness + avgSampleLoudness;
                                musicLoudnessCount = 2;
                            }
                            averageMusicLoudness = musicLoudnessAcc / musicLoudnessCount;
                        }
                    }
                    if (isReady)
                    {
                        if (playerListener.GoalDetected)
                        {
                            if(goalHornPlaying || !isGoalHornEnabled){
                                playerListener.GoalDetected = false;
                            }else{
                                // previousTimeSamples = masterSpeaker.timeSamples;
                                // speakerEchos[masterSpeaker.name].delay = 0f;
                                // masterSpeaker.Stop();
                                // masterSpeaker.volume *= 0.5f;
                                // masterSpeaker.clip = goalHornClip;
                                // masterSpeaker.loop = false;
                                if(clipZeroed){
                                    // StartCoroutine(StartFade(0.25f, 1));
                                    clipZeroed = false;
                                }
                                goalHornClipVolMult = averageMusicLoudness == 0.0f ? 0.3f : ((averageMusicLoudness *goalHornVolumeUserMult) / averageGoalHornLoudness);//+ 0.03225f;
                                foreach (AudioSource aSource in speakers)
                                {
                                    // speakerEchos[aSource.name].delay = 0f;
                                    // aSource.Stop();
                                    aSource.volume *= goalHornClipVolMult;
                                    aSource.dopplerLevel = 0.005f;
                                    // aSource.clip = goalHornClip;
                                    aSource.loop = false;
                                }
                                StartCoroutine(SyncSourcesGoalHorn());
                                goalHornPlaying = true;
                                playerListener.GoalDetected = false;
                                StartCoroutine(SyncSourcesAfterGoal());
                            }
                        }
                        if (!reverseLoopOrder)
                        {
                            foreach (AudioSource aSource in speakers)
                            {
                                //aSource.clip.SetData(sample, lastPos);
                                //aSource.clip = masterClip;
                                float dist = UnityEngine.Vector3.Distance(aSource.transform.position, playerListener.transform.position) * (speedOfSoundMultiplier * speakerDelays[aSource.name]);//1.19f; //1.142f;
                                speakerEchos[aSource.name].delay = dist;
                                // if(loops > 300){
                                //     aSource.Pause();
                                // }
                                if (!aSource.isPlaying) { aSource.Play(); }
                                reverseLoopOrder = true;
                            }
                            //masterSpeaker.clip.SetData(sample, lastPos);
                            //masterSpeaker.clip = masterClip;
                            // float Mastdist = UnityEngine.Vector3.Distance(masterSpeaker.transform.position, playerListener.transform.position) * (speedOfSoundMultiplier * speakerDelays[masterSpeaker.name]);// 1.19f; ;
                            // speakerEchos[masterSpeaker.name].delay = Mastdist;
                            if (!masterSpeaker.isPlaying) { masterSpeaker.Play(); }
                        }
                        else
                        {
                            foreach (AudioSource aSource in Enumerable.Reverse(speakers))
                            {
                                //aSource.clip.SetData(sample, lastPos);
                                //aSource.clip = masterClip;
                                float dist = UnityEngine.Vector3.Distance(aSource.transform.position, playerListener.transform.position) * (speedOfSoundMultiplier * speakerDelays[aSource.name]);// 1.19f;
                                speakerEchos[aSource.name].delay = dist;
                                aSource.dopplerLevel = 0.005f;
                                // if(loops > 300){
                                //     aSource.Pause();
                                // }
                                if (!aSource.isPlaying) { aSource.Play(); }
                                //aSource.timeSamples = masterSpeaker.timeSamples;
                                reverseLoopOrder = false;
                            }
                            //masterSpeaker.clip.SetData(sample, lastPos);
                            //masterSpeaker.clip = masterClip;
                            // float Mastdist = UnityEngine.Vector3.Distance(masterSpeaker.transform.position, playerListener.transform.position) * (speedOfSoundMultiplier * speakerDelays[masterSpeaker.name]);//  1.19f;
                            // speakerEchos[masterSpeaker.name].delay = Mastdist;
                            if (!masterSpeaker.isPlaying) { masterSpeaker.Play(); }
                        }

                        float playerXAbs = Math.Abs(playerListener.HeadPos.x);
                        if (playerXAbs > 40f)
                        {
                            float vol = Map(playerXAbs, 40.0001f, 90f, 0.01f, 0.79f);
                            AudioListener.volume = listenerVolume * (0.49f + (Mathf.Log10(vol) / -4.0f));//41/(playerXAbs);// Mathf.Log10((41/(Math.Abs(playerListener.head.position.x)))*(41/(Math.Abs(playerListener.head.position.x))) * 20) - 0.29f; //
                            //UnityEngine.Debug.Log(listenerVolume);                                                          //Debug.Log(AudioListener.volume);
                            if(!inSpawnRoom){
                                foreach (AudioReverbFilter reverb in speakerReverbs.Values)
                                {
                                    reverb.reverbPreset = spawnRoomReverbPreset;
                                }
                                inSpawnRoom = true;
                            }
                            vol = Map(playerXAbs, 40.0001f, 76f, 0.0001f, 1.0f);
                            playerListernerLowPass.cutoffFrequency = 2000 + ((Mathf.Log10(vol) / -4.0f) * 16000f);

                        }
                        else
                        {
                            //float vol2 = Map(40.001f, 40.0001f, 90f, 0.004f, 1.0f);
                            //AudioListener.volume = Mathf.Log10(vol) / -4.0f;//41/(playerXAbs);// Mathf.Log10((41/(Math.Abs(playerListener.head.position.x)))*(41/(Math.Abs(playerListener.head.position.x))) * 20) - 0.29f; //
                            //Debug.Log(Mathf.Log10(vol2) / -4.0f);
                            //UnityEngine.Debug.Log(listenerVolume);
                            if(inSpawnRoom){
                                foreach (AudioReverbFilter reverb in speakerReverbs.Values)
                                {
                                    reverb.reverbPreset = globalReverbPreset;
                                }
                                inSpawnRoom = false;
                            }
                            AudioListener.volume = listenerVolume;
                            playerListernerLowPass.cutoffFrequency = 22000f;
                        }
                        if (playerListener.HeadPos.x != -105.5 && (playerXAbs > 72))
                        {
                            if (!respawnResetDone)
                            {
                                if (!goalHornPlaying)
                                {
                                    StartCoroutine(SyncSources());
                                }
                                respawnResetDone = true;
                            }
                        }
                        else
                        {
                            respawnResetDone = false;
                        }
                        if (loops > 36000)
                        {
                            StartCoroutine(SyncSources());
                        }
                        else
                        {
                            loops++;
                        }
                    }

                    // Put the data in the audio source.


                    lastPos = pos;
                }
                if(AppSelectionDropdownMouseOver.isOver) {
                    AppSelectionDropdownMouseOver.isOver = false;
                    refreshAppList();
                }
            }

            // foreach(AudioSource aSource in speakers){
            //     //aSource.clip.SetData(sample, lastPos);
            //     // if(loops > 300){
            //     //     aSource.Pause();
            //     // }
            //     //if(!aSource.isPlaying){aSource.Play();}
            //     aSource.timeSamples = masterSpeaker.timeSamples;
            //     //reverseLoopOrder = true;
            // }
        }
    }

    void InputDropdownValueChanged(Dropdown change)
    {
        if (!isAppListRefreshing)
        {
            var newInput = AudioInputDropdown.options[AudioInputDropdown.value].text;
            var audioCableOutput = audioEndpointsJson.endpoints.FirstOrDefault(e => e.name == newInput);

            if (audioCableOutput == null)
            {
                AppSelectionDropdown.interactable = false;
                var session = audioEndpointsJson.endpoints
                .SelectMany(e => e.sessions)
                .Where(s => s.exeName == appExeName)
                .FirstOrDefault();
                if (session != null)
                {
                    StartCoroutine(resetAppToOriginalEndpoint(session.processId));
                }
            }
            else
            {
                AppSelectionDropdown.interactable = true;
                if (AppSelectionDropdown.value != 0)
                {
                    var session = audioEndpointsJson.endpoints
                    .SelectMany(e => e.sessions)
                    .Where(s => s.exeName == appExeName)
                    .FirstOrDefault();
                    if (session != null)
                    {
                        StartCoroutine(setAppToVAC(session.processId, newInput, false));
                    }
                }
            }
            Microphone.End(inputName);
            PlayerPrefs.SetString("InputName", newInput);
            PlayerPrefs.Save();
            inputName = newInput;
            sourceInit();
        }
    }

    void ReverbPresetDropdownValueChanged(Dropdown change)
    {
        var newPreset = ReverbPresetDropdown.options[ReverbPresetDropdown.value].text;
        AudioReverbPreset newPresetEnum;

        if (Enum.TryParse<AudioReverbPreset>(newPreset, out newPresetEnum))
        {
            if(newPresetEnum != globalReverbPreset && isReady){
                PlayerPrefs.SetInt("GlobalReverbPreset", (int)newPresetEnum);
                PlayerPrefs.Save();
                globalReverbPreset = newPresetEnum;
                if(!inSpawnRoom && !goalHornPlaying){
                    foreach (AudioReverbFilter reverb in speakerReverbs.Values)
                    {
                        reverb.reverbPreset = globalReverbPreset;
                    }
                }
            }
        }
    }

    void SpawnRoomReverbPresetDropdownValueChanged(Dropdown change)
    {
        var newPreset = SpawnRoomReverbPresetDropdown.options[SpawnRoomReverbPresetDropdown.value].text;
        AudioReverbPreset newPresetEnum;

        if (Enum.TryParse<AudioReverbPreset>(newPreset, out newPresetEnum))
        {
            if(newPresetEnum != spawnRoomReverbPreset && isReady){
                PlayerPrefs.SetInt("SpawnRoomReverbPreset", (int)newPresetEnum);
                PlayerPrefs.Save();
                spawnRoomReverbPreset = newPresetEnum;
                if(inSpawnRoom && !goalHornPlaying){
                    foreach (AudioReverbFilter reverb in speakerReverbs.Values)
                    {
                        reverb.reverbPreset = spawnRoomReverbPreset;
                    }
                }
            }
        }
    }

    public void GoalHornVolMultChanged(float sliderValue) {
         goalHornVolumeUserMult = (0.5f +(sliderValue * 0.05f));
         PlayerPrefs.SetFloat("GoalHornVolumeUserMult", goalHornVolumeUserMult);
         PlayerPrefs.Save();
    }

    public void GoalHornTimeChanged(float sliderValue) {
        goalHornMaxDuration = sliderValue;
        PlayerPrefs.SetFloat("GoalHornMaxDuration", goalHornMaxDuration);
        PlayerPrefs.Save();
    }
    void AppSelectionDropdownValueChanged(Dropdown change)
    {
        var newAppSource = AppSelectionDropdown.options[AppSelectionDropdown.value].text;
        if (isFirstAppInit || newAppSource != appExeName)
        {
            isFirstAppInit = false;
            var oldSession = audioEndpointsJson.endpoints
                .SelectMany(e => e.sessions)
                .Where(s => s.exeName == appExeName)
                .FirstOrDefault();
            if (oldSession != null)
            {
                AppEndpoint originalEndpoint = originalAppEndpoints.FirstOrDefault(appEP => appEP.processId == oldSession.processId);
                if (originalEndpoint != null)
                {
                    StartCoroutine(resetAppToOriginalEndpoint(oldSession.processId));
                }
            }
            if (AppSelectionDropdown.value == 0)
            {
                appExeName = "";
                PlayerPrefs.SetString("AppSourceName", appExeName);
                PlayerPrefs.Save();
            }
            else
            {
                PlayerPrefs.SetString("AppSourceName", newAppSource);
                PlayerPrefs.Save();
                appExeName = newAppSource;
                var newSession = audioEndpointsJson.endpoints
                .SelectMany(e => e.sessions)
                .Where(s => s.exeName == appExeName)
                .FirstOrDefault();
                if (newSession != null)
                {
                    StartCoroutine(setAppToVAC(newSession.processId, inputName));
                    Microphone.End(inputName);
                    sourceInit();
                }
            }
        }
    }

    void OpenWinAppAudioSettings()
    {
        Application.OpenURL("ms-settings:apps-volume");
    }
    void DownloadLatestRelease()
    {
        try
        {
            updateFileName = "EchoSpeakerSystemInstall_" + latestReleaseVer + ".exe";
            WebClient webClient = new WebClient();
            webClient.DownloadFileCompleted += Completed;
            webClient.DownloadFileAsync(new Uri(latestReleaseURL), Path.GetTempPath() + updateFileName);
        }
        catch
        {

        }
        UpdateDownloadBtnGameObject.SetActive(false);
    }
    private void Completed(object sender, AsyncCompletedEventArgs e)
    {

        Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Path.GetTempPath(), updateFileName),
            UseShellExecute = true
        });

        Application.Quit();
    }
    void OpenVACDownload()
    {
        Application.OpenURL("https://software.muzychenko.net/freeware/vac464lite.zip");
        VACDownloadBtnGameObject.SetActive(false);
    }
    private IEnumerator SyncSourcesInit()
    {
        //speakerEchos[masterSpeaker.name].delay = 0f;
        yield return new WaitForSeconds(2);
        masterSpeaker.clip = null;
        masterSpeaker.clip = masterClip;
        foreach (AudioSource aSource in speakers)
        {
            //speakerEchos[aSource.name].delay = 0f;
            aSource.clip = null;
            aSource.clip = masterClip;
            aSource.timeSamples = masterSpeaker.timeSamples;
            aSource.mute = false;
        }
        yield return null;
        foreach (AudioReverbFilter reverb in speakerReverbs.Values)
        {
            reverb.reverbPreset = globalReverbPreset;
        }
        loops = 0;
        isReady = true;
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(StartFade(3, 1));
    }

    private IEnumerator SyncSourcesAfterGoal()
    {
        //speakerEchos[masterSpeaker.name].delay = 0f;
        float waitTime = goalHornMaxDuration;
        if (goalHornClip.length < waitTime)
        {
            waitTime = goalHornClip.length - 2.55f;
        }
        yield return new WaitForSeconds(waitTime);
        StartCoroutine(StartFade(2.5f, 0.001f));
        yield return new WaitForSeconds(2.48f);
        // float currentTime = 0;
        // float currentVol = listenerVolume;
        // float targetValue = Mathf.Clamp(0.3f, 0.0001f, 1);

        // while (currentTime < 3f)
        // {
        //     currentTime += Time.deltaTime;
        //     float newVol = Mathf.Lerp(currentVol, targetValue, currentTime / 3f);
        //     listenerVolume = newVol;
        //     UnityEngine.Debug.Log(listenerVolume);
        //     yield return null;
        // }
        // yield break;
        // masterSpeaker.volume *= 2f;
        // masterSpeaker.timeSamples = lastPos;
        // masterSpeaker.clip = masterClip;
        // masterSpeaker.loop = true;
        StartCoroutine(StartFade(0.15f, 0));

        foreach (AudioSource aSource in speakers)
        {
            if(!inSpawnRoom){
                speakerReverbs[aSource.name].reverbPreset = globalReverbPreset;
            }
            speakerEchos[aSource.name].delay = 0f;
            aSource.dopplerLevel = 0.0f;
            //aSource.clip = null;
            aSource.loop = true;
            aSource.clip = masterClip;
            aSource.timeSamples = masterSpeaker.timeSamples;
            aSource.volume *= 1f / goalHornClipVolMult;
        }
        goalHornPlaying = false;
        loops = 0;
        isReady = true;
        goalHornToggle.enabled = true;
        goalHornVolMultSlider.enabled = true;
        goalHornTimeSlider.enabled = true;
        yield return null;
        // foreach (AudioSource aSource in speakers)
        // {
        //     // aSource.clip = masterClip;
        //     aSource.loop = true;
        //     //aSource.timeSamples = masterSpeaker.timeSamples;
        //     aSource.volume *= 3f;
        // }
        StartCoroutine(StartFade(2.5f, 1));
        // StartCoroutine(SyncSources());
    }
    private IEnumerator ResetAudio()
    {
        //  while (true)
        isReady = false;
        Microphone.End(inputName);
        //PlayerPrefs.SetString("InputName", newInput);
        //PlayerPrefs.Save();
        //inputName = newInput;
        sourceInit();
        yield return null;
        // //  {
        //      foreach (AudioSource aSource in speakers)
        //      {
        //          aSource.timeSamples = masterSpeaker.timeSamples;
        //          yield return null;
        //      }
        //      loops = 0;
        //  }
    }
    private IEnumerator SyncSources()
    {
        bool shouldSetVol = listenerVolume != 0.0f;
        StartCoroutine(StartFade(0.15f, 0));
        //  while (true)
        //  {
        //previousTimeSamples = masterSpeaker.timeSamples;
        //masterSpeaker.clip = null;
        // speakerEchos[masterSpeaker.name].delay = 0f;
        //masterSpeaker.clip = masterClip;
        //masterSpeaker.timeSamples = previousTimeSamples;
        foreach (AudioSource aSource in speakers)
        {
            speakerEchos[aSource.name].delay = 0f;
            aSource.dopplerLevel = 0.0f;
            //aSource.clip = null;
            aSource.clip = masterClip;
            aSource.timeSamples = masterSpeaker.timeSamples;
        }
        if(shouldSetVol){
            StartCoroutine(StartFade(0.15f, 1));
        }
        loops = 0;
        isReady = true;
        yield return null;
        //  }
    }

    private IEnumerator SyncSourcesGoalHorn()
    {
        goalHornToggle.enabled = false;
        goalHornVolMultSlider.enabled = false;
        goalHornTimeSlider.enabled = false;
        StartCoroutine(StartFade(0.15f, 0));

        //  while (true)
        //  {
        //masterSpeaker.clip = null;
        // speakerEchos[masterSpeaker.name].delay = 0f;
        // masterSpeaker.clip = goalHornClip;
        // masterSpeaker.timeSamples = 0;
        // foreach (AudioReverbFilter reverb in speakerReverbs.Values)
        // {
        //     reverb.reverbPreset = AudioReverbPreset.Generic;
        // }
        foreach (AudioSource aSource in speakers)
        {
            speakerReverbs[aSource.name].reverbPreset = AudioReverbPreset.Generic;
            speakerEchos[aSource.name].delay = 0f;
            // aSource.Stop();
            aSource.dopplerLevel = 0f;
            aSource.clip = goalHornClip;
            aSource.timeSamples = 13000;
        }
        StartCoroutine(StartFade(0.15f, 1));
        // foreach (AudioSource aSource in speakers)
        // {
        //     aSource.Play();                        
        // }

        loops = 0;
        isReady = true;
        yield return null;
        //  }
    }

    public IEnumerator StartFade(float duration, float targetVolume)
    {
        float currentTime = 0;
        float currentVol = listenerVolume;
        float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float newVol = Mathf.Lerp(currentVol, targetValue, currentTime / duration);
            listenerVolume = newVol;
            //UnityEngine.Debug.Log(listenerVolume);
            yield return null;
        }
        yield break;
    }

    private IEnumerator setAppToVAC(int procID, string input, bool retainOriginalEndpoint = true)
    {
        var audioCableOutput = audioEndpointsJson.endpoints.FirstOrDefault(e => e.name == input);
        if (audioCableOutput != null)
        {
            Process AudioSwitch = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = (Application.streamingAssetsPath + "\\AudioSwitch.exe"),
                    Arguments = procID + " \"" + audioCableOutput.id + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }

            };
            AudioSwitch.Start();
            AudioSwitch.WaitForExit(900);
            string line = AudioSwitch.StandardOutput.ReadLine();
            if (AudioSwitch.ExitCode == 0)
            {
                wasAppEndpointChanged = true;
                if (retainOriginalEndpoint)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        originalAppEndpoints.Add(new AppEndpoint { processId = procID, originalEndpointID = "" });
                    }
                    else
                    {
                        originalAppEndpoints.Add(new AppEndpoint { processId = procID, originalEndpointID = line });
                    }
                }
            }
        }
        StartCoroutine(SyncSources());
        yield return null;
    }
    private IEnumerator resetAppToOriginalEndpoint(int procID)
    {
        AppEndpoint originalEndpoint = originalAppEndpoints.FirstOrDefault(appEP => appEP.processId == procID);
        if (originalEndpoint != null)
        {
            Process AudioSwitch = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = (Application.streamingAssetsPath + "\\AudioSwitch.exe"),
                    Arguments = procID + " \"" + originalEndpoint.originalEndpointID + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }

            };
            AudioSwitch.Start();
            AudioSwitch.WaitForExit(900);
            wasAppEndpointChanged = false;
            originalAppEndpoint = "";
            originalAppEndpoints.Remove(originalEndpoint);
        }
        yield return null;
    }

    void OnApplicationQuit()
    {
        try
        {
            Cleanup();

        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log("Disconnect failed");
        }
    }

    void Cleanup()
    {
        try
        {
            if (!hasCleanedUp)
            {
                Microphone.End(inputName);
                if (wasAppEndpointChanged)
                {
                    var Originalsession = audioEndpointsJson.endpoints
                .SelectMany(e => e.sessions)
                .Where(s => s.exeName == appExeName)
                .FirstOrDefault();
                    if (Originalsession != null)
                    {
                        Process AudioSwitch = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = (Application.streamingAssetsPath + "\\AudioSwitch.exe"),
                                Arguments = Originalsession.processId + " \"" + originalAppEndpoint + "\"",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardInput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }

                        };
                        AudioSwitch.Start();
                        AudioSwitch.WaitForExit(900);
                        wasAppEndpointChanged = false;
                        originalAppEndpoint = "";
                        hasCleanedUp = true;
                    }
                }
            }
        }
        catch (Exception ex) { }
    }
    IEnumerator GetLatestVer()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get("https://api.github.com/repos/iblowatsports/Echo-VR-Speaker-System/releases/latest"))
        {
            // Request and wait for the desired page. 73

            yield return webRequest.SendWebRequest();
            try
            {
                if (webRequest.isNetworkError)
                {
                }
                else
                {
                    string resp = webRequest.downloadHandler.text;
                    VersionJson latestVersion = JsonUtility.FromJson<VersionJson>(resp);
                    
                    if (latestVersion.tag_name != VERSION_TAGNAME)
                    {
                        latestReleaseVer = latestVersion.tag_name;
                        latestReleaseURL = latestVersion.assets.First(url => url.browser_download_url.EndsWith("exe")).browser_download_url;
                        UpdateDownloadBtn.onClick.AddListener(delegate
                        {
                            DownloadLatestRelease();
                        });
                        UpdateDownloadBtnGameObject.SetActive(true);
                    }
                }
            }
            catch
            {

            }
        }
    }

    IEnumerator GetWhatsNew()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get("https://api.github.com/repos/iblowatsports/Echo-VR-Speaker-System/releases"))
        {
            // Request and wait for the desired page. 73

            yield return webRequest.SendWebRequest();
            try
            {
                if (webRequest.isNetworkError)
                {
                }
                else
                {
                    string resp = webRequest.downloadHandler.text;
                    VersionJson[] releases = JsonHelper.FromJson<VersionJson>(resp);
                    
                    VersionJson thisRelease = releases.FirstOrDefault(r => r.tag_name == VERSION_TAGNAME);
                    if(thisRelease != null){
                        GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "WhatsNewText").GetComponent<Text>().text = thisRelease.body.Replace("**","").Replace(" _"," ").Replace("_ ", " ");
                        GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "WhatsNewTitle").GetComponent<Text>().text = "What's New (" + VERSION_TAGNAME + ")";
                    if(isNewUpdate){
                        PlayerPrefs.SetString("RunningVersion", VERSION_TAGNAME);
                        PlayerPrefs.Save();
                        GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "WhatsNewPopup").gameObject.SetActive(true);
                    }
                    else{
                        GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "WhatsNewPopup").gameObject.SetActive(false);
                    }
                    }
                }
            }
            catch(Exception ex)
            {
                GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "WhatsNewTitle").GetComponent<Text>().text = "What's New (" + VERSION_TAGNAME + ")";
                GameObject.Find("UICanvas").transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "WhatsNewPopup").gameObject.SetActive(false);
            }
        }
    }

    float Map(float s, float a1, float a2, float b1, float b2)
    {
        return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
    }
}

public static class JsonHelper
    {
        public static T[] FromJson<T>(string jsonArray)
        {
            jsonArray = WrapArray (jsonArray);
            return FromJsonWrapped<T> (jsonArray);
        }
 
        public static T[] FromJsonWrapped<T> (string jsonObject)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(jsonObject);
            return wrapper.items;
        }
 
        private static string WrapArray (string jsonArray)
        {
            return "{ \"items\": " + jsonArray + "}";
        }
 
        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.items = array;
            return JsonUtility.ToJson(wrapper);
        }
 
        public static string ToJson<T>(T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.items = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }
 
        [Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }
    }
[System.Serializable]
public class VersionJson
{
    public string tag_name;
    public Author author;
    public string html_url;
    public Asset[] assets;
    public string body;
}
[System.Serializable]
public class Asset
{
    public string browser_download_url;
    public Uploader uploader;
}
[System.Serializable]
public class Author
{
    public string html_url;
}
[System.Serializable]
public class Uploader
{
    public string html_url;
}

//Serializable classes for JSON serializing from the API output.
[System.Serializable]
public class Game
{
    public bool isNewstyle;
    public float caprate;
    public long nframes;
    public Frame[] frames;
}
[System.Serializable]
public class Stats
{
    public int possession_time;
    public int points;
    public int goals;
    public int saves;
    public int stuns;
    public int interceptions;
    public int blocks;
    public int passes;
    public int catches;
    public int steals;
    public int assists;
    public int shots_taken;
}
[System.Serializable]
public class Last_Score
{
    public float disc_speed;
    public string team;
    public string goal_type;
    public int point_amount;
    public float distance_thrown;
    public string person_scored;
    public string assist_scored;
}
[System.Serializable]
public class Frame
{
    public Disc disc;
    public double frameTimeOffset;

    public string sessionid;
    public int orange_points;
    public bool private_match;
    public string client_name;
    public string game_clock_display;
    public string game_status;
    public float game_clock;
    public string match_type;

    public Team[] teams;

    public string map_name;
    public int[] possession;
    public bool tournament_match;
    public int blue_points;

    public Last_Score last_score;


}
[System.Serializable]
public class Disc
{
    public float[] position;
    public float[] velocity;
    public int bounce_count;
}
[System.Serializable]
public class Team
{
    public Player[] players;
    public string team;
    public bool possession;
    public Stats stats;

}
[System.Serializable]
public class Player
{
    public string name;
    // public float[] rhand;
    public int playerid;
    public Head head;
    public Body body;
    public Lhand lhand;
    public Rhand rhand;
    public float[] position;
    // public float[] lhand;
    public long userid;
    public Stats stats;
    public int number;
    public int level;
    public bool possession;
    // public float[] left;
    public bool invulnerable;
    // public float[] up;
    // public float[] forward;
    public bool stunned;
    public float[] velocity;
    public bool blocking;
}
[System.Serializable]
public class Head
{
    public float[] position;
    public float[] left;
    public float[] up;
    public float[] forward;
}
[System.Serializable]
public class Body
{
    public float[] position;
    public float[] left;
    public float[] up;
    public float[] forward;
}
[System.Serializable]
public class Lhand
{
    public float[] pos;
    public float[] left;
    public float[] up;
    public float[] forward;
}
[System.Serializable]
public class Rhand
{
    public float[] pos;
    public float[] left;
    public float[] up;
    public float[] forward;
}

[System.Serializable]
public class MatchEvent
{
    public string EventTypeName;
    public EventData[] Data;
}

[System.Serializable]
public class EventData
{
    public string Key;
    public string Value;
}

[System.Serializable]
public class AudioEndpoints
{
    public Endpoint[] endpoints;
}
[System.Serializable]
public class Endpoint
{
    public Session[] sessions;
    public string name;
    public string id;
}
[System.Serializable]
public class Session
{
    public string exeName;
    public int processId;
}

public class AppEndpoint
{
    public string originalEndpointID;
    public int processId;
}

public class PlayerStats : Stats
{
    public PlayerStats(int pt, int points, int goals, int saves, int stuns, int interceptions, int blocks, int passes, int catches, int steals, int assists, int shots_taken)
    {
        this.possession_time = pt;
        this.points = points;
        this.goals = goals;
        this.saves = saves;
        this.stuns = stuns;
        this.interceptions = interceptions;
        this.blocks = blocks;
        this.passes = passes;
        this.catches = catches;
        this.steals = steals;
        this.assists = assists;
        this.shots_taken = shots_taken;
    }
}