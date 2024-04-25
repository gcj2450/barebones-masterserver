using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Barebones.Logging;
using Barebones.MasterServer;
using Barebones.Networking;

public enum GameServerState
{
    Initial,
    ConnectingToMaster,
    ConnectedToMaster,
    FailedToConnectToMaster,
    Registering,
    Registered,
    FailedToRegister,
    WaitingForClients,
    GameStarted,
    GameEnded,
    DisconnectedFromMaster,
    Stop
};

public class GameServer : MonoBehaviour {

    public LogLevel LogLevel = LogLevel.All;
    public BmLogger Logger = Msf.Create.Logger(typeof(GameServer).Name);

    public GameObject ButtonUI;
    public Button[] Buttons;

    public bool useWs = true;
    public int GameDuration = 2;
    public float QuitDelay = 10;

    public event Action<GameServerMatchDetailsPacket> Registered;
    public event Action GameStarted;
    public event Action GameEnded;

    private float WaitingToConnectToMaster = 0f;
    private float WaitingForClients = 10f;
    private float ConnectToMasterTimeout = 10f;
    private int gamePort;

    private List<IPeer> Clients;

    private SpawnTaskController currentSpawnTaskController;
    private int userCount = 0;
    private IServerSocket gameServerSocket;

    private GameServerState _clientState;
    public GameServerState GameServerState
    {
        get { return _clientState; }
        set
        {
            _clientState = value;
            ShowGameServerState();
        }
    }

    private string _status;
    public string Status
    {
        set
        {
            _status = value;
        }
        get { return _status; }
    }

    ColorBlock highlightedColorBlock;

    private void ShowGameServerState()
    {
        if ((int)this.GameServerState < Buttons.Length &&
             Buttons[(int)this.GameServerState] != null)
        {
            Buttons[(int)this.GameServerState].Select();
            Buttons[(int)this.GameServerState].colors = highlightedColorBlock;
        }
    }

    public void Awake()
    {
        Logger.LogLevel = LogLevel;

        ButtonUI.gameObject.SetActive(true);

        highlightedColorBlock = ColorBlock.defaultColorBlock;
        highlightedColorBlock.highlightedColor = new Color(135 / 255f, 206 / 255f, 250 / 255f);

        if (Msf.Args.IsProvided("-gamePort"))
        {
            gamePort = Msf.Args.ExtractValueInt("-gamePort");
        }

        DumpCommandLineArgs();

        Clients = new List<IPeer>();

        SetupGameServerSocket();

        GoToState_Initial();
    }

