using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using Barebones.Logging;
using Barebones.MasterServer;
using Barebones.Networking;

public class CustomSpawner : SpawnerBehaviour
{

    public BmLogger CustomLogger = Msf.Create.Logger(typeof(CustomSpawner).Name);

    [Header("Custom Spawner")]
    public LogLevel CustomSpawnerLogLevel = LogLevel.Info;

    public int startingPort = 2600;
    public int endingPort = 2700;

    private PortAllocator gamePorts;

    protected override void Start()
    {
        base.Start();

        CustomLogger.LogLevel = CustomSpawnerLogLevel;
        Logger.LogLevel = LogLevel;

        startingPort = Msf.Args.ExtractValueInt("-gameStartPort", startingPort);
        endingPort = Msf.Args.ExtractValueInt("-gameEndPort", endingPort);

        CustomLogger.Info(string.Format("startingPort: {0}  endingPort: {1}", startingPort, endingPort));
        gamePorts = new PortAllocator(startingPort, endingPort);
    }

    protected override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

    }

    void CustomSpawnRequestHandler(SpawnRequestPacket packet, IIncommingMessage message)
    {
        SpawnerController controller = Msf.Server.Spawners.GetController(packet.SpawnerId);

        if (string.IsNullOrEmpty(Msf.Args.MasterIp))
            controller.DefaultSpawnerSettings.MasterIp = "127.0.0.1";

        MyCustomSpawnRequestHandler(packet, message);
    }

    public void MyCustomSpawnRequestHandler(SpawnRequestPacket packet, IIncommingMessage message)
    {
        CustomLogger.Debug("Custom spawn handler started handling a request to spawn process");

        var controller = Msf.Server.Spawners.GetController(packet.SpawnerId);

        if (controller == null)
        {
            message.Respond("Failed to spawn a process. Spawner controller not found", ResponseStatus.Failed);
            return;
        }

        var port = Msf.Server.Spawners.GetAvailablePort();
        int gamePort = gamePorts.GetAvailablePort();

        // Check if we're overriding an IP to master server
        var masterIp = string.IsNullOrEmpty(controller.DefaultSpawnerSettings.MasterIp) ?
            controller.Connection.ConnectionIp : controller.DefaultSpawnerSettings.MasterIp;

        // Check if we're overriding a port to master server
        var masterPort = controller.DefaultSpawnerSettings.MasterPort < 0 ?
            controller.Connection.ConnectionPort : controller.DefaultSpawnerSettings.MasterPort;

        // Machine Ip
        var machineIp = controller.DefaultSpawnerSettings.MachineIp;

        // Path to executable
        var path = controller.DefaultSpawnerSettings.ExecutablePath;
        if (string.IsNullOrEmpty(path))
        {
            path = File.Exists(Environment.GetCommandLineArgs()[0])
                ? Environment.GetCommandLineArgs()[0]
                : System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        }

        // In case a path is provided with the request
        if (packet.Properties.ContainsKey(MsfDictKeys.ExecutablePath))
            path = packet.Properties[MsfDictKeys.ExecutablePath];

        // Get the scene name
        var sceneNameArgument = packet.Properties.ContainsKey(MsfDictKeys.SceneName)
            ? string.Format("{0} {1} ", Msf.Args.Names.LoadScene, packet.Properties[MsfDictKeys.SceneName])
            : "";

        if (!string.IsNullOrEmpty(packet.OverrideExePath))
        {
            path = packet.OverrideExePath;
        }

        // If spawn in batchmode was set and `DontSpawnInBatchmode` arg is not provided
        var spawnInBatchmode = controller.DefaultSpawnerSettings.SpawnInBatchmode
                               && !Msf.Args.DontSpawnInBatchmode;

        var startProcessInfo = new System.Diagnostics.ProcessStartInfo(path)
        {
            CreateNoWindow = false,
            UseShellExecute = false,
            Arguments = " " +
                (spawnInBatchmode ? "-batchmode -nographics " : "") +
                (controller.DefaultSpawnerSettings.AddWebGlFlag ? Msf.Args.Names.WebGl + " " : "") +
                sceneNameArgument +
                string.Format("{0} {1} ", Msf.Args.Names.MasterIp, masterIp) +
                string.Format("{0} {1} ", Msf.Args.Names.MasterPort, masterPort) +
                string.Format("{0} {1} ", Msf.Args.Names.SpawnId, packet.SpawnId) +
                string.Format("{0} {1} ", Msf.Args.Names.AssignedPort, port) +
                string.Format("{0} {1} ", Msf.Args.Names.MachineIp, machineIp) +
                string.Format("{0} {1} ", "-gamePort", gamePort) +
                (Msf.Args.DestroyUi ? Msf.Args.Names.DestroyUi + " " : "") +
                string.Format("{0} \"{1}\" ", Msf.Args.Names.SpawnCode, packet.SpawnCode) +
                packet.CustomArgs
        };

        CustomLogger.Debug("Starting process with args: " + startProcessInfo.Arguments);

        var processStarted = false;

        try
        {
            new Thread(() =>
            {
                try
                {
                    CustomLogger.Debug("New thread started");

                    using (var process = System.Diagnostics.Process.Start(startProcessInfo))
                    {
                        CustomLogger.Debug("Process started. Spawn Id: " + packet.SpawnId + ", pid: " + process.Id);
                        processStarted = true;

                        var processId = process.Id;

                        // Notify server that we've successfully handled the request
                        BTimer.ExecuteOnMainThread(() =>
                        {
                            message.Respond(ResponseStatus.Success);
                            controller.NotifyProcessStarted(packet.SpawnId, processId, startProcessInfo.Arguments);
                        });

                        process.WaitForExit();
                    }
                }
                catch (Exception e)
                {
                    if (!processStarted)
                        BTimer.ExecuteOnMainThread(() => { message.Respond(ResponseStatus.Failed); });

                    CustomLogger.Error("An exception was thrown while starting a process. Make sure that you have set a correct build path. " +
                                 "We've tried to start a process at: '" + path + "'. You can change it at 'SpawnerBehaviour' component");
                    CustomLogger.Error(e);
                }
                finally
                {
                    BTimer.ExecuteOnMainThread(() =>
                    {
                        // Release the port number
                        Msf.Server.Spawners.ReleasePort(port);
                        gamePorts.ReleasePort(gamePort);

                        CustomLogger.Debug("Notifying about killed process with spawn id: " + packet.SpawnerId);
                        controller.NotifyProcessKilled(packet.SpawnId);
                    });
                }

            }).Start();
        }
        catch (Exception e)
        {
            message.Respond(e.Message, ResponseStatus.Error);
            Logs.Error(e);
        }
    }

    protected override void HandleSpawnRequest(SpawnRequestPacket packet, IIncommingMessage message)
    {
        CustomSpawnRequestHandler(packet, message);
    }
}

