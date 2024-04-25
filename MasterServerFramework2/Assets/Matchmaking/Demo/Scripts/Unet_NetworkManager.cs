using Barebones.Logging;
using Barebones.MasterServer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Unet_NetworkManager : NetworkManager
{
    public LogLevel LogLevel = LogLevel.Info;
    public BmLogger Logger = Msf.Create.Logger(typeof(Unet_NetworkManager).Name);

    private bool isServer = false;

    void Awake()
    {
        Logger.LogLevel = LogLevel;
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        if (isServer)
            return;

        Logger.Info("NetworkManager OnClientConnect Add Player");

        ClientScene.AddPlayer(conn, 0);
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        isServer = true;
        Logger.Info("NetworkManager OnServerConnect");
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        Logger.Info("NetworkManager OnClientDisconnect");
    }

    public override void OnClientError(NetworkConnection conn, int errorCode)
    {
        Logger.Info("NetworkManager OnClientError " + errorCode);
    }

    public void HostConnect(int port)
    {
        networkPort = port;
        Logger.Info("NetworkManager HostConnect " + port);
        StartHost();
    }

    public void HostDisconnect()
    {
        Logger.Info("NetworkManager HostDisconnect");

        StopHost();
    }

    public void ClientConnect(string address, int port)
    {
        networkPort = port;
        networkAddress = address;
        Logger.Info("NetworkManager ClientConnect " + port);

        StartClient();
    }

    public void ClientDisconnect()
    {
        Logger.Info("NetworkManager ClientDisconnect");

        StopClient();
    }

}
