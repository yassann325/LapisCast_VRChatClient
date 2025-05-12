
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;

namespace LapisCast{
    public class LapisCastTimelineClock : UdonSharpBehaviour
    {
        private double targetAdjustUnixOffset = 0;
        private double adjustmentUnixOffset = 0;
        private DataList unixOffsetList = new DataList();

        void Start()
        {
            adjustmentUnixOffset = GetLocalHostUnixTime();
        }

        void Update()
        {
            adjustmentUnixOffset = MoveTowardsDouble(adjustmentUnixOffset, targetAdjustUnixOffset, Time.deltaTime*0.5f);
        }

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
            return adjustmentUnixOffset;
        }

        // Adjust SceneStartTime use ServerBased Timestamp
        public void AdjustTimelineClock(double serverTime, double offsetTime){
            unixOffsetList.Add(new DataToken(serverTime + offsetTime - Time.time));
            if(unixOffsetList.Count > 10){
                unixOffsetList.RemoveAt(0);
            }

            double aveTime = 0;
            for(int i = 0; i < unixOffsetList.Count; i++){
                aveTime += unixOffsetList[i].Double;
            }
            aveTime /= unixOffsetList.Count;

            targetAdjustUnixOffset = aveTime;
            if(Math.Abs(targetAdjustUnixOffset - adjustmentUnixOffset) > 3){
                adjustmentUnixOffset = targetAdjustUnixOffset;
            }
        }

        public double GetUnixTimestamp(){
            return GetUnixSceneStartTime() + Time.time;
        }

        private double MoveTowardsDouble(double current, double target, double maxDelta)
        {
            double difference = target - current;
            if (Math.Abs(difference) <= maxDelta)
                return target;
            return current + Math.Sign(difference) * maxDelta;
        }

        public double GetTimestamp(){
            return GetUnixTimestamp();
        }
    }
}
