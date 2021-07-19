using System;
using System.Collections;
using System.Collections.Generic;
// If type or namespace TwitchLib could not be found. Make sure you add the latest TwitchLib.Unity.dll to your project folder
// Download it here: https://github.com/TwitchLib/TwitchLib.Unity/releases
// Or download the repository at https://github.com/TwitchLib/TwitchLib.Unity, build it, and copy the TwitchLib.Unity.dll from the output directory
using TwitchLib.Unity;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Api.Helix.Models.Users;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;

public class TwitchAPI : MonoBehaviour
{
	private Api _api;

    public ChannelsToWatchData channelsToWatch = new ChannelsToWatchData();
    public Credentials credentialsData = new Credentials();
    public int watchingChannel = -1;

    public string pathToExe = "";

    public int freq = 30;

    [Header("UI")]
    public Button startButton;
    public Button stopButton;
    public Button login;
    public Button add;
    public Button credentials;

    [Header("Add Stream")]
    public WatchingStreamItem watchingStreamItem;
    public RectTransform rectTransform;

    public TMP_InputField channelName;
    public TMP_InputField game;
    public TMP_InputField timeToWatch;

    [Header("Auth")]
    public TMP_InputField twitchID;
    public TMP_InputField oauthToken;

    public TextMeshProUGUI status;



    private Dictionary<string, WatchingStreamItem> uiItems = new Dictionary<string, WatchingStreamItem>();

    private void Start() {
        startButton.onClick.AddListener(StartMonitor);
        stopButton.onClick.AddListener(StopMonitor);
        login.onClick.AddListener(OpenForLogin);
        credentials.onClick.AddListener(CredentialsWeb);

        add.onClick.AddListener(AddChannel);

        stopButton.gameObject.SetActive(false);

        Debug.Log(Application.persistentDataPath);

        if(!File.Exists(Application.persistentDataPath + "/data.json")){
            string jsonContent = JsonUtility.ToJson(channelsToWatch);
            File.WriteAllText(Application.persistentDataPath + "/data.json", jsonContent);
        }
        else{
            string jsonContent = File.ReadAllText(Application.persistentDataPath + "/data.json");
            channelsToWatch = JsonUtility.FromJson<ChannelsToWatchData>(jsonContent);

            foreach(var channel in channelsToWatch.channels){
                WatchingStreamItem wi = Instantiate(watchingStreamItem, rectTransform);
                wi.SetData(channel, RemoveItem, PosChange);
                uiItems.Add(channel.internalID, wi);
            }
        }

        if(!File.Exists(Application.persistentDataPath+ "/credentials.json")){
            string jsonContent = JsonUtility.ToJson(credentialsData);
            File.WriteAllText(Application.persistentDataPath+ "/credentials.json", jsonContent);
        }
        else{
            string jsonContent = File.ReadAllText(Application.persistentDataPath+ "/credentials.json");
            credentialsData = JsonUtility.FromJson<Credentials>(jsonContent);
            oauthToken.text = credentialsData.oauth;
            twitchID.text = credentialsData.client;
        }
    }

    private void PosChange(int oldPos, int newPosition)
    {
        StopMonitor();

        Debug.Log($"Moved object from: {oldPos} to {newPosition}");

        channelsToWatch.channels.Move(oldPos, newPosition);

       
        UpdateJSON();

    }

    private void AddChannel()
    {
        if(!string.IsNullOrEmpty(timeToWatch.text) && !string.IsNullOrEmpty(channelName.text) && !string.IsNullOrEmpty(game.text)){
            ChannelInfo ci = new ChannelInfo();
            ci.channelName = channelName.text;
            ci.secondsToWatch = int.Parse(timeToWatch.text) * 60;
            ci.game = game.text;
            ci.internalID = Guid.NewGuid().ToString();

            channelsToWatch.channels.Add(ci);

            WatchingStreamItem wi = Instantiate(watchingStreamItem, rectTransform);
            wi.SetData(ci, RemoveItem, PosChange);

            uiItems.Add(ci.internalID, wi);

            UpdateJSON();

            timeToWatch.text = "";
            game.text = "";
            channelName.text = "";
        }
    }

