using Barebones.Logging;
using Barebones.MasterServer;
using Barebones.Networking;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CustomMatchmaker : ServerModuleBehaviour
{
    public LogLevel LogLevel = LogLevel.Info;
    public BmLogger Logger = Msf.Create.Logger(typeof(CustomMatchmaker).Name);

    public int PlayersPerMatch = 1;
    public float PollFrequency = 2f;

    public event Action<int> WaitingPlayersChanged;
    public event Action<int> GamesUnderwayChanged;
    public event Action<int> GamesPlayedChanged;
    public event Action<int> GamesAbortedChanged;

    protected Dictionary<string, MatchmakerPlayer> playersWaiting;

    private IServer server;

    private Dictionary<int, Match> matches;

    private SpawnersModule spawnersModule;

    private int gamesPlayed;
    public int GamesPlayed
    {
        get
        {
            return gamesPlayed;
        }
        set
        {
            gamesPlayed = value;
            if (GamesPlayedChanged != null)
                GamesPlayedChanged.Invoke(gamesPlayed);
        }
    }

    private int gamesUnderway;
    public int GamesUnderway
    {
        get
        {
            return gamesUnderway;
        }
        set
        {
            gamesUnderway = value;
            if (GamesUnderwayChanged != null)
                GamesUnderwayChanged.Invoke(gamesUnderway);
        }
    }

    private int gamesAborted;
    public int GamesAborted
    {
        get
        {
            return gamesAborted;
        }
        set
        {
            gamesAborted = value;
            if (GamesAbortedChanged != null)
                GamesAbortedChanged.Invoke(gamesAborted);
        }
    }

    public override void Initialize(IServer server)
    {
        base.Initialize(server);
        this.server = server;

        Logger.LogLevel = LogLevel;

        playersWaiting = new Dictionary<string, MatchmakerPlayer>();

        matches = new Dictionary<int, Match>();

        GamesPlayed = 0;
        GamesAborted = 0;
        GamesUnderway = 0;

        this.server.SetHandler((short)CustomOpCodes.requestStartGame, RequestStartGame);
        this.server.SetHandler((short)CustomOpCodes.gameServerMatchDetails, RegisterGameServerDetails);
        this.server.SetHandler((short)CustomOpCodes.gameServerMatchCompletion, GameServerMatchCompletion);

        spawnersModule = this.server.GetModule<SpawnersModule>();

        StartCoroutine(MatchmakerCoroutine());
    }

    private void GameServerMatchCompletion(IIncommingMessage message)
    {
        GameServerMatchCompletionPacket matchCompletion = message.Deserialize(new GameServerMatchCompletionPacket());
        Logger.Info(string.Format("GameServerMatchCompletion {0} {1}", matchCompletion.SpawnId, matchCompletion.Success));

        if (matches.ContainsKey(matchCompletion.SpawnId))
        {
            Match match = matches[matchCompletion.SpawnId];

            matches.Remove(matchCompletion.SpawnId);
            if (matchCompletion.Success)
                GamesPlayed = GamesPlayed + 1;
            else GamesAborted = GamesAborted + 1;

            GamesUnderway = GamesUnderway - 1;
        }
    }

    private void RegisterGameServerDetails(IIncommingMessage message)
    {
        GameServerMatchDetailsPacket details = message.Deserialize(new GameServerMatchDetailsPacket());
        Logger.Info(string.Format("RegisterGameServerDetails  SpawnId: {0}  MachineId: {1}  AssignedPort: {2}  SpawnCode: {3}  GameSecondaryPort: {4}",
            details.SpawnId,
            details.MachineId,
            details.AssignedPort,
            details.SpawnCode,
            details.GamePort));

        if (matches.ContainsKey(details.SpawnId))
        {
            Match match = matches[details.SpawnId];
            match.AssignedPort = details.AssignedPort;
            match.MachineId = details.MachineId;
            match.SpawnCode = details.SpawnCode;
            match.GamePort = details.GamePort;

            GamesUnderway = GamesUnderway + 1;

            NotifyClientsAndStartMatch(match);
        }
        else
        {
            Logger.Error("RegisterGameServerDetails: could not find match with spawnId = " + details.SpawnId);
        }
    }

    private void RequestStartGame(IIncommingMessage message)
    {
        int peerId = message.Peer.Id;
        var peer = Server.GetPeer(peerId);

        if (peer == null)
        {
            message.Respond("Peer with a given ID is not in the game", ResponseStatus.Error);
            return;
        }

        var account = peer.GetExtension<IUserExtension>();

        if (account == null)
        {
            message.Respond("Peer has not been authenticated", ResponseStatus.Failed);
            return;
        }

        AddMatchmakerPlayer(new MatchmakerPlayer(account.Username, message.Peer, Time.time));

        message.Respond("Ok", ResponseStatus.Success);
    }

    protected IEnumerator MatchmakerCoroutine()
    {
        float frequency = PollFrequency;

        while (true)
        {
            yield return new WaitForSeconds(frequency);

            bool started = TryToStartAMatch();
            frequency = started ? .1f : PollFrequency;
        }
    }

    private bool TryToStartAMatch()
    {
        if (playersWaiting.Count < PlayersPerMatch)
        {
            return false;
        }

        Logger.Info("Matchmaker " + playersWaiting.Count);

        List<MatchmakerPlayer> usersInMatch = new List<MatchmakerPlayer>();

        int numberPlayers = 0;
        lock (playersWaiting)
        {
            while (true)
            {
                string user = playersWaiting.Keys.First();
                MatchmakerPlayer player = playersWaiting[user];
                RemoveMatchmakerPlayer(user);
                usersInMatch.Add(player);
                numberPlayers++;
                if (numberPlayers >= PlayersPerMatch)
                    break;
            }
        }

        StartMatch(usersInMatch);

        return true;
    }

    public void RemoveMatchmakerPlayer(string username)
    {
        bool removed = false;
        lock (playersWaiting)
        {
            if (playersWaiting.ContainsKey(username))
            {
                playersWaiting.Remove(username);
                removed = true;
            }
        }
        if (removed && WaitingPlayersChanged != null)
            WaitingPlayersChanged.Invoke(playersWaiting.Count);
    }

    public void AddMatchmakerPlayer(MatchmakerPlayer player)
    {
        bool added = false;
        lock (playersWaiting)
        {
            if (!playersWaiting.ContainsKey(player.Username))
            {
                playersWaiting.Add(player.Username, player);
                added = true;
            }
        }
        if (added && WaitingPlayersChanged != null)
            WaitingPlayersChanged.Invoke(playersWaiting.Count);
    }

    private bool StartMatch(List<MatchmakerPlayer> usersInMatch)
    {
        SpawnTask spawnTask = SpawnGameServer(usersInMatch);

        if (spawnTask == null)
        {
            Logger.Info("no gameservers available");
            // put the users back in the queue
            foreach (MatchmakerPlayer player in usersInMatch)
            {
                AddMatchmakerPlayer(player);
            }
            return false;
        }

        Match match = new Match(spawnTask.SpawnId, usersInMatch);

        this.matches.Add(match.SpawnId, match);
        if (GamesUnderwayChanged != null)
            GamesUnderwayChanged.Invoke(matches.Count);

        return true;
    }

    private SpawnTask SpawnGameServer(List<MatchmakerPlayer> usersInMatch)
    {
        var settings = new Dictionary<string, string>();
        settings.Add("Type", "Deathmatch");
        settings.Add("Duration", "600");
        settings.Add("Region", "Earth");
        settings.Add("UserCount", usersInMatch.Count.ToString());

        SpawnTask task = spawnersModule.Spawn(settings, "");

        if (task == null)
        {
            Logger.Info("Busy");
            return null;
        }

        return task;
    }

    private void ReturnPlayersToMatchmaker(List<MatchmakerPlayer> usersInMatch)
    {
        foreach (MatchmakerPlayer player in usersInMatch)
        {
            AddMatchmakerPlayer(player);
        }
    }

    private void NotifyClientsAndStartMatch(Match match)
    {
        var msg = Msf.Create.Message(
            (short)CustomOpCodes.gameServerMatchDetails,
            new GameServerMatchDetailsPacket()
            {
                SpawnId = match.SpawnId,
                MachineId = match.MachineId,
                AssignedPort = match.AssignedPort,
                SpawnCode = match.SpawnCode,
                GamePort = match.GamePort
            });

        foreach (var player in match.Players)
        {
            player.Peer.SendMessage(msg);
        }
    }

}

