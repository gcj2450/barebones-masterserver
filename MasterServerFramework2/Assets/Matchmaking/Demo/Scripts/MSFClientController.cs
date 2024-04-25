using Barebones.MasterServer;
using Barebones.Networking;
using Barebones.Logging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MSFClientController : MonoBehaviour {

    public string ServerIp = "127.0.0.1";
    public int ServerPort = 5000;
    public ClientController controller;

    public LogLevel LogLevel = LogLevel.Info;
    public BmLogger Logger = Msf.Create.Logger(typeof(MSFClientController).Name);

    public event Action<GameServerMatchDetailsPacket> ConnectToGameServer;
    public event Action GameStarted;
    public event Action GameEnded;

    public bool useWs = true;

    // timeouts and waits
    private float ConnectToMasterTimeout = 10f;
    public float ConnectToGameServerTimeout = 30f;
    private float LogInTimeout = 5f;

    private float WaitingToConnectToMaster = 1f;
    private float RequestedGameTimeout = 60f;
    private float WaitingBetweenGames = 2f;

    private IClientSocket gameServerSocket;

    void Start()
    {
        UnityEngine.Random.InitState((int)(System.DateTime.Now.Ticks % 10000));

        Logger.LogLevel = LogLevel;

        Msf.Connection.Connected += Connected;
        Msf.Connection.Disconnected += Disconnected;

        GoToState_Initial();
    }

    private void Connected()
    {
        GoToState_ConnectedToMaster();
    }

    private void Disconnected()
    {
        GoToState_DisconnectedFromMaster();
    }

    private void GoToState_Initial()
    {
        controller.ClientState = ClientState.Initial;
        controller.Status = string.Empty;

        StartCoroutine(CoroutineUtils.StartWaiting(WaitingToConnectToMaster,
            () => { GoToState_ConnectingToMaster(); },
            1f,
            (time) => { controller.Status = string.Format("Waiting {0}s", time); }, false));
    }

    private IEnumerator backgroundEnumerator;

    private void StopBackgroundEnumerator()
    {
        if (backgroundEnumerator != null)
        {
            StopCoroutine(backgroundEnumerator);
            backgroundEnumerator = null;
        }
    }

    private void GoToState_ConnectingToMaster()
    {
        controller.ClientState = ClientState.ConnectingToMaster;

        backgroundEnumerator = CoroutineUtils.StartWaiting(ConnectToMasterTimeout,
                () => { GoToState_FailedToConnectToMaster("Timed out"); },
                1f,
                (time) => { controller.Status = string.Format("\"{0}:{1}\" {2}s", ServerIp, ServerPort, time); });
        StartCoroutine(backgroundEnumerator);

        Msf.Connection.Connect(ServerIp, ServerPort);
    }

    private void GoToState_DisconnectedFromMaster()
    {
        StopBackgroundEnumerator();

        controller.ClientState = ClientState.DisconnectedFromMaster;
        controller.Status = string.Empty;

        StartCoroutine(CoroutineUtils.StartWaiting(2f, () => { GoToState_Stop(); }));
    }

    private void OnConnected()
    {
        GoToState_ConnectedToMaster();
    }

    private void GoToState_FailedToConnectToMaster(string error)
    {
        controller.ClientState = ClientState.FailedToConnectToMaster;
        controller.Status = error;
        StartCoroutine(CoroutineUtils.StartWaiting(1f, () => { GoToState_Initial(); }));
    }

    private void GoToState_ConnectedToMaster()
    {
        StopBackgroundEnumerator();

        controller.ClientState = ClientState.ConnectedToMaster;
        controller.Status = string.Format("at \"{0}:{1}\"", ServerIp, ServerPort);

        StartCoroutine(CoroutineUtils.StartWaiting(1f,
            () => { GoToState_LoggingIn(); }));
    }

    private void GoToState_LoggingIn()
    {
        controller.ClientState = ClientState.LoggingIn;
        controller.Status = string.Empty;

        backgroundEnumerator = CoroutineUtils.StartWaiting(LogInTimeout,
            () => { GoToState_FailedToLogIn("Timed out"); },
            1f,
            (time) => {
                controller.Status =
                    string.Format("Trying to log in {0}s", time);
            });
        StartCoroutine(backgroundEnumerator);

        Msf.Client.Auth.LogInAsGuest((successful, loginError) =>
        {
            if (successful == null)
            {
                GoToState_FailedToLogIn(loginError);
            }
            else
            {
                GoToState_LoggedIn(Msf.Client.Auth.AccountInfo.Username);
            }
        });
    }

    private void GoToState_LoggedIn(string username)
    {
        StopBackgroundEnumerator();

        controller.ClientState = ClientState.LoggedIn;
        controller.Status = string.Format("Logged in: {0}", username);

        Msf.Client.Connection.SetHandler((short)CustomOpCodes.gameServerMatchDetails, OnGameServerMatchDetails);

        StartCoroutine(CoroutineUtils.StartWaiting(3f,
                () => { GoToState_BetweenGames(); }));
    }

    private void OnGameServerMatchDetails(IIncommingMessage message)
    {
        GameServerMatchDetailsPacket details = message.Deserialize(new GameServerMatchDetailsPacket());
        GoToState_AssignedGame(details);
    }

    private void GoToState_AssignedGame(GameServerMatchDetailsPacket details)
    {
        StopBackgroundEnumerator();

        string s = string.Format("Game Server Details  SpawnId {0}  SpawnCode {1}  MachineId {2}  AssignedPort {3}  GameSecondaryPort {4}",
            details.SpawnId,
            details.SpawnCode,
            details.MachineId,
            details.AssignedPort,
            details.GamePort);
        Logger.Info(s);

        controller.ClientState = ClientState.AssignedGame;
        controller.Status = string.Empty;

        GoToState_ConnectingToGameServer(details);
    }

    private void GoToState_FailedToLogIn(string loginError)
    {
        controller.ClientState = ClientState.FailedToLogIn;
        controller.Status = loginError;

        StartCoroutine(CoroutineUtils.StartWaiting(3f,
            () => { GoToState_LoggingIn(); }));
    }

    private void GoToState_BetweenGames()
    {
        controller.ClientState = ClientState.BetweenGames;
        controller.Status = string.Empty;

        backgroundEnumerator = CoroutineUtils.StartWaiting(WaitingBetweenGames,
            () => { GoToState_RequestedGame(); },
            1f,
            (time) => { controller.Status = string.Format("Waiting {0}s", time); }, false);
        StartCoroutine(backgroundEnumerator);
    }

    private void GoToState_RequestedGame()
    {
        StopBackgroundEnumerator();

        controller.ClientState = ClientState.RequestedGame;
        controller.Status = string.Empty;

        var msg = MessageHelper.Create((short)CustomOpCodes.requestStartGame, "Please");
        Msf.Connection.Peer.SendMessage(msg);

        backgroundEnumerator = CoroutineUtils.StartWaiting(RequestedGameTimeout,
            () => { GoToState_FailedToGetGame("Timed out"); },
            1f,
            (time) => { controller.Status = string.Format("Waiting for game {0}s", time); });
        StartCoroutine(backgroundEnumerator);
    }

    private void GoToState_FailedToGetGame(string message)
    {
        StopBackgroundEnumerator();

        controller.ClientState = ClientState.FailedToGetGame;
        controller.Status = message;

        controller.GamesAborted = controller.GamesAborted + 1;

        StartCoroutine(CoroutineUtils.StartWaiting(3f, () => { GoToState_BetweenGames(); }));
    }

    private void GoToState_ConnectingToGameServer(GameServerMatchDetailsPacket details)
    {
        controller.ClientState = ClientState.ConnectingToGameServer;
        controller.Status = string.Empty;

        SetupGameServerSocket();

        Logger.Info(string.Format("Connect to game server on {0}:{1}", details.MachineId, details.GamePort));

        gameServerSocket.Connect(details.MachineId, details.AssignedPort);

        backgroundEnumerator = CoroutineUtils.StartWaiting(ConnectToGameServerTimeout,
            () => { GoToState_FailedToConnectToGameServer(); },
            1f,
            (time) => { controller.Status = string.Format("Waiting for game {0}s", time); });
        StartCoroutine(backgroundEnumerator);

        if (ConnectToGameServer != null)
            ConnectToGameServer.Invoke(details);

    }

    private void GoToState_FailedToConnectToGameServer()
    {
        StopBackgroundEnumerator();

        controller.ClientState = ClientState.FailedToConnectToGameServer;
        controller.Status = string.Empty;

        controller.GamesAborted = controller.GamesAborted + 1;

        gameServerSocket = null;
    }

    private void HandleGameServerMessage(IIncommingMessage msg)
    {
        string s = msg.ToString();
        controller.Status = s;
        if (s.CompareTo("Start Game") == 0)
        {
            GoToState_GameStarted();
            return;
        }
        if (s.CompareTo("End Game") == 0)
        {
            GoToState_GameEnded();
            return;
        }
    }

    private void GoToState_ConnectedToGameServer()
    {
        StopBackgroundEnumerator();

        controller.ClientState = ClientState.ConnectedToGameServer;
        controller.Status = string.Empty;

    }

    private void GoToState_GameStarted()
    {
        controller.ClientState = ClientState.GameStarted;
        controller.Status = string.Empty;

        if (GameStarted != null)
            GameStarted.Invoke();
    }

    private void GoToState_GameEnded()
    {
        controller.ClientState = ClientState.GameEnded;
        controller.Status = string.Empty;


        gameServerSocket.Disconnected -= GoToState_DisconnectedFromGameServer;
        gameServerSocket.Disconnect();
        gameServerSocket = null;

        controller.GamesPlayed = controller.GamesPlayed + 1;

        if (GameEnded != null)
            GameEnded.Invoke();

        GoToState_BetweenGames();
    }

    private void GoToState_DisconnectedFromGameServer()
    {
        StopBackgroundEnumerator();

        if (controller.ClientState != ClientState.GameEnded)
        {
            controller.GamesAborted = controller.GamesAborted + 1;
            Logger.Debug("aborted " + controller.ClientState);
        }

        controller.ClientState = ClientState.DisconnectedFromGameServer;
        controller.Status = string.Empty;

        gameServerSocket.Connected -= GoToState_ConnectedToGameServer;
        gameServerSocket.Disconnected -= GoToState_DisconnectedFromGameServer;
        gameServerSocket = null;

        if (!Msf.Connection.IsConnected)
            GoToState_DisconnectedFromMaster();
    }

    private void SetupGameServerSocket()
    {
        if (useWs)
            gameServerSocket = new ClientSocketWs();
        else gameServerSocket = new ClientSocketUnet();

        gameServerSocket.Connected += GoToState_ConnectedToGameServer;
        gameServerSocket.Disconnected += GoToState_DisconnectedFromGameServer;
        gameServerSocket.SetHandler(new PacketHandler(0, HandleGameServerMessage));
    }

    private void GoToState_Stop()
    {
        controller.ClientState = ClientState.Stop;
        controller.Status = string.Empty;
    }

}
