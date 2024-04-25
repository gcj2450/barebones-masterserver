using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Match
{
    public int SpawnId;
    public List<MatchmakerPlayer> Players;
    public string MachineId { get; set; }
    public int AssignedPort { get; set; }
    public string SpawnCode { get; set; }
    public int GamePort { get; set; }

    public Match(int spawnId, List<MatchmakerPlayer> players)
    {
        this.SpawnId = spawnId;
        this.Players = players;
        this.MachineId = string.Empty;
        this.SpawnCode = string.Empty;
    }
}
