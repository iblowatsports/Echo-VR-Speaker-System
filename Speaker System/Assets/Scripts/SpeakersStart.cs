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

public class SpeakersStart : MonoBehaviour
{
    string VERSION_TAGNAME = "v0.3.2";

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
    public AudioSource Source1;
    string inputName = "";
    bool isFirstAppInit = true;
    string appExeName = "";
    string originalAppEndpoint = "";
    bool wasAppEndpointChanged = false;
    public bool hasCleanedUp = false;
    List<AppEndpoint> originalAppEndpoints = new List<AppEndpoint>();
    AudioSource masterSpeaker;
    List<AudioSource> speakers = new List<AudioSource>();

    AudioEchoFilter masterSpeakerEcho;
    List<AudioEchoFilter> speakerFilters = new List<AudioEchoFilter>();

    string latestReleaseURL = "";
    string latestReleaseVer = "";
    int speakerCount = 18;
    bool reverseLoopOrder;
    SpatialPlayerListener playerListener;
    AudioListener playerAudioListener;
    AudioLowPassFilter playerListernerLowPass;
    Dropdown.OptionData AudioInputData, AppSelectionData;
    AudioEndpoints audioEndpointsJson = null;
    List<Dropdown.OptionData> AudioInputMessages = new List<Dropdown.OptionData>();
    List<Dropdown.OptionData> AppSelectionMessages = new List<Dropdown.OptionData>();
    Dropdown AudioInputDropdown, AppSelectionDropdown;
    int AudioInputIndex, AppSelectionIndex;
    GameObject VACDownloadBtnGameObject, UpdateDownloadBtnGameObject;
    Button MSSettingsBtn, VACDownloadBtn, UpdateDownloadBtn, RefreshAppListBtn;
    string VACInputName = "(Virtual Audio Cable)";
    const int FREQUENCY = 48000;
    AudioClip mic;
    int lastPos, pos;
    int loops;
    bool respawnResetDone;

