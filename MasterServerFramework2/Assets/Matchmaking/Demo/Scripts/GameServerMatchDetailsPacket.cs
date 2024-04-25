using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;

public class GameServerMatchDetailsPacket : SerializablePacket
{
    public int SpawnId;
    public string MachineId;
    public int AssignedPort;
    public string SpawnCode;
    public int GamePort;

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        writer.Write(SpawnId);
        writer.Write(MachineId);
        writer.Write(AssignedPort);
        writer.Write(SpawnCode);
        writer.Write(GamePort);
    }

    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        SpawnId = reader.ReadInt32();
        MachineId = reader.ReadString();
        AssignedPort = reader.ReadInt32();
        SpawnCode = reader.ReadString();
        GamePort = reader.ReadInt32();
    }
}
