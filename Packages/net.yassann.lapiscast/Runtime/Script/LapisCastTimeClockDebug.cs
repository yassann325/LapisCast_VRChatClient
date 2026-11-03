
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using LapisCast;
using TMPro;
using UnityEngine.UI;

public class LapisCastTimeClockDebug : LapisCastBehaviour
{
    [SerializeField]
    private TMP_Text text;
    [SerializeField]
    private Text uitext;

    private LapisCastCore lapisCastCore;

    void Start()
    {
        LapisCastBehaviourInit();
        lapisCastCore = GetLapisCastCore();
    }

    void Update()
    {

        string [] debugTexts = {
        /* LapisCast Time */    lapisCastCore.GetTimestamp().ToString("F1"),
        /* LocalHostTime */     lapisCastCore.GetLocalHostUnixTime().ToString("F1"),
        /* UnixTime */          lapisCastCore.GetUnixTimestamp().ToString("F1"),
        /* StreamTime */        lapisCastCore.GetStreamTimestamp().ToString("F1"),
        /* Unix - Stream */     (lapisCastCore.GetUnixTimestamp() - lapisCastCore.GetStreamTimestamp()).ToString("F3")
        };
        
        ApplyTimeText(string.Join("\n", debugTexts));     
    }

    private void ApplyTimeText(string timetext)
    {
        if (Utilities.IsValid(uitext))
        {
            uitext.text = timetext;
        }
        if (Utilities.IsValid(text))
        {
            text.text = timetext;
        }
    }
}
