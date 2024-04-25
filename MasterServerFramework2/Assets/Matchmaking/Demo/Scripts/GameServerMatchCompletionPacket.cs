using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;

public class GameServerMatchCompletionPacket : SerializablePacket
{
    public int SpawnId;
    public bool Success;

    public override void ToBinaryWriter(EndianBinaryWriter writer)
    {
        writer.Write(SpawnId);
        writer.Write(Success);
    }

    public override void FromBinaryReader(EndianBinaryReader reader)
    {
        SpawnId = reader.ReadInt32();
        Success = reader.ReadBoolean();
    }
}
