
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.Udon.Common.Interfaces;

namespace LapisCast{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public partial class LapisCastCore : UdonSharpBehaviour
    {
        [Header("LapisCast Settings"), Space(3)]

        // LapisCast Core Vals
        //Client Settings
        public float StartWaiting = 0f;
        public float LoadingInterval = 5f;
        public float MaxEventDelay = 1f;
        [SerializeField, UdonSynced]
        public bool LocalTestMode = false;
        [SerializeField, UdonSynced]
        private VRCUrl InstanceURL = new VRCUrl("https://lapis.yassann.net/lapiscast/public/{instanceid-here}");
        [SerializeField, UdonSynced]
        public bool EnableLapisCast = true;
        public bool EnableLapisCastEventExec = true;
        public bool EnableLapisCastEventOutput = true;
        public bool DisableLogOnNonWindows = true;

        //Client Values
        private VRCUrl localTestURL = new VRCUrl("http://localhost:48080/test/lapiscast");
        [UdonSynced, FieldChangeCallback(nameof(EventSpace))] private string eventSpace = "VRChat@defaultinstance";
        public string EventSpace
        {
            get => eventSpace;
            set { eventSpace = value; OnEventSpaceChanged(); }
        }
        private string log_prefix = "__LapisCast__";
        private string error_prefix = "__LapisCastError__";
        //UploadData Cache
        private DataDictionary uploadDataDict = new DataDictionary();
        //SendEvent Frame
        private DataDictionary messageFrameDict = new DataDictionary(){
            {"namespace",""},
            {"eventspace",""},
            {"eventkey",""},
            {"value",""}
        };
        private DataDictionary downloadDataDict = new DataDictionary();
        private LapisCastBehaviour[] lapisCastBehaviours = new LapisCastBehaviour[0];
        private double _lastAppliedTimestamp = 0;

        //Timer
        private float download_timer = 0;
        private float upload_timer = 0;

        private VRCUrl timeAdjustmentUrl = new VRCUrl("https://lapis.yassann.net/lapiscast/public/00000000-0000-0000-0000-000000000000");


        void Start()
        {
            // Init Clock
            ClockInit();

            EventManagerInit();

            //Setting Timer
            download_timer = LoadingInterval;
            download_timer -= StartWaiting;

            //Init current instance EventSpaceId
            if(Networking.IsOwner(Networking.LocalPlayer, gameObject)){
                int instancehash = $"{Networking.LocalPlayer.displayName}{DateTime.Now.Millisecond}".GetHashCode();
                string EventSpaceId = Mathf.Abs(instancehash).ToString();
                while(EventSpaceId.Length < 8){
                    EventSpaceId = $"{EventSpaceId}0";
                }
                EventSpace = $"VRChat@{EventSpaceId}";
                RequestSerialization();
            }

            // If Empty Instance URL Access for time correction
            if(InstanceURL.ToString().Length == 0){
                VRCStringDownloader.LoadUrl(timeAdjustmentUrl, (IUdonEventReceiver)this);
            }
        }

        private void Update() {
            // Update Clock
            ClockUpdate();
            
            // StringLoading Loop
            if(download_timer >= LoadingInterval){
                //Trigger StringLoading
                StringDownload();
                download_timer = 0;
            }
            else{
                download_timer += Time.deltaTime;
            }

            PlayTimeline();

            // Export Evnt Log 40fps
            if(upload_timer > 0.025f){
                if(uploadDataDict.Count != 0){
                    OutputLog();
                }
                upload_timer = 0;
            }
            else{
                upload_timer += Time.deltaTime;
            }
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if(Networking.IsOwner(Networking.LocalPlayer, gameObject)){
                RequestSerialization();
            }
        }

        private void OnEventSpaceChanged()
        {
            //Setting EventSpace
            messageFrameDict.SetValue("eventspace", EventSpace);
        }

        //Get Instance Data
        private void StringDownload(){
            if(!(EnableLapisCastEventExec && EnableLapisCast)) return;
            if(LocalTestMode){
                VRCStringDownloader.LoadUrl(localTestURL, (IUdonEventReceiver)this);
            }else{
                if (InstanceURL.ToString().Length == 0){ return; }
                VRCStringDownloader.LoadUrl(InstanceURL, (IUdonEventReceiver)this);
            }
        }

