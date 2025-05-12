
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
    public class LapisCastCore : UdonSharpBehaviour
    {
        //Client Settings
        public float StartWaiting = 0f;
        public float LoadingInterval = 5f;
        public float TimeLineOffset = 7f;
        public float MaxEventDelay = 1f;
        public bool LocalTestMode = false;
        public VRCUrl InstanceURL = new VRCUrl("https://lapis.yassann.net/lapiscast/public/{instanceid-here}");
        public bool EnableLapisCast = true;

        //Client Values
        private VRCUrl localTestURL = new VRCUrl("http://localhost:48080/test/lapiscast");
        [UdonSynced]
        private string EventSpaceId = "defaultinstance";
        private string EventSpace = "VRChat@defaultinstance";
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

        //TimelineClock
        private LapisCastTimelineClock timelineClock;


        void Start()
        {        
            //Setting Timer
            download_timer = LoadingInterval;
            download_timer -= StartWaiting;
            //Init current instance EventSpaceId
            if(Networking.IsOwner(Networking.LocalPlayer, gameObject)){
                if(EventSpaceId == "defaultinstance"){
                    int instancehash = $"{Networking.LocalPlayer.displayName}{DateTime.Now.Millisecond}".GetHashCode();
                    EventSpaceId = Mathf.Abs(instancehash).ToString();
                    while(EventSpaceId.Length < 8){
                        EventSpaceId = $"{EventSpaceId}0";
                    }
                }
                RequestSerialization();
            }
            messageFrameDict.SetValue("eventspace", EventSpace);
            //Get TimelineClock
            timelineClock = gameObject.GetComponent<LapisCastTimelineClock>();
        }

        private void Update() {

            if(download_timer >= LoadingInterval){
                //Trigger StringLoading
                StringDownload();
                download_timer = 0;
            }
            else{
                download_timer += Time.deltaTime;
            }

            PlayTimeline();

            if(upload_timer > 0.05f){
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

        public override void OnDeserialization()
        {
            //Setting EventSpace
            EventSpace = $"VRChat@{EventSpaceId}";
            messageFrameDict.SetValue("eventspace", EventSpace);
        }

        //Get Instance Data
        private void StringDownload(){
            if(!EnableLapisCast) return;
            if(LocalTestMode){
                VRCStringDownloader.LoadUrl(localTestURL, (IUdonEventReceiver)this);
            }else{
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
                        timelineClock.AdjustTimelineClock(servtime.Double, 0);
                    }
                }
                else{
                    Debug.LogError("Prop Data not find.");
                    return;
                }
                if(result.DataDictionary.TryGetValue("tlmain", TokenType.DataDictionary, out DataToken tlValue)){
                    
                }
                else{
                    Debug.LogError("Timeline Data not find.");
                    return;
                }

                //Download Data Timestamps
                //string timestamp list
                DataList _timeline_stamps = tlValue.DataDictionary.GetKeys();
                _timeline_stamps.Sort();
                int ignore_error_colum = 0;
                //Server Timestamp Loop
                for(int i = 0;i < _timeline_stamps.Count; i++){
                    //Compare Timestamp
                    if(double.TryParse(_timeline_stamps[i].ToString(), out double server_timestamp)){
                        if(server_timestamp > _lastAppliedTimestamp){
                            //update lastTimestamp
                            _lastAppliedTimestamp = server_timestamp;
                            
                            //タイムラインからイベントリストを取り出し
                            if(tlValue.DataDictionary.TryGetValue(_timeline_stamps[i], out DataToken messageFrameList)){
                                downloadDataDict.Add(server_timestamp, messageFrameList.DataList);
                            }
                            else{
                                Debug.LogError("invalid data format");
                                Debug.Log($"{error_prefix}StringLoadSuccess. But Dataformat Invaild");
                            }
                           
                        }
                        else{
                            ignore_error_colum++;
                        }
                    }
                }
                //Debug.Log($"ingoreColum= {ignore_error_colum}");
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

        //Play Timeline
        private void PlayTimeline(){
            double baseTimestamp = timelineClock.GetTimestamp() - TimeLineOffset;
            DataList _timestamplist = downloadDataDict.GetKeys();
            DataList _removekeylist = new DataList();
            //Debug.Log($"TimeLine DataCount= {downloadDataDict.Count}");
            for(int i = 0;i < downloadDataDict.Count; i++){
                if(_timestamplist.TryGetValue(i, out DataToken timestamp)){
                    if(downloadDataDict.TryGetValue(timestamp, out DataToken messageFrameList)){
                        if(timestamp.TokenType == TokenType.Double && messageFrameList.TokenType == TokenType.DataList){
                            if(baseTimestamp - MaxEventDelay <= (double)timestamp){
                                if((double)timestamp <= baseTimestamp){
                                    //Debug.Log($"TimeLine Play at {timestamp} currentTime={timelineClock.GetTimestamp().ToString()}");
                                    //Debug.Log(value.TokenType); //Dictionary
                                    for(int ii = 0; ii < messageFrameList.DataList.Count; ii++){
                                        if(messageFrameList.DataList.TryGetValue(ii, TokenType.DataDictionary, out DataToken flameEvents)){
                                            CallFlameEvents(flameEvents.DataDictionary);
                                        }
                                        else{
                                            Debug.LogError("Before Event Call. FlameEvents not found.");
                                        }
                                    }
                                    _removekeylist.Add(timestamp);
                                }
                            }
                            else{
                                _removekeylist.Add(timestamp);
                            }
                        }
                    }
                    else{
                        _removekeylist.Add(timestamp);
                    }
                }
            }
            //Clean up downloadDataDict
            for(int i = 0; i < _removekeylist.Count; i++){
                downloadDataDict.Remove(_removekeylist[i]);
            }
        }
        //Call per TimeStamp
        private void CallFlameEvents(DataDictionary messageFrame){
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
            
            CallBehaviours(spacename, eventspace, eventkey, value);
        }
        //Call per Namespace
        private void CallBehaviours(string spacename,string eventspace, string keyname, DataToken value){
            //Debug.Log("CallBehaviours");
            bool sameinstance = false;
            if(EventSpace == eventspace){   
                sameinstance = true;          
            }
            for(int i = 0; i < lapisCastBehaviours.Length; i++){
                lapisCastBehaviours[i]._triggerLapisEvent(spacename, keyname, value, sameinstance);
            }
        }

        //Upload Instance Data
        public void AddEvent(string spacename, string keyname, DataToken value){
            if(!EnableLapisCast) return;
            //set MessageFrame
            messageFrameDict.SetValue("namespace", spacename);
            messageFrameDict.SetValue("eventkey", keyname);
            messageFrameDict.SetValue("value", value);

            string timestamp = timelineClock.GetUnixTimestamp().ToString();
            
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
    }
}

