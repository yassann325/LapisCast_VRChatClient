
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine;
using VRC.SDK3.Components;
using LapisCast;
using VRC.SDK3.Data;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LapisCastDebugger : LapisCastBehaviour
{
    public bool sendDebugEvent = false;
    [SerializeField]
    private Toggle debugModeToggle;

    [Space(20)]
    [SerializeField]
    private VRCUrlInputField EventNameInputField;
    [SerializeField]
    private Text EventNameText;
    public string EventName = "debugEvent";

    [Space(20)]
    [SerializeField]
    private VRCUrlInputField ValueInputField;
    [SerializeField]
    private Text ValueText;
    public string Value = "testValue";

    [Space(20)]
    [SerializeField]
    private VRCUrlInputField SpaceNameInputField;
    [SerializeField]
    private Text SpaceNameText;
    public string SpacetName = "debugSpace";

    [Space(20)]
    private float eventSendIntervalTime = 1;
    private float timer = 0;

    void Start()
    {
        if(SpacetName == ""){
            SpacetName = "debugSpace";
        }
        LapisCastBehaviourInit(SpacetName);
        EventNameText.text = EventName;
        ValueText.text = Value;
        SpaceNameText.text = SpacetName;
    }

    private void Update() {
        if(sendDebugEvent && timer > eventSendIntervalTime){
            timer = 0;
            SendTestEvent();
        }
        timer += Time.deltaTime;
    }

    public void OnDebugParamChanged()
    {
        debugModeToggle.isOn = sendDebugEvent;

        EventNameText.text = EventName;
        ValueText.text = Value;
        SpaceNameText.text = SpacetName;
        SetSpaceName(SpacetName);
    }


    // Set SendDebugEvent State
    public void ToggleSendDebugEvent(){
        SetSendDebugEventState(!sendDebugEvent);
    }
    public void EnableSendDebugEvent(){
        SetSendDebugEventState(true);
    }
    public void DisableSendDebugEvent(){
        SetSendDebugEventState(false);
    }
    public void SetSendDebugEventState(bool state){
        sendDebugEvent = state;
        OnDebugParamChanged();
    }

    //Send Event
    public void SendTestEvent(){
        SendLapisCast(EventName, new DataToken(Value));
    }

    public void OnEventNameChanged(){
        EventName = EventNameInputField.GetUrl().ToString();
        OnDebugParamChanged();
    }

    public void OnValueChanged(){
        Value = ValueInputField.GetUrl().ToString();
        OnDebugParamChanged();
    }

    public void OnSpaceNameChanged(){
        SpacetName = SpaceNameInputField.GetUrl().ToString();
        if(SpacetName == ""){
            SpacetName = "debugSpace";
        }
        OnDebugParamChanged();
    }
}
