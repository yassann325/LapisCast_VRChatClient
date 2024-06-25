
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.Udon.Common.Interfaces;

namespace LapisCast{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class LapisCastCore : UdonSharpBehaviour
    {
        //Client Settings
        public float StartWaiting = 0f;
        public float LoadingInterval = 5f;
        public float TimeLineOffset = 0.5f;
        public bool LocalTestMode = false;
        public VRCUrl InstanceURL = new VRCUrl("https://lapis.yassann.net/lapiscast/public/get/{instanceid-here}");

        //Client Values
        private VRCUrl localTestURL = new VRCUrl("http://localhost:33000/test/lapiscast");
        [UdonSynced]
        private string InstanceHash = "defaultinstance";
        private string log_prefix = "__LapisCast__";
        private string error_prefix = "__LapisCastError__";
        private DataDictionary uploadDataDict = new DataDictionary();
        private DataDictionary downloadDataDict = new DataDictionary();
        private LapisCastBehaviour[] lapisCastBehaviours = new LapisCastBehaviour[0];
        private double _lastAppliedTimestamp = 0;

        //Timer
        private float download_timer = 0;
        private float upload_timer = 0;


        void Start()
        {        
            download_timer = LoadingInterval;
            download_timer -= StartWaiting;
            if(Networking.IsOwner(Networking.LocalPlayer, gameObject)){
                if(InstanceHash == "defaultinstance"){
                    int instancehash = $"{Networking.LocalPlayer.displayName}{DateTime.Now.Millisecond}".GetHashCode();
                    InstanceHash = Mathf.Abs(instancehash).ToString();
                    while(InstanceHash.Length < 10){
                        InstanceHash = $"{InstanceHash}0";
                    }
                }
            }
            Debug.Log($"InstanceHash= {InstanceHash}");
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
                    uploadDataDict.Clear();
                }
                upload_timer = 0;
            }
            else{
                upload_timer += Time.deltaTime;
            }
        }

        //Get Instance Data
        private void StringDownload(){
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
                DataList _server_keylist = result.DataDictionary.GetKeys();
                _server_keylist.Sort();
                int ignore_colum = 0;
                //Server Timestamp Loop
                for(int i = 0;i < _server_keylist.Count; i++){
                    //Debug.Log(_keylist[i]);
                    if(double.TryParse(_server_keylist[i].ToString(), out double server_timestamp)){
                        if(server_timestamp > _lastAppliedTimestamp){
                            //update lastTimestamp
                            _lastAppliedTimestamp = server_timestamp;
                            
                            if(result.DataDictionary.TryGetValue(_server_keylist[i], out DataToken timelinedata)){
                                 //Event Timestamp Loop
                                DataList _keylist = timelinedata.DataDictionary.GetKeys();
                                for(int ii = 0; ii < _keylist.Count; ii++){
                                    if(double.TryParse(_keylist[ii].ToString(), out double timestamp)){
                                        //if not exist dict. add
                                        if(downloadDataDict.TryGetValue(timestamp, out DataToken exist)){
                                            Debug.LogWarning($"LapisCast Sense ExistKey");
                                        }
                                        else{
                                            DataToken value = timelinedata.DataDictionary[_keylist[ii]];
                                            //Debug.Log($"Add Key={_keylist[ii]}");
                                            downloadDataDict.Add(timestamp, value);
                                        }
                                    } 
                                }
                            }
                            else{
                                Debug.LogError("invalid data format");
                                Debug.Log($"{error_prefix}StringLoadSuccess. But Dataformat Invaild");
                            }
                           
                        }
                        else{
                            ignore_colum++;
                        }
                    }
                }
                //Debug.Log($"ingoreColum= {ignore_colum}");
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
            double baseTimestamp = GetUnixTime() - Mathf.Max(TimeLineOffset, 0);
            DataList _keylist = downloadDataDict.GetKeys();
            DataList _removekeylist = new DataList();
            //Debug.Log($"TimeLine DataCount= {downloadDataDict.Count}");
            for(int i = 0;i < downloadDataDict.Count; i++){
                if(_keylist.TryGetValue(i, out DataToken timestamp)){
                    //Debug.Log(timestamp.TokenType); //Double
                    if(downloadDataDict.TryGetValue(timestamp, out DataToken value)){
                        if(timestamp.TokenType == TokenType.Double){
                            if((double)timestamp <= baseTimestamp && (double)timestamp >= baseTimestamp - 1){
                                //Debug.Log($"TimeLine Play at {timestamp}");
                                //Debug.Log(value.TokenType); //Dictionary
                                CallFlameEvents(value.DataDictionary);
                                _removekeylist.Add(timestamp);
                            }
                        }
                    }
                }
            }
            //Clean up downloadDataDict
            for(int i = 0; i < _removekeylist.Count; i++){
                downloadDataDict.Remove(_removekeylist[i]);
            }
        }
        //Call per TimeStamp
        private void CallFlameEvents(DataDictionary flameData){
            //Debug.Log("CallFlameEvents");
            DataList spacenamelist = flameData.GetKeys();
            for(int i = 0; i < spacenamelist.Count; i++){
                DataDictionary spaceData = flameData[spacenamelist[i]].DataDictionary;
                DataList keynamelist = spaceData.GetKeys();
                for(int j = 0; j < keynamelist.Count; j++){
                    CallBehaviours(spacenamelist[i].ToString(), keynamelist[j].ToString(), spaceData[keynamelist[j]]);
                }
            }
        }
        //Call per Namespace
        private void CallBehaviours(string spacename, string keyname, DataToken value){
            bool sameinstance = false;
            string[] spacename_split = spacename.Split('@');
            if(spacename_split.Length > 1){   
                spacename = spacename_split[0];
                if(spacename_split[1] == InstanceHash){
                    sameinstance = true;      
                }            
            }
            for(int i = 0; i < lapisCastBehaviours.Length; i++){
                lapisCastBehaviours[i]._triggerLapisEvent(spacename, keyname, value, sameinstance);
            }
        }

        //Upload Instance Data
        public void AddEvent(string spacename, string keyname, DataToken value){
            spacename = $"{spacename}@{InstanceHash}";
            if(uploadDataDict.TryGetValue(spacename, out DataToken name_space)){
                if(name_space.TokenType != TokenType.DataDictionary){
                    //ネームスペース以外の構造は削除
                    uploadDataDict.Remove(name_space);
                }
                else{
                    name_space.DataDictionary.SetValue(keyname, value);
                }
            }
            else{
                uploadDataDict.SetValue(spacename, new DataDictionary());
                ((DataDictionary)uploadDataDict[spacename]).SetValue(keyname, value);
            }
        }
        private void OutputLog(){
            if(uploadDataDict.Count == 0){
                return;
            }
            if (VRCJson.TrySerializeToJson(uploadDataDict, JsonExportType.Minify, out DataToken result))
            {
                //string _json = result.String;
                Debug.Log($"{log_prefix}{result.String}");
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

        //getTimestamp
        private double GetUnixTime(){
            DateTime now = DateTime.UtcNow;
            // Unixエポック（1970年1月1日）を定義
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            // 現在時刻とUnixエポックの差を求める
            TimeSpan elapsedTime = now - unixEpoch;
            // 秒単位の経過時間をdouble型で取得（小数点以下の精度も含む）
            return elapsedTime.TotalSeconds;
        }
    }
}