    private void UpdateJSON()
    {
        string jsonContent = JsonUtility.ToJson(channelsToWatch);
        File.WriteAllText(Application.persistentDataPath + "/data.json", jsonContent);
    }

    private void RemoveItem(ChannelInfo obj)
    {
        StopMonitor();

        WatchingStreamItem wi = uiItems[obj.internalID];

        channelsToWatch.channels.RemoveAll( c => c.internalID == wi.channelInfo.internalID);

        Destroy(wi.gameObject);

        UpdateJSON();

    }

    private void CredentialsWeb()
    {
        System.Diagnostics.Process.Start(pathToExe, $"https://twitchtokengenerator.com/ --new-window"); 
    }

    public void StartMonitor()
	{
        
        CloseBrowser();
		// To keep the Unity application active in the background, you can enable "Run In Background" in the player settings:
		// Unity Editor --> Edit --> Project Settings --> Player --> Resolution and Presentation --> Resolution --> Run In Background
		// This option seems to be enabled by default in more recent versions of Unity. An aditional, less recommended option is to set it in code:
		// Application.runInBackground = true;

		// Create new instance of Api
		_api = new Api();

		// The api needs a ClientID or an OAuth token to start making calls to the api.

        credentialsData.client = twitchID.text;
        credentialsData.oauth = oauthToken.text;

        _api.Settings.ClientId = credentialsData.client;
        _api.Settings.AccessToken = credentialsData.oauth;

        string jsonContent = JsonUtility.ToJson(credentialsData);
        File.WriteAllText(Application.persistentDataPath+ "/credentials.json", jsonContent);

        StartCoroutine(Monitor(freq));

        
        stopButton.gameObject.SetActive(true);
        startButton.gameObject.SetActive(false);

	}

    public void StopMonitor()
	{
        StopAllCoroutines();
        CloseBrowser();

        watchingChannel = -1;
        
        stopButton.gameObject.SetActive(false);
        startButton.gameObject.SetActive(true);
    }

    public void OpenForLogin(){
        System.Diagnostics.Process.Start(pathToExe, $"https://www.twitch.tv/ --new-window"); 
    }

    System.Diagnostics.Process browserProcess = null; 