    // Use this for initialization
    void Start()
    {

        Application.targetFrameRate = 25;
        QualitySettings.vSyncCount = 0;

        inputName = PlayerPrefs.GetString("InputName", "Line 1 (Virtual Audio Cable)");
        appExeName = PlayerPrefs.GetString("AppSourceName", "");
        AudioInputDropdown = GameObject.Find("AudioSourceDropdown").GetComponent<Dropdown>();
        AudioInputDropdown.ClearOptions();
        AppSelectionDropdown = GameObject.Find("AppSelectionDropdown").GetComponent<Dropdown>();
        MSSettingsBtn = GameObject.Find("OpenMSAppAudioSettingsBtn").GetComponent<Button>();
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
        GameObject playerObject = GameObject.Find("Player Listener");
        playerListener = playerObject.GetComponent<SpatialPlayerListener>();
        if (!playerListener.isIgniteBotEmbedded)
        {
            StartCoroutine(GetLatestVer());
        }
        playerAudioListener = playerObject.GetComponent<AudioListener>();
        playerListernerLowPass = playerAudioListener.GetComponent<AudioLowPassFilter>();
        masterSpeaker = GameObject.Find("Speaker 1").GetComponent<AudioSource>();
        for (int i = 2; i <= speakerCount; i++)
        {
            speakers.Add(GameObject.Find("Speaker " + i).GetComponent<AudioSource>());
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
        AppSelectionDropdown.onValueChanged.AddListener(delegate
        {
            AppSelectionDropdownValueChanged(AppSelectionDropdown);
        });
        refreshAppList();
        if (!defaultFound && !VACFound)
        {
            Error("Couldn't find audio device " + inputName + ". Make sure to install Virtual Audio Cable and set the output device of your music source to " + inputName + " in the Windows settings under 'App Volume and Device Settings'.", "Error");
            VACDownloadBtnGameObject.SetActive(true);
            VACDownloadBtn.onClick.AddListener(delegate
            {
                OpenVACDownload();
            });
        }

        sourceInit();
    }

    void sourceInit()
    {

        mic = Microphone.Start(inputName, true, 300, FREQUENCY);
        reverseLoopOrder = false;
        loops = 0;
        var clip = AudioClip.Create("test", 300 * FREQUENCY, 1, FREQUENCY, false);
        masterSpeaker.clip = clip;
        masterSpeaker.loop = true;
        foreach (AudioSource aSource in speakers)
        {
            aSource.clip = clip;
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
        if (playerListener.quitCalled)
        {
            Cleanup();
        }
        else
        {
            if (!hasCleanedUp && (pos = Microphone.GetPosition(inputName)) > (FREQUENCY / 2) - 1)
            {
                if (lastPos > pos) lastPos = 0;

                if (pos - lastPos > 0)
                {
                    float playerXAbs = Math.Abs(playerListener.head.position.x);
                    // Allocate the space for the sample.
                    float[] sample = new float[(pos - lastPos) * 1];

                    // Get the data from microphone.
                    mic.GetData(sample, lastPos);
                    float highest = 0.0f;
                    for (int i = 0; i < sample.Length; i++)
                    {
                        if (sample[i] > highest)
                        {
                            highest = sample[i];
                        }
                        sample[i] = sample[i] * 1.95f;
                        if (sample[i] > 1.0f)
                        {
                            UnityEngine.Debug.Log(sample[i]);
                            sample[i] = 1.0f;
                        }
                    }

                    if (!reverseLoopOrder)
                    {
                        foreach (AudioSource aSource in speakers)
                        {
                            aSource.clip.SetData(sample, lastPos);
                            AudioEchoFilter echo = aSource.GetComponent<AudioEchoFilter>();
                            float dist = Vector3.Distance(aSource.transform.position, playerListener.transform.position) * 1.142f;
                            echo.delay = dist;
                            // if(loops > 300){
                            //     aSource.Pause();
                            // }
                            if (!aSource.isPlaying) { aSource.Play(); }
                            reverseLoopOrder = true;
                        }
                        masterSpeaker.clip.SetData(sample, lastPos);
                        AudioEchoFilter MasterEcho = masterSpeaker.GetComponent<AudioEchoFilter>();
                        float Mastdist = Vector3.Distance(masterSpeaker.transform.position, playerListener.transform.position) * 1.142f; ;
                        MasterEcho.delay = Mastdist;
                        if (!masterSpeaker.isPlaying) { masterSpeaker.Play(); }
                    }
                    else
                    {
                        foreach (AudioSource aSource in Enumerable.Reverse(speakers))
                        {
                            aSource.clip.SetData(sample, lastPos);
                            AudioEchoFilter echo = aSource.GetComponent<AudioEchoFilter>();
                            float dist = Vector3.Distance(aSource.transform.position, playerListener.transform.position) * 1.142f;
                            echo.delay = dist;
                            // if(loops > 300){
                            //     aSource.Pause();
                            // }
                            if (!aSource.isPlaying) { aSource.Play(); }
                            //aSource.timeSamples = masterSpeaker.timeSamples;
                            reverseLoopOrder = false;
                        }
                        masterSpeaker.clip.SetData(sample, lastPos);
                        AudioEchoFilter MasterEcho = masterSpeaker.GetComponent<AudioEchoFilter>();
                        float Mastdist = Vector3.Distance(masterSpeaker.transform.position, playerListener.transform.position) * 1.142f;
                        MasterEcho.delay = Mastdist;
                        if (!masterSpeaker.isPlaying) { masterSpeaker.Play(); }
                    }
                    if (playerXAbs > 40f)
                    {
                        float vol = Map(playerXAbs, 40.0001f, 90f, 0.01f, 0.79f);
                        AudioListener.volume = 0.49f + (Mathf.Log10(vol) / -4.0f);//41/(playerXAbs);// Mathf.Log10((41/(Math.Abs(playerListener.head.position.x)))*(41/(Math.Abs(playerListener.head.position.x))) * 20) - 0.29f; //
                                                                                  //Debug.Log(AudioListener.volume);
                        vol = Map(playerXAbs, 40.0001f, 76f, 0.0001f, 1.0f);
                        playerListernerLowPass.cutoffFrequency = 4000 + ((Mathf.Log10(vol) / -4.0f) * 16000f);

                    }
                    else
                    {
                        //float vol2 = Map(40.001f, 40.0001f, 90f, 0.004f, 1.0f);
                        //AudioListener.volume = Mathf.Log10(vol) / -4.0f;//41/(playerXAbs);// Mathf.Log10((41/(Math.Abs(playerListener.head.position.x)))*(41/(Math.Abs(playerListener.head.position.x))) * 20) - 0.29f; //
                        //Debug.Log(Mathf.Log10(vol2) / -4.0f);
                        AudioListener.volume = 1.0f;
                        playerListernerLowPass.cutoffFrequency = 22000f;
                    }
                    if (playerListener.head.position.x != -105.5 && (playerXAbs > 72))
                    {
                        if (!respawnResetDone)
                        {
                            StartCoroutine(SyncSources());
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

                    // Put the data in the audio source.


                    lastPos = pos;
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
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C \"" + Application.streamingAssetsPath + "\\InstallEchoSpeakerSystem.bat\" " + latestReleaseURL + " " + latestReleaseVer + " 1";
            startInfo.Verb = "runas";
            startInfo.UseShellExecute = true;
            process.StartInfo = startInfo;
            process.Start();
            Application.Quit();
        }
        catch
        {

        }
        UpdateDownloadBtnGameObject.SetActive(false);
    }
    void OpenVACDownload()
    {
        Application.OpenURL("https://software.muzychenko.net/freeware/vac464lite.zip");
        VACDownloadBtnGameObject.SetActive(false);
    }
    private IEnumerator SyncSourcesInit()
    {
        yield return new WaitForSeconds(2);
        foreach (AudioSource aSource in speakers)
        {
            aSource.timeSamples = masterSpeaker.timeSamples;
            yield return null;
        }
        loops = 0;
    }
    private IEnumerator ResetAudio()
    {
        //  while (true)
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
        //  while (true)
        //  {
        foreach (AudioSource aSource in speakers)
        {
            aSource.timeSamples = masterSpeaker.timeSamples;
            yield return null;
        }
        loops = 0;
        //  }    
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
                playerListener.Cleanup();
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
            try
            {
                yield return webRequest.SendWebRequest();

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
                        latestReleaseURL = latestVersion.assets.First(url => url.browser_download_url.EndsWith("zip")).browser_download_url;
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

    float Map(float s, float a1, float a2, float b1, float b2)
    {
        return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
    }
}
[System.Serializable]
public class VersionJson
{
    public string tag_name;
    public Author author;
    public string html_url;
    public Asset[] assets;
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