    private void DumpCommandLineArgs()
    {
        string args = string.Empty;

        foreach (string arg in Environment.GetCommandLineArgs())
        {
            args += arg + " ";
        }
        args = args.TrimEnd();
        Debug.Log(args);
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

    private void GoToState_Initial()
    {
        GameServerState = GameServerState.Initial;
        Status = string.Empty;

        StartCoroutine(CoroutineUtils.StartWaiting(WaitingToConnectToMaster,
            () => { GoToState_ConnectingToMaster(); },
            1f,
            (time) => { Status = string.Format("Waiting {0}s", time); }, false));
    }

    private void GoToState_ConnectingToMaster()
    {
        GameServerState = GameServerState.ConnectingToMaster;
        Status = string.Empty;

        // connect to the master server
        Msf.Connection.Connected += Connected;
        Msf.Connection.Disconnected += Disconnected;

        Logger.Info(string.Format("Connecting to master on {0}:{1}", Msf.Args.MasterIp, Msf.Args.MasterPort));

        backgroundEnumerator = CoroutineUtils.StartWaiting(ConnectToMasterTimeout,
            () => { GoToState_FailedToConnectToMaster("Timed out"); },
            1f,
            (time) => { Status = string.Format("Trying to connect {0}s", time); });
        StartCoroutine(backgroundEnumerator);

        Msf.Connection.Connect(Msf.Args.MasterIp, Msf.Args.MasterPort);
    }

    private void Disconnected()
    {
        GoToState_DisconnectedFromMaster();
    }

    private void Connected()
    {
        GoToState_ConnectedToMaster();
    }

    private void GoToState_ConnectedToMaster()
    {
        StopBackgroundEnumerator();

        GameServerState = GameServerState.ConnectedToMaster;
        Status = string.Empty;

        GoToState_Registering();
    }


    private void GoToState_FailedToConnectToMaster(string message)
    {
        GameServerState = GameServerState.FailedToConnectToMaster;
        Status = message;

        Msf.Connection.Connected -= Connected;
        Msf.Connection.Disconnected -= Disconnected;

        StartCoroutine(CoroutineUtils.StartWaiting(QuitDelay, () => { GoToState_Stop(); }));
    }

    private void GoToState_Stop()
    {
        GameServerState = GameServerState.Stop;
        Status = string.Empty;

        Application.Quit();
    }

    private void GoToState_Registering()
    {
        GameServerState = GameServerState.Registering;
        Status = string.Empty;

        int spawnId = Msf.Args.SpawnId == -1 ? 0 : Msf.Args.SpawnId;
        string spawnCode = Msf.Args.SpawnCode == null ? "5acc3d" : Msf.Args.SpawnCode;
        Logger.Info(string.Format("spawnerId: {0}  spawnerCode: {1}", spawnId, spawnCode));

        Msf.Server.Spawners.RegisterSpawnedProcess(spawnId, spawnCode, HandleRegistered);
    }

    private void HandleRegistered(SpawnTaskController taskController, string error)
    {
        currentSpawnTaskController = taskController;

        if (currentSpawnTaskController == null)
        {
            throw new Exception("HandleRegistered null taskController");
        }

        userCount = 1;

        if (taskController.Properties == null)
        {
            throw new Exception("HandleRegistered null Properties");
        }

        if (currentSpawnTaskController.Properties.ContainsKey("UserCount"))
        {
            userCount = int.Parse(currentSpawnTaskController.Properties["UserCount"]);
            Logger.Info(string.Format("Match scheduled with {0} users.", userCount));
        }

        // log the property dictionary
        //foreach (KeyValuePair property in taskController.Properties)
        //{
        //    Logger.Info(string.Format("{0} = {1}", property.Key, property.Value));
        //}

        Logger.Info("Listening for game clients on port: " + Msf.Args.AssignedPort);

        gameServerSocket.Listen(Msf.Args.AssignedPort);

        GoToState_Registered();
    }

    private void GoToState_Registered()
    {
        GameServerState = GameServerState.Registered;
        Status = string.Empty;

        SendGameServerMatchDetails();

        GoToState_WaitingForClients();
    }

    private void SetupGameServerSocket()
    {
        if (useWs)
            gameServerSocket = new ServerSocketWs();
        else
            gameServerSocket = new ServerSocketUnet();

        gameServerSocket.Connected += ClientConnected;
        gameServerSocket.Disconnected += ClientDisconnected;
    }

    private void ClientConnected(IPeer client)
    {
        Status = string.Format("ClientConnected: Id {0}", client.Id);
        Logger.Info(Status);

        Clients.Add(client);
        if (Clients.Count == userCount)
        {
            StartCoroutine(CoroutineUtils.StartWaiting(3f, () => { GoToState_GameStarted(); }));
        }
    }

    private void ClientDisconnected(IPeer client)
    {
        Clients.Remove(client);
    }

    private void GoToState_WaitingForClients()
    {
        GameServerState = GameServerState.WaitingForClients;
        Status = string.Empty;

        backgroundEnumerator = CoroutineUtils.StartWaiting(WaitingForClients,
            () => { GoToState_GameEnded(false); },
            1f,
            (time) => { Status = string.Format("Waiting for clients {0}s", time); });
        StartCoroutine(backgroundEnumerator);
    }

    private void SendGameServerMatchDetails()
    {
        GameServerMatchDetailsPacket details = new GameServerMatchDetailsPacket()
        {
            SpawnId = Msf.Args.SpawnId,
            MachineId = Msf.Args.MachineIp,
            AssignedPort = Msf.Args.AssignedPort,
            SpawnCode = Msf.Args.SpawnCode,
            GamePort = gamePort
        };

        Logger.Info(string.Format("SpawnId: {0}  MachineId: {1}  AssignedPort: {2}  SpawnCode: {3}  GameSecondaryPort: {4}",
            details.SpawnId,
            details.MachineId,
            details.AssignedPort,
            details.SpawnCode,
            details.GamePort));
        Logger.Info("send gameservermatchdetails");

        Msf.Connection.SendMessage((short)CustomOpCodes.gameServerMatchDetails, details, (status, response) =>
        {
            if (status != ResponseStatus.Success)
            {
                Logger.Debug("Failed to get response");
            }
        });

        if (Registered != null)
        {
            Registered.Invoke(details);
        }
    }

    private void GoToState_GameStarted()
    {
        StopBackgroundEnumerator();

        GameServerState = GameServerState.GameStarted;
        Status = string.Empty;

        BroadcastMessage(MessageHelper.Create(0, "Start Game"));

        StartCoroutine(CoroutineUtils.StartWaiting(GameDuration,
            () => { GoToState_GameEnded(); },
            1f,
            (time) => {
                Status = string.Format("Game Count Down {0}", time);
                BroadcastMessage(MessageHelper.Create(0, Status));
            }, false));

        if (GameStarted != null)
            GameStarted.Invoke();
    }

    private void GoToState_GameEnded(bool success = true)
    {
        GameServerState = GameServerState.GameEnded;
        Status = string.Empty;

        BroadcastMessage(MessageHelper.Create(0, "End Game"));

        var msg = Msf.Create.Message((short)CustomOpCodes.gameServerMatchCompletion, new GameServerMatchCompletionPacket()
        {
            SpawnId = Msf.Args.SpawnId,
            Success = success
        });
        Msf.Connection.Peer.SendMessage(msg);

        if (GameEnded != null)
            GameEnded.Invoke();

        GoToState_DisconnectedFromMaster();
    }

    private void GoToState_DisconnectedFromMaster()
    {
        GameServerState = GameServerState.DisconnectedFromMaster;
        Status = string.Empty;

        // could close gameserversocket, connection to master
        StartCoroutine(CoroutineUtils.StartWaiting(QuitDelay, () => { GoToState_Stop(); }));
    }

    private void BroadcastMessage(IMessage msg)
    {
        Logger.Info(Encoding.UTF8.GetString(msg.Data));
        foreach (IPeer client in Clients)
        {
            client.SendMessage(msg, DeliveryMethod.Reliable);
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(2, 2, Screen.width, style.fontSize);
        style.fontSize = 14;
        style.alignment = TextAnchor.UpperLeft;
        style.normal.textColor = Color.black;

        string text = string.Format("[GameServer | {0}] {1}",
            GameServerState, Status);
        GUI.Label(rect, text, style);
    }
}
