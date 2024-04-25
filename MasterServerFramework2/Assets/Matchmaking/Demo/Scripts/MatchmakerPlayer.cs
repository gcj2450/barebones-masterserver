using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;

public class MatchmakerPlayer
{
    public string Username { get; set; }
    public readonly IPeer Peer;
    public float Time { get; set; }

    protected Dictionary<string, string> Properties;

    public MatchmakerPlayer(string username, IPeer peer, float time, Dictionary<string, string> properties = null)
    {
        Username = username;
        Peer = peer;
        Time = time;
        Properties = (properties == null) ? new Dictionary<string, string>() : properties;
    }

    public void SetProperty(string key, string value)
    {
        Properties[key] = value;
    }

    public string GetProperty(string key)
    {
        string result;

        Properties.TryGetValue(key, out result);
        return result;
    }
}
