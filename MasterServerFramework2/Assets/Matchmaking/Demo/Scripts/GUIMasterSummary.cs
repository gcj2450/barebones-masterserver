using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using System;

public class GUIMasterSummary : MonoBehaviour {

    public CustomMatchmaker matchmaker;
    public AuthModule authModule;

    private int waitingPlayers;
    private int playersLoggedIn;
    private int gamesPlayed;
    private int gamesUnderway;
    private int gamesAborted;

    void Start()
    {
        if (!MasterServerBehaviour.IsMasterRunning)
        {
            Msf.Server.Connection.Connected += Connected;
        }
        else
        {
            Connected();
        }
    }

    private void Connected()
    {
        if (!MasterServerBehaviour.IsMasterRunning)
            return;

        matchmaker.GamesPlayedChanged += GamesPlayedChanged;
        matchmaker.WaitingPlayersChanged += WaitingPlayersChanged;
        matchmaker.GamesUnderwayChanged += GamesUnderwayChanged;
        matchmaker.GamesAbortedChanged += GameAbortedChanged;

        authModule.LoggedIn += LoggedIn;
        authModule.LoggedOut += LoggedOut;
    }

    
    private void LoggedOut(IUserExtension account)
    {
        playersLoggedIn--;
    }

    private void LoggedIn(IUserExtension account)
    {
        playersLoggedIn++;
    }

    private void GamesUnderwayChanged(int n)
    {
        gamesUnderway = n;
    }

    private void WaitingPlayersChanged(int n)
    {
        waitingPlayers = n;
    }

    private void GamesPlayedChanged(int n)
    {
        gamesPlayed = n;
    }

    private void GameAbortedChanged(int n)
    {
        gamesAborted = n;
    }

    void OnGUI()
    {
        if (!MasterServerBehaviour.IsMasterRunning)
            return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 14;

        Rect rect = new Rect(2, 2, Screen.width, style.fontSize);
        style.alignment = TextAnchor.UpperLeft;
        style.normal.textColor = Color.black;

        string text = string.Empty;
        text = string.Format("[Master | Users  LoggedIn: {0} Matchmaking: {1}]", playersLoggedIn, waitingPlayers);
        text += string.Format("\t[Games   Played: {0} Underway: {1}  Aborted: {2}]", gamesPlayed, gamesUnderway, gamesAborted);
        GUI.Label(rect, text, style);
    }

}
