
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
using LapisCast;
using VRC.SDK3.Data;
using UnityEngine.UI;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LapisCastConsole : LapisCastBehaviour
{
    [SerializeField]
    private LapisCastCore LapisCast;
    [SerializeField]
    private VRCUrlInputField urlInputField;
    [SerializeField]
    private Text UrlText;
    [SerializeField]
    private TMP_Text consoleText;
    private DataList consoleLogList = new DataList();

    [UdonSynced]
    public VRCUrl lapiscastUrl = new VRCUrl("");

    [Space(20)]
    [UdonSynced]
    private string _currentPanel = "log";
    [SerializeField]
    private CanvasGroup logPanel;
    [SerializeField]
    private CanvasGroup debugPanel;

    [SerializeField]
    private Toggle logPanelCheck;
    [SerializeField]
    private Toggle debugPanelCheck;

    void Start()
    {
        LapisCastBehaviourInit();
        ClearConsoleText();
        lapiscastUrl = LapisCast.InstanceURL;

        ApplyNewURL();
        SetPanelActive(_currentPanel);
    }

    private void Update()
    {
        ApplyNewURL();
        SetPanelActive(_currentPanel);
    }


    public void ClearConsoleText(){
        if(consoleText){
            consoleText.text = "";
        }
    }

    public override void OnLapisCastAllEvent(string spanename, string keyname, DataToken value, bool sameinstance)
    {
        string logLine = $"{spanename} | {keyname}/{value}";
        if (logLine.Length > 50)
        {
            logLine = logLine.Substring(0, 50);
        }
        consoleLogList.Add(logLine+"\n");
        
        if (consoleText)
        {       
            if(consoleLogList.Count >= 16){
                consoleLogList.RemoveAt(0);
            }
            string logText = "";
            for (int i = consoleLogList.Count-1; i >= 0; i--)
            {
                logText += consoleLogList[i].String;
            }
            consoleText.text = logText;
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if(Networking.IsOwner(gameObject)){
            RequestSerialization();
        }
    }

    private void ApplyNewURL(){
        LapisCast.InstanceURL = lapiscastUrl;
        UrlText.text = lapiscastUrl.ToString();
    }

    public void OnEnterURL(){
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        lapiscastUrl = urlInputField.GetUrl();
        RequestSerialization();
    }

    // panel
    private void applyCanvasGroupSetting(CanvasGroup canvasGroup, bool state){
        if(state){
            canvasGroup.alpha = 1;
        }
        else{
            canvasGroup.alpha = 0;
        }
        canvasGroup.interactable = state;
        canvasGroup.blocksRaycasts = state;
    }
    public void SetPanelActive(string panelName){
        bool logPanelState = false;
        bool debugPanelState = false;
        
        if(panelName == "log"){
            logPanelState = true;
        }
        else if(panelName == "debug"){
            debugPanelState = true;
        }
        logPanelCheck.isOn = logPanelState;
        debugPanelCheck.isOn = debugPanelState;
        applyCanvasGroupSetting(logPanel, logPanelState);
        applyCanvasGroupSetting(debugPanel, debugPanelState);
    }

    public void SetLogPanel(){
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _currentPanel = "log";
        RequestSerialization();
    }

    public void SetDebugPanel(){
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _currentPanel = "debug";
        RequestSerialization();
    }
}
