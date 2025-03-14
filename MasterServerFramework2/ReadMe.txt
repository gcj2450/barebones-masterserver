Start Master Server and Spawner
./MasterAndSpawner -msfStartMaster -msfStartSpawner -msfMachineIp xxx.xxx.xxx.xxx

⚠️ When starting a spawner, you should always set -msfMachineIp to the public IP of your server (the one that http://checkip.dyndns.org would give you). This address will be passed to spawned game servers.

ℹ️ This is not necessary when you run locally.

Start Master Only
./MasterAndSpawner -msfStartMaster

Start Spawner Only
./MasterAndSpawner -msfStartSpawner -msfMasterIp xxx.xxx.xxx.xxx

If you don't give an address, it will try connect to "127.0.0.1". This is all good when your master server runs locally, but when not - you'll need to give the IP

Start Spawner And Give A Path to Game Server Executables
./MasterAndSpawner -msfStartSpawner -msfExe 'D:/GameServer.exe'

Start Spawner With A Limited Capacity
./MasterAndSpawner -msfStartSpawner -msfMaxProcesses 5

Start Spawned Game servers with Websocket connection (WebGL support)
./MasterAndSpawner -msfStartSpawner -msfWebgl