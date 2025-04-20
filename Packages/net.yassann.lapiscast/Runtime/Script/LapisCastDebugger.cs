
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine;
using LapisCast;
using VRC.SDK3.Data;
using UnityEngine.UI;

public class LapisCastDebugger : LapisCastBehaviour
{
    public Text LogText = null;
    public bool SendDebugEvent = true;
    private float timer = 0;
    void Start()
    {
        LapisCastBehaviourInit("UdonDebugger");
        if(LogText){
            LogText.text = "";
        }
    }

    private void Update() {
        if(SendDebugEvent){
            if(timer > 1){
                timer = 0;
                SendTestEvent();
            }
            timer += Time.deltaTime;
        }
    }


    //Send Event
    public void SendTestEvent(){
        SendLapisCast("DebugerEvent", new DataToken("Debuger Hello!"));
    }

    //Receive All Events
    public override void OnLapisCastAllEvent(string spanename, string keyname, DataToken value, bool sameinstance)
    {
        if(LogText){
            LogText.text += $"{spanename} | {keyname}/{value}  sameinstance {sameinstance}\n";
        }
        else{
            Debug.Log($"{spanename} | {keyname}/{value}  sameinstance {sameinstance}");
        }
    }

    //Receive UdonDebugger Event
    public override void OnLapisCastEvent(string spanename, string keyname, DataToken value, bool sameinstance)
    {
        Debug.Log($"{spanename} | {keyname}/{value}  sameinstance {sameinstance}");
    }
}