        public override void OnStringLoadSuccess(IVRCStringDownload downloadresult)
        {
            string jsonstring = downloadresult.Result;
            //Debug.Log($"DownLoadData= {jsonstring}");
            if(VRCJson.TryDeserializeFromJson(jsonstring, out DataToken result))
            {
                if(result.TokenType != TokenType.DataDictionary){
                    Debug.LogError("DataType not DataDictionary");
                    Debug.Log($"{error_prefix}StringLoadSuccess. But DataType not DataDictionary");
                    return;
                }
                //Debug.Log("OnStringLoadSuccess");

                if(result.DataDictionary.TryGetValue("prop", TokenType.DataDictionary, out DataToken propValue)){
                    if(propValue.DataDictionary.TryGetValue("servtime", TokenType.Double, out DataToken servtime)){
                        AdjustTimelineClock(servtime.Double, -0.1);
                    }
                }
                else{
                    Debug.LogError("Prop Data not find.");
                    return;
                }

                if(result.DataDictionary.TryGetValue("ts", TokenType.DataList, out DataToken tsList)){
                    if(result.DataDictionary.TryGetValue("dls", TokenType.DataList, out DataToken eventList)){
                        DecodeEvent(tsList.DataList, eventList.DataList);
                    }
                    else{
                        Debug.LogError("EventData List not find.");
                        return;
                    }
                }
                else{
                    Debug.LogError("Timestamp List not find.");
                    return;
                }
            }
            else
            {
                Debug.LogError(result.ToString());
            }
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            Debug.LogError(result.Error);
            Debug.Log($"{error_prefix}{result.Error}");
        }

        //Call per TimeStamp
        private void CallFlameEvents(double timestamp, DataDictionary messageFrame){
            //Debug.Log("CallFlameEvents");
            string spacename = "";
            string eventspace = "";
            string eventkey = "";
            DataToken value = new DataToken("");

            if(messageFrame.TryGetValue("ns", out DataToken _spacename)){
                spacename = _spacename.String;
            }
            else{return;}
            if(messageFrame.TryGetValue("es", out DataToken _eventspace)){
                eventspace = _eventspace.String;
            }
            else{return;}
            if(messageFrame.TryGetValue("ky", out DataToken _eventkey)){
                eventkey = _eventkey.String;
            }
            else{return;}
            if(messageFrame.TryGetValue("vl", out DataToken _value)){
                value = _value;
            }
            else{return;}
            
            CallBehaviours(timestamp, spacename, eventspace, eventkey, value);
        }
        //Call per Namespace
        private void CallBehaviours(double timestamp, string spacename,string eventspace, string keyname, DataToken value){
            // Debug.Log($"CallBehaviours {timestamp} {spacename} {eventspace} {keyname}");
            bool sameinstance = EventSpace == eventspace;
            for(int i = 0; i < lapisCastBehaviours.Length; i++){
                lapisCastBehaviours[i]._triggerLapisEvent(timestamp, spacename, keyname, value, sameinstance);
            }
        }

        //Upload Instance Data
        public void AddEvent(string spacename, string keyname, DataToken value){
            if(!(EnableLapisCastEventOutput && EnableLapisCast)) return;

            #if !(UNITY_EDITOR || UNITY_STANDALONE_WIN)
                if (DisableLogOnNonWindows) return;
            #endif

            //set MessageFrame
            messageFrameDict.SetValue("namespace", spacename);
            messageFrameDict.SetValue("eventkey", keyname);
            messageFrameDict.SetValue("value", value);

            string timestamp = GetUnixTimestamp().ToString();
            
            //Add eventmessage to uploadDataDict
            if(uploadDataDict.TryGetValue(timestamp, out DataToken timelineColumn)){
                if(timelineColumn.TokenType != TokenType.DataList){
                    //if Invalid Delete it
                    uploadDataDict.Remove(timestamp);
                }
                else{
                    timelineColumn.DataList.Add(messageFrameDict.DeepClone());
                }
            }
            else{
                uploadDataDict.SetValue(timestamp, new DataList());
                ((DataList)uploadDataDict[timestamp]).Add(messageFrameDict.DeepClone());
            }
        }

        //Output DebugLog Text
        private void OutputLog(){
            if(!(EnableLapisCastEventOutput && EnableLapisCast))
            {
                uploadDataDict.Clear();
                return;
            }
            if(uploadDataDict.Count == 0){
                return;
            }
            if (VRCJson.TrySerializeToJson(uploadDataDict, JsonExportType.Minify, out DataToken result))
            {
                //string _json = result.String;
                Debug.Log($"{log_prefix}{result.String}");
                uploadDataDict.Clear();
            }
            else
            {
                Debug.LogError(result.ToString());
            }
        }

        //subscribe client behaviours
        public LapisCastCore _subscribe_behaviour(LapisCastBehaviour behaviour){
            LapisCastBehaviour[] newList = new LapisCastBehaviour[lapisCastBehaviours.Length + 1];
            for (int i = 0;i < lapisCastBehaviours.Length; i++){
                newList[i] = lapisCastBehaviours[i];
            }
            newList[lapisCastBehaviours.Length] = behaviour;

            lapisCastBehaviours = newList;
            return this;
        }

        // LapisCast Param Setting
        public void SetLocalTestMode(bool state)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            LocalTestMode = state;
            RequestSerialization();
        }
        public bool GetLocalTestMode() { return LocalTestMode; }

        public void SetInstanceUrl(VRCUrl url)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            InstanceURL = url;
            RequestSerialization();
        }
        public VRCUrl GetInstanceUrl() { return InstanceURL; }

        public void SetLapisCastEnable(bool state)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            EnableLapisCast = state;
            RequestSerialization();
        }
        public bool GetLapisCastEnable() { return EnableLapisCast; }

    }
}

