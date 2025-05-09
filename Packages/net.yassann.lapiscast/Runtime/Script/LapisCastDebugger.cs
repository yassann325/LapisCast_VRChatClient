
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
    [SerializeField, UdonSynced]
    public bool sendDebugEvent = true;
    [SerializeField]
    private Toggle debugModeToggle;

    [Space(20)]
    [SerializeField]
    private VRCUrlInputField EventNameInputField;
    [SerializeField]
    private Text EventNameText;
    [UdonSynced]
    public string EventName = "debugEvent";

    [Space(20)]
    [SerializeField]
    private VRCUrlInputField ValueInputField;
    [SerializeField]
    private Text ValueText;
    [UdonSynced]
    public string Value = "testValue";

    [Space(20)]
    [SerializeField]
    private VRCUrlInputField SpaceNameInputField;
    [SerializeField]
    private Text SpaceNameText;
    [UdonSynced]
    public string SpacetName = "debugSpace";

    [Space(20)]
    [UdonSynced]
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
        if(sendDebugEvent && Networking.IsMaster){
            if(timer > eventSendIntervalTime){
                timer = 0;
                SendTestEvent();
            }
            timer += Time.deltaTime;
        }
    }

    public override void OnDeserialization()
    {
        debugModeToggle.isOn = sendDebugEvent;

        EventNameText.text = EventName;
        ValueText.text = Value;
        SpaceNameText.text = SpacetName;
        SetSpaceName(SpacetName);
    }


    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if(Networking.IsOwner(gameObject)){
            RequestSerialization();
        }
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
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        sendDebugEvent = state;
        RequestSerialization();
    }

    //Send Event
    public void SendTestEvent(){
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "doTestEvent");
    }

    public void doTestEvent(){
        SendLapisCast(EventName, new DataToken(Value));
    }

    public void OnEventNameChanged(){
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        EventName = EventNameInputField.GetUrl().ToString();
        RequestSerialization();
    }

    public void OnValueChanged(){
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        Value = ValueInputField.GetUrl().ToString();
        RequestSerialization();
    }

    public void OnSpaceNameChanged(){
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        SpacetName = SpaceNameInputField.GetUrl().ToString();
        if(SpacetName == ""){
            SpacetName = "debugSpace";
        }
        RequestSerialization();
    }
}
