
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using UnityEngine.PlayerLoop;

namespace LapisCast{
    public partial class LapisCastCore
    {
        [Header("Clock Settings"), Space(3)]

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

        private void ClockInit()
        {
            // Init Clock
            adjustedSceneStartTime = targetAdjustSceneStartTime = GetLocalHostUnixTime();
            gamePlayTimeOffset = Time.time;
        }

        private void ClockUpdate()
        {
            // Update Clock
            adjustedSceneStartTime = MoveTowardsDouble(adjustedSceneStartTime, targetAdjustSceneStartTime, Time.deltaTime*0.1f);
            sceneStreamStartTime = MoveTowardsDouble(sceneStreamStartTime, targetSceneStreamStartTime, Time.deltaTime*0.1f);
        }

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


