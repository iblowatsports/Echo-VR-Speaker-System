using System.IO;
using System.Net;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public static class EchoArenaAPI
{
    public static Session CurrentSession = null;
    public static string APIURL = "http://127.0.0.1:6721/session";
    public static bool Connected = false;

    public static bool MakingCall = false;

    //download raw json data from the api url and deserialize to something we can use
    public static IEnumerator MakeAPICall()
    {
        string json;

        using (UnityWebRequest webRequest = UnityWebRequest.Get(APIURL))
        {
            MakingCall = true;
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                Debug.Log("no connection");
                Connected = false;
                CurrentSession = null;
            }
            else
            {
                json = webRequest.downloadHandler.text;

                try
                {
                    CurrentSession = JsonUtility.FromJson<Session>(json);
                    Connected = true;
                }
                catch { Connected = false; CurrentSession = null; Debug.Log("Failed to parse json!"); }
            }
            MakingCall = false;
        }
    }

    public class Session
    {
        #region disc
        public class Disc
        {
            public float[] position;
            public float[] forward;
            public float[] velocity;
            public int bounce_count;
        }
        public Disc disc;
        #endregion
        #region loose bits
        public string sessionid;
        public int orange_team_restart_request;
        public string sessionip;
        public string game_status;
        public string game_clock_display;
        public float game_clock;
        public string match_type;
        public string map_name;
        public bool private_match;
        public int orange_points;
        public bool tournament_match;
        public int blue_team_restart_request;
        public string client_name;
        public int blue_points;
        #endregion
        #region pause
        public class Pause
        {
            public string paused_state;
            public string unpaused_team;
            public string paused_requested_team;
            public float unpaused_timer;
            public float paused_timer;
        }
        public Pause pause;
        #endregion
        #region last_score
        public class LastScore
        {
            public float disc_speed;
            public string team;
            public string goal_type;
            public int point_amount;
            public float distance_thrown;
            public string person_scored;
            public string assist_scored;
        }
        public LastScore last_score;
        #endregion

        #region teams & players
        public Team[] teams;

        public class Team
        {
            public Player[] players;
            public string team;
            public bool possession;
            public Stats stats;

        }
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
        public class Stats
        {
            public float possession_time;
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
        public class Head
        {
            public float[] position;
            public float[] left;
            public float[] up;
            public float[] forward;
        }
        public class Body
        {
            public float[] position;
            public float[] left;
            public float[] up;
            public float[] forward;
        }
        public class Lhand
        {
            public float[] pos;
            public float[] left;
            public float[] up;
            public float[] forward;
        }
        public class Rhand
        {
            public float[] pos;
            public float[] left;
            public float[] up;
            public float[] forward;
        }
        #endregion
    }
}
