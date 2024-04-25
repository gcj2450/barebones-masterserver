using Barebones.Logging;
using Barebones.MasterServer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Unet_GameServer : NetworkBehaviour
{

    public LogLevel LogLevel = LogLevel.All;
    public BmLogger Logger = Msf.Create.Logger(typeof(Unet_GameServer).Name);

    public GameServer gameServer;
    public Unet_NetworkManager unetNetworkManager;

    public GameObject gameServerStates;
    public TextMesh UnetText;
    public MeshRenderer Ground;

    void Start()
    {

        Logger.LogLevel = LogLevel;

        gameServer.Registered += Registered;
        gameServer.GameStarted += GameStarted;
        gameServer.GameEnded += GameEnded;
    }

    private void GameStarted()
    {
        Logger.Info("GameStarted");
    }

    private void GameEnded()
    {
        Logger.Info("GameEnded");

        unetNetworkManager.HostDisconnect();
        gameServerStates.gameObject.SetActive(true);

        UnetText.text = string.Empty;
        Ground.material.color = Color.gray;
    }

    private void Registered(GameServerMatchDetailsPacket details)
    {
        Logger.Info(string.Format("Registered {0}", details.GamePort));
        unetNetworkManager.HostConnect(details.GamePort);

        gameServerStates.gameObject.SetActive(false);

        UnetText.text = details.SpawnId.ToString();
        Ground.material.color = DistinctColors.GetDistinctColor(details.SpawnId);
    }
}
