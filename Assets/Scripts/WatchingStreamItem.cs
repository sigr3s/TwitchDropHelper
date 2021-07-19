using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class WatchingStreamItem : MonoBehaviour {
    public Button remove;
    public TextMeshProUGUI channelName;
    public TextMeshProUGUI game;
    public Image fill;

    public ChannelInfo channelInfo;
    Action<ChannelInfo> tryRemove;
    Action<int, int> positionChnage;

    private void Awake() {
        GetComponent<DragController>().onPositionChange.AddListener(StreamPriorityChanged);
    }

    private void StreamPriorityChanged(int oldPos, int newPos)
    {
        positionChnage.Invoke(oldPos, newPos);
    }

    public void SetData(ChannelInfo info, Action<ChannelInfo> tryRemove, Action<int, int> positionChange){
        remove.onClick.AddListener(RemoveThis);
        channelName.text = info.channelName;
        game.text = info.game;

        this.channelInfo = info;
        this.tryRemove = tryRemove;
        this.positionChnage = positionChange;

        fill.fillAmount = ((float)info.secondsWatched / (float)info.secondsToWatch);
    }

    private void RemoveThis()
    {
        tryRemove.Invoke(channelInfo);
    }

    public void UpdateData(ChannelInfo ci)
    {
        fill.fillAmount = ((float) ci.secondsWatched / (float) ci.secondsToWatch);
    }
}