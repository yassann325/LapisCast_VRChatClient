
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
    private string _currentPanel = "log";
    [UdonSynced, FieldChangeCallback(nameof(LapisCastEnable))]
    private bool _lapisCastEnable = true;
    [UdonSynced, FieldChangeCallback(nameof(LapisCastLocalTestMode))]
    private bool _lapisCastLocalTestMode = false;
    [SerializeField]
    private Toggle lapisCastEnableCheck;
    [SerializeField]
    private CanvasGroup logPanel;
    [SerializeField]
    private CanvasGroup debugPanel;

    [SerializeField]
    private Toggle logPanelCheck;
    [SerializeField]
    private Toggle debugPanelCheck;
    [SerializeField]
    private Toggle lapisCastLocalTestModeCheck;


    public bool LapisCastEnable
    {
        get => _lapisCastEnable;
        set { _lapisCastEnable = value; lapisCastEnableCheck.isOn = value; LapisCast.EnableLapisCast = value; }
    }
    public bool LapisCastLocalTestMode
    {
        get => _lapisCastLocalTestMode;
        set { _lapisCastLocalTestMode = value; lapisCastLocalTestModeCheck.isOn = value; LapisCast.LocalTestMode = value; }
    }

    void Start()
    {
        LapisCastBehaviourInit();
        ClearConsoleText();
        lapiscastUrl = LapisCast.InstanceURL;
        if (Networking.IsOwner(Networking.LocalPlayer, gameObject))
        {
            LapisCastEnable = LapisCast.EnableLapisCast;
            LapisCastLocalTestMode = LapisCast.LocalTestMode;
        }

        ApplyNewURL();
        SetPanelActive(_currentPanel);
    }

    private void Update()
    {
        ApplyNewURL();
    }


    // del require
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
        _currentPanel = "log";
        SetPanelActive(_currentPanel);
    }

    public void SetDebugPanel(){
        _currentPanel = "debug";
        SetPanelActive(_currentPanel);
    }

    public void ToggleLapisCastEnable()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        LapisCastEnable = !LapisCastEnable;
        RequestSerialization();
    }

    public void ToggleLapisCastLocalTestMode()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        LapisCastLocalTestMode = !LapisCastLocalTestMode;
        RequestSerialization();
    }
}
