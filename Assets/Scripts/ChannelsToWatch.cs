using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChannelsToWatch", menuName = "Twitch/ChannelsToWatch", order = 0)]
public class ChannelsToWatch : ScriptableObject {
    public List<ChannelInfo> channels = new List<ChannelInfo>();
}


[System.Serializable]
public struct ChannelInfo{
    public string channelName;
    public string game;
    public string id;
    public int secondsToWatch;
    public int secondsWatched;
    public bool watching;
    public string internalID;
}