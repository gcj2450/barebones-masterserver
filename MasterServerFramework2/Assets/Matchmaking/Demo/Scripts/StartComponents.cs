using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones;
using Barebones.MasterServer;
using static UnityEngine.UI.CanvasScaler;

public class StartComponents : MonoBehaviour
{

    public GameObject ClientController;
    public GameObject MasterServer;
    public GameObject GameServer;
    public GameObject Spawner;

    public GUIConsole guiConsole;

    [Header("Unet")]
    public bool UseUnet = false;
    public GameObject Unet;
    public GameObject UnetClientController;
    public GameObject UnetGameServer;

    [Header("Bolt")]
    public bool UseBolt = false;
    public GameObject Bolt;
    public GameObject BoltClientController;
    public GameObject BoltGameServer;

    void Start()
    {
        bool anythingStarted = false;
        bool inEditor = false;

#if UNITY_EDITOR
        inEditor = true;
#endif
        if (inEditor)
        {
            StartMasterServer();
            StartSpawner();
            anythingStarted = true;
        }

        if (Msf.Args.SpawnId != -1)
        {
            StartGameServer();
            anythingStarted = true;
        }

        if (Msf.Args.StartMaster)
        {
            StartMasterServer();
            anythingStarted = true;
        }

        if (Msf.Args.IsProvided("-client"))
        {
            StartClient();
            anythingStarted = true;
        }

        if (Msf.Args.IsProvided(Msf.Args.Names.StartSpawner))
        {
            StartSpawner();
            anythingStarted = true;
        }

        if (!anythingStarted)
        {
            Debug.Log(string.Format("supported: -client {0} {1}", Msf.Args.Names.StartMaster, Msf.Args.Names.StartSpawner));
        }

    }

    private void StartMasterServer()
    {
        MasterServer.gameObject.SetActive(true);
        MasterServerBehaviour master = MasterServer.GetComponent<MasterServerBehaviour>();
        master.StartServer();
    }

    private void ActivateBoltOrUnet(bool isClient)
    {
        if (UseUnet)
        {
            ActivateUnet(isClient);
        }
        if (UseBolt)
        {
            ActivateBolt(isClient);
        }
    }

    private void ActivateUnet(bool isClient)
    {
        Unet.gameObject.SetActive(true);

        if (isClient)
        {
            UnetClientController.gameObject.SetActive(true);
        }
        else
        {
            UnetGameServer.gameObject.SetActive(true);
        }
    }

    private void ActivateBolt(bool isClient)
    {
        Bolt.gameObject.SetActive(true);

        if (isClient)
        {
            BoltClientController.gameObject.SetActive(true);
        }
        else
        {
            BoltGameServer.gameObject.SetActive(true);
        }
    }

    private void StartClient()
    {
        ActivateBoltOrUnet(true);

        guiConsole.Show = false;
        ClientController.gameObject.SetActive(true);
    }

    private void StartGameServer()
    {
        ActivateBoltOrUnet(false);

        GameServer.gameObject.SetActive(true);
        guiConsole.Show = false;
    }

    private void StartSpawner()
    {
        guiConsole.Show = true;
        Msf.Connection.Connect(Msf.Args.MasterIp, Msf.Args.MasterPort);
        Spawner.gameObject.SetActive(true);
    }

}