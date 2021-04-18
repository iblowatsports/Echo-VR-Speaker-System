using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerListener : MonoBehaviour
{
    public EchoArenaAPI.Session.Player player;
    public Transform defaultPos;
    public Vector3 HeadPos;

    public bool GoalDetected = false;

    private int TotalPoints;

    private void Update()
    {
        //Detect goals
        if (EchoArenaAPI.Connected && EchoArenaAPI.CurrentSession.orange_points + EchoArenaAPI.CurrentSession.blue_points > TotalPoints) 
        { 
            GoalDetected = true;

            TotalPoints = EchoArenaAPI.CurrentSession.orange_points + EchoArenaAPI.CurrentSession.blue_points;
        }
    }

    void FixedUpdate()
    {
        //make a api call every frame
        if(EchoArenaAPI.MakingCall == false) StartCoroutine(EchoArenaAPI.MakeAPICall());

        if (EchoArenaAPI.Connected == false)
        {
            transform.position = defaultPos.position;
            transform.rotation = defaultPos.rotation;

            return;
        }

        player = FindClientPlayer();

        if (player.head.position != null && player.head.forward != null)
        {
            Vector3 TargetPos = new Vector3(player.head.position[2], player.head.position[1], player.head.position[0]);
            Vector3 TargetRot = new Vector3(player.head.forward[2], player.head.forward[1], player.head.forward[0]);

            transform.position = Tools.LerpVector(transform.position, TargetPos);
            transform.rotation = Quaternion.LookRotation(Tools.LerpVector(transform.forward, TargetRot));
        }

        HeadPos = transform.position;
    }

    //TODO: could be optimized
    EchoArenaAPI.Session.Player FindClientPlayer()
    {
        foreach (EchoArenaAPI.Session.Team t in EchoArenaAPI.CurrentSession.teams)
        {
            foreach (EchoArenaAPI.Session.Player p in t.players)
            {
                if (p.name == EchoArenaAPI.CurrentSession.client_name && t.team != "SPECTATORS")
                {
                    return p;
                }
            }
        }

        return EchoArenaAPI.CurrentSession.teams[0].players[0];
    }
}
