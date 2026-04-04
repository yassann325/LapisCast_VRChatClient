
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

        // =================================================== //
        // LapisCast Clock Vals
        [Tooltip("LapisCast単独使用の際のオフセットです。")]
        public float StandaloneTimelineOffset = -7f;
        [Tooltip("配信の時間情報を使用する際のオフセットです。")]
        public float StreamTimelineOffset = -0.5f;
        [SerializeField, UdonSynced]
        private bool UseStreamTimestamp = false;

        // Adjust Target Timestamp
        private double targetAdjustSceneStartTime = 0;
        private double targetSceneStreamStartTime = 0;
        // Current Adjusting Timecursor
        private double adjustedSceneStartTime = 0;
        private double sceneStreamStartTime = 0;

        // Adjust Step Average List
        private DataList unixOffsetList = new DataList();
        private DataList streamTimeOffsetList = new DataList();

        // World Init Time in Unity Client
        private double gamePlayTimeOffset = 0;


        void Start()
        {
            // Init Clock
            adjustedSceneStartTime = targetAdjustSceneStartTime = GetLocalHostUnixTime();
            gamePlayTimeOffset = Time.time;

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
            adjustedSceneStartTime = MoveTowardsDouble(adjustedSceneStartTime, targetAdjustSceneStartTime, Time.deltaTime*0.5f);
            sceneStreamStartTime = MoveTowardsDouble(sceneStreamStartTime, targetSceneStreamStartTime, Time.deltaTime*0.5f);

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
            double baseTimestamp = GetTimestamp();
            DataList _timestamplist = downloadDataDict.GetKeys();
            DataList _removekeylist = new DataList();
            //Debug.Log($"TimeLine DataCount= {downloadDataDict.Count}");
            for(int i = 0;i < downloadDataDict.Count; i++){
                if(_timestamplist.TryGetValue(i, TokenType.Double, out DataToken timestamp)){
                    if(downloadDataDict.TryGetValue(timestamp, TokenType.DataList, out DataToken messageFrameList)){
                        if(baseTimestamp - MaxEventDelay <= timestamp.Double){
                            if(timestamp.Double <= baseTimestamp){
                                //Debug.Log($"TimeLine Play at {timestamp} currentTime={GetTimestamp().ToString()}");
                                //Debug.Log(value.TokenType); //Dictionary
                                for(int ii = 0; ii < messageFrameList.DataList.Count; ii++){
                                    if(messageFrameList.DataList.TryGetValue(ii, TokenType.DataDictionary, out DataToken flameEvents)){
                                        CallFlameEvents(timestamp.Double, flameEvents.DataDictionary);
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
            //Debug.Log("CallBehaviours");
            bool sameinstance = false;
            if(EventSpace == eventspace){   
                sameinstance = true;          
            }
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

        
        //======================================================//
        // Clock Functions

        //getHostTimestamp
        public double GetLocalHostUnixTime(){
            DateTime now = DateTime.UtcNow;
            // Unixエポック（1970年1月1日）を定義
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            // 現在時刻とUnixエポックの差を求める
            TimeSpan elapsedTime = now - unixEpoch;
            // 秒単位の経過時間をdouble型で取得（小数点以下3桁まで）
            return Math.Round(elapsedTime.TotalSeconds, 3);
        }

        //======================================================//
        // LapisCast Clock
        // return UnixTime when HostBased SceneStartTime or ServerBased SceneStartTime
        public double GetUnixSceneStartTime(){
            return adjustedSceneStartTime;
        }

        public double GetVRChatInstaneActiveTime()
        {
            return Time.time - gamePlayTimeOffset;
        }

        // Adjust SceneStartTime use ServerBased Timestamp
        public void AdjustTimelineClock(double serverTime, double offsetTime){
            unixOffsetList.Add(new DataToken(serverTime + offsetTime - GetVRChatInstaneActiveTime()));
            if(unixOffsetList.Count > 10){
                unixOffsetList.RemoveAt(0);
            }

            double aveTime = 0;
            for(int i = 0; i < unixOffsetList.Count; i++){
                aveTime += unixOffsetList[i].Double;
            }
            aveTime /= unixOffsetList.Count;

            targetAdjustSceneStartTime = aveTime;
            // If there is a large deviation, it is forced to be applied.
            if(Math.Abs(targetAdjustSceneStartTime - adjustedSceneStartTime) > 3){
                adjustedSceneStartTime = targetAdjustSceneStartTime;
            }
        }

        public double GetUnixTimestamp(){
            return GetUnixSceneStartTime() + GetVRChatInstaneActiveTime();
        }

        private double MoveTowardsDouble(double current, double target, double maxDelta)
        {
            double difference = target - current;
            if (Math.Abs(difference) <= maxDelta)
                return target;
            return current + Math.Sign(difference) * maxDelta;
        }

        //======================================================//
        // Stream Clock
        public void AdjustStreamTimelineClock(double streamTime, bool forceApply){
            if (forceApply)
            {
                streamTimeOffsetList.Clear();
            }

            // 現在時刻とStreamTimeの違いを計測
            streamTimeOffsetList.Add(new DataToken(streamTime - GetLocalHostUnixTime()));
            if(streamTimeOffsetList.Count > 10){
                streamTimeOffsetList.RemoveAt(0);
            }

            double aveTime = 0;
            for(int i = 0; i < streamTimeOffsetList.Count; i++){
                aveTime += streamTimeOffsetList[i].Double;
            }
            aveTime /= streamTimeOffsetList.Count;

            targetSceneStreamStartTime = aveTime;
            // If there is a large deviation, it is forced to be applied.
            if(Math.Abs(targetSceneStreamStartTime - sceneStreamStartTime) > 3){
                sceneStreamStartTime = targetSceneStreamStartTime;
            }
        }

        public double GetStreamTimestamp(){
            return sceneStreamStartTime + GetLocalHostUnixTime();
        }


        //======================================================//
        // return preferential Timestamp
        public double GetTimestamp(){
            if(UseStreamTimestamp){
                return GetStreamTimestamp() + StreamTimelineOffset;
            }
            else{
                return GetUnixTimestamp() + StandaloneTimelineOffset;
            }
        }

        // LapisCast Clock Param Setting
        public void SetUseStreamTimestamp(bool state)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            UseStreamTimestamp = state;
            RequestSerialization();
        }
        public bool GetUseStreamTimestamp() { return UseStreamTimestamp; }
    }
}