    private IEnumerator Monitor(int frequency)
    {        
        int potentialChannel = -1;

        while(true){

            potentialChannel = -1;
            
            if(watchingChannel < 0 || watchingChannel > 0){
                for(int i = 0; i < channelsToWatch.channels.Count; i++){

                    bool channelOnline = false;
                    ChannelInfo checkingStream = channelsToWatch.channels[i];

                    if(i >= watchingChannel && watchingChannel >= 0){
                        break;
                    }
                    
                    if(string.IsNullOrEmpty(checkingStream.id)){
                        status.text = "Check online to watch? "+ checkingStream.channelName;

                        GetUsersResponse getUsersResponse = null;
                        yield return _api.InvokeAsync(_api.Helix.Users.GetUsersAsync(logins: new List<string> { checkingStream.channelName }),
                                        (response) => {
                            getUsersResponse = response;
                        });

                        if(getUsersResponse.Users.Length == 0) continue;

                        status.text = "Got id for stream "+ getUsersResponse.Users[0].Id;

                        checkingStream.id = getUsersResponse.Users[0].Id;

                        channelsToWatch.channels[i] = checkingStream;
                    }
		
		            
                    yield return _api.InvokeAsync(_api.V5.Streams.BroadcasterOnlineAsync(checkingStream.id),
									  (response) => channelOnline = response);


                    TwitchLib.Api.V5.Models.Streams.StreamByUser liveStream = null;

                    yield return _api.InvokeAsync(_api.V5.Streams.GetStreamByUserAsync(checkingStream.id),
									  (response) => liveStream = response);

                    status.text = channelOnline ? $"<color=green>{checkingStream.channelName} Online -- Playing: {liveStream.Stream.Game}</color>" : $"<color=red>{checkingStream.channelName} Offline </color>";
                     

                    if(channelOnline && IsStreamingDesiredGame(liveStream, checkingStream)){
                        potentialChannel = i;
                        break;
                    }
                }

                
            }

            if(potentialChannel >= 0){

                Debug.Log(potentialChannel);

                if(watchingChannel >= 0){
                    ChannelInfo oldStream = channelsToWatch.channels[watchingChannel];
                    oldStream.watching = false;
                    channelsToWatch.channels[watchingChannel] = oldStream;
                    CloseBrowser();
                }

                ChannelInfo checkingStream = channelsToWatch.channels[potentialChannel];
                checkingStream.watching = true;
                watchingChannel = potentialChannel;
                channelsToWatch.channels[watchingChannel] = checkingStream;
                
                Debug.Log("Searching new to watch?");

                if(watchingChannel >= 0){
                    browserProcess = System.Diagnostics.Process.Start(pathToExe, $"https://www.twitch.tv/{channelsToWatch.channels[watchingChannel].channelName} --new-window"); 
                    yield return new WaitForSeconds(5);
                }
            }
            else if(watchingChannel >= 0){
                bool channelStillOnline = false;
                var channelInfo = channelsToWatch.channels[watchingChannel];

                yield return _api.InvokeAsync(_api.V5.Streams.BroadcasterOnlineAsync(channelInfo.id),
									  (response) => channelStillOnline = response);

                TwitchLib.Api.V5.Models.Streams.StreamByUser liveStream = null;

                yield return _api.InvokeAsync(_api.V5.Streams.GetStreamByUserAsync(channelInfo.id),
									  (response) => liveStream = response);

                if(!channelStillOnline || !IsStreamingDesiredGame(liveStream, channelInfo)){
                    channelInfo.watching = false;
                    channelsToWatch.channels[watchingChannel] = channelInfo;
                    watchingChannel = -1;
                    CloseBrowser();
                }
                else{
                    channelInfo.secondsWatched += frequency;

                    channelsToWatch.channels[watchingChannel] = channelInfo;

                    if(channelInfo.secondsWatched >=  channelInfo.secondsToWatch){
                        channelsToWatch.channels.RemoveAt(watchingChannel);

                        WatchingStreamItem wi = uiItems[channelInfo.internalID];

                        Destroy(wi.gameObject);

                        watchingChannel = -1;

                        CloseBrowser();
                    }
                    else{
                        WatchingStreamItem wi = uiItems[channelInfo.internalID];
                        wi.UpdateData(channelsToWatch.channels[watchingChannel]);
                    }

                    UpdateJSON();
                }
            }

            yield return new WaitForSeconds(frequency);
        }
    }

    public bool IsStreamingDesiredGame(TwitchLib.Api.V5.Models.Streams.StreamByUser liveStream, ChannelInfo checkingStream ){
        bool game = false;
                    
        if(liveStream != null && liveStream.Stream != null){
            Debug.Log(liveStream.Stream.Game.ToLower());
            game = liveStream.Stream.Game.ToLower().Contains(checkingStream.game.ToLower()) || (checkingStream.game == "*");
        }   

        return game;
    }

    public void CloseBrowser(){
        Debug.Log("Close browser?");

        try
        {
            browserProcess?.Kill();
        }
        catch (System.Exception)
        {
            
        }
        
        browserProcess = null;

        System.Diagnostics.Process [] edgeInstances = System.Diagnostics.Process.GetProcessesByName("msedge");

        Debug.Log(" Trying to close: " + edgeInstances.Length);

        foreach(System.Diagnostics.Process p in edgeInstances){
            p.Kill();
        }
    }
	
}

[System.Serializable]
public class Credentials{
    public string client;
    public string oauth;
}