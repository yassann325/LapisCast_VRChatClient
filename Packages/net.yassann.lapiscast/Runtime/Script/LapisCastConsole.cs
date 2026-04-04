
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
    private LapisCastCore LapisCast;

    [SerializeField]
    private VRCUrlInputField urlInputField;
    [SerializeField]
    private Text UrlText;
    [SerializeField]
    private TMP_Text consoleText;
    private DataList consoleLogList = new DataList();


    [Space(20)]
    private string _currentPanel = "log";

    [SerializeField]
    private Toggle lapisCastEnableCheck;

    [Header("Menu Panels")]
    [SerializeField]
    private CanvasGroup logPanel;
    [SerializeField]
    private CanvasGroup debugPanel;
    [SerializeField]
    private CanvasGroup settingPanel;

    [Header("Menu Toggles")]
    [SerializeField]
    private Toggle logPanelCheck;
    [SerializeField]
    private Toggle debugPanelCheck;
    [SerializeField]
    private Toggle settingPanelCheck;

    [Header("Debug Menu Things")]
    [SerializeField]
    private Toggle lapisCastLocalTestModeCheck;

    [Header("Debug Menu Things")]
    [SerializeField]
    private Toggle LapisCastEventExecToggle;
    [SerializeField]
    private Toggle LapisCastEventOutputToggle;


    void Start()
    {
        LapisCastBehaviourInit();
        LapisCast = GetLapisCastCore();

        ClearConsoleText();

        SetPanelActive(_currentPanel);

        LapisCastEventExecToggle.isOn = LapisCast.EnableLapisCastEventExec;
        LapisCastEventOutputToggle.isOn = LapisCast.EnableLapisCastEventOutput;
    }

    private void Update()
    {
        UrlText.text = LapisCast.GetInstanceUrl().ToString();
        lapisCastEnableCheck.isOn = LapisCast.GetLapisCastEnable();
        lapisCastLocalTestModeCheck.isOn = LapisCast.GetLocalTestMode();
    }


    // del require
    public void ClearConsoleText(){
        if(consoleText){
            consoleText.text = "";
        }
    }

    public override void OnLapisCastAllEvent(double timestamp, string spanename, string keyname, DataToken value, bool sameinstance)
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

    public void OnEnterURL(){
        LapisCast.SetInstanceUrl(urlInputField.GetUrl());
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
        bool settingPanelState = false;
        
        if(panelName == "log"){
            logPanelState = true;
        }
        else if(panelName == "debug"){
            debugPanelState = true;
        }
        else if (panelName == "setting")
        {
            settingPanelState = true;
        }
        logPanelCheck.isOn = logPanelState;
        debugPanelCheck.isOn = debugPanelState;
        settingPanelCheck.isOn = settingPanelState;
        applyCanvasGroupSetting(logPanel, logPanelState);
        applyCanvasGroupSetting(debugPanel, debugPanelState);
        applyCanvasGroupSetting(settingPanel, settingPanelState);
    }

    public void SetLogPanel(){
        _currentPanel = "log";
        SetPanelActive(_currentPanel);
    }

    public void SetDebugPanel(){
        _currentPanel = "debug";
        SetPanelActive(_currentPanel);
    }

    public void SetSettingPanel(){
        _currentPanel = "setting";
        SetPanelActive(_currentPanel);
    }

    public void ToggleLapisCastEnable()
    {
        LapisCast.SetLapisCastEnable(!LapisCast.GetLapisCastEnable());
    }

    public void ToggleLapisCastLocalTestMode()
    {
        LapisCast.SetLocalTestMode(!LapisCast.GetLocalTestMode());
    }

    public void ToggleLapisCastEventExec()
    {
        LapisCast.EnableLapisCastEventExec = LapisCastEventExecToggle.isOn;
    }

    public void ToggleLapisCastEventOutput()
    {
        LapisCast.EnableLapisCastEventOutput = LapisCastEventOutputToggle.isOn;
    }
}
