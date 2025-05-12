
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;

namespace LapisCast{
    public class LapisCastTimelineClock : UdonSharpBehaviour
    {
        private double initUnixOffset = 0;
        private DataList unixOffsetList = new DataList();

        void Start()
        {
            initUnixOffset = GetLocalHostUnixTime();
        }

        void Update()
        {
            
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

        public double GetUnixOffset(){
            double offsetTime = initUnixOffset;
            if(unixOffsetList.Count > 0){
                offsetTime = 0;
                for(int i = 0; i < unixOffsetList.Count; i++){
                    offsetTime += unixOffsetList[i].Double;
                }
                offsetTime /= unixOffsetList.Count;
            }
            return offsetTime;
        }

        public void AdjustTimelineClock(double serverTime, double offsetTime){
            unixOffsetList.Add(new DataToken(serverTime + offsetTime - Time.time));
            if(unixOffsetList.Count > 10){
                unixOffsetList.RemoveAt(0);
            }
        }

        public double GetTimestamp(){
            return GetLocalHostUnixTime() + Time.time;
        }
    }
}
