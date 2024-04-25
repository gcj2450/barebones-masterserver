using Barebones.Logging;
using Barebones.MasterServer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unet_ClientController : MonoBehaviour
{

    public LogLevel LogLevel = LogLevel.All;
    public BmLogger Logger = Msf.Create.Logger(typeof(Unet_ClientController).Name);

    public MSFClientController ClientController;
    public Unet_NetworkManager unetNetworkManager;

    public GameObject clientStates;
    public TextMesh UnetText;
    public MeshRenderer Ground;

    // Use this for initialization
    void Start()
    {

        Logger.LogLevel = LogLevel;

        ClientController.ConnectToGameServer += ConnectToGameServer;
        ClientController.GameStarted += GameStarted;
        ClientController.GameEnded += GameEnded;
    }

    private void GameStarted()
    {
        Logger.Info("GameStarted");
    }

    private void GameEnded()
    {
        Logger.Info("GameEnded");

        unetNetworkManager.ClientDisconnect();
        clientStates.gameObject.SetActive(true);

        UnetText.text = string.Empty;
        Ground.material.color = Color.gray;
    }

    private void ConnectToGameServer(GameServerMatchDetailsPacket details)
    {
        Logger.Info(string.Format("ConnectToGameServer {0}", details.GamePort));

        unetNetworkManager.ClientConnect(details.MachineId, details.GamePort);

        clientStates.gameObject.SetActive(false);

        UnetText.text = details.SpawnId.ToString();
        Ground.material.color = DistinctColors.GetDistinctColor(details.SpawnId);
    }
}
