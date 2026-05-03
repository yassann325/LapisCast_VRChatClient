
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using LapisCast;
using TMPro;

public class LapisCastTimeClockDebug : LapisCastBehaviour
{
    [SerializeField]
    private TMP_Text text;

    void Start()
    {
        LapisCastBehaviourInit();
    }

    void Update()
    {
        if(!text){ return; }

        string [] debugTexts = {
        /* LapisCast Time */    GetLapisCastCore().GetTimestamp().ToString("F1"),
        /* LocalHostTime */     GetLapisCastCore().GetLocalHostUnixTime().ToString("F1"),
        /* UnixTime */          GetLapisCastCore().GetUnixTimestamp().ToString("F1"),
        /* StreamTime */        GetLapisCastCore().GetStreamTimestamp().ToString("F1"),
        /* Unix - Stream */     (GetLapisCastCore().GetUnixTimestamp() - GetLapisCastCore().GetStreamTimestamp()).ToString("F3")
        };
        text.text = string.Join("\n", debugTexts);       
    }
}
