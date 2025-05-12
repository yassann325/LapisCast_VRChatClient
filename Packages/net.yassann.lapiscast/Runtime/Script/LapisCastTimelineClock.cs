
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using VRC.SDK3.Rendering;
using VRC.Udon.Common.Interfaces;

namespace LapisCast{
    public class LapisCastTimelineClock : UdonSharpBehaviour
    {
        private double targetAdjustUnixOffset = 0;
        private double adjustmentUnixOffset = 0;
        private DataList unixOffsetList = new DataList();
        
        [Tooltip("配信の時間情報を使用する際のオフセットです。")]
        public float StreamTimelineOffset = 0;

        public bool UseStreamTimestamp = false;
        public int BitSize = 16;
        public bool InvertTexture_Y = false;
        private RenderTexture sourceTexture = null;
        private double streamUnixDiffTime = 0;


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

        //======================================================//
        // Stream Clock
        // Request Read Stream Data
        public void AdjustStreamTimelineClock(RenderTexture rt){
            sourceTexture = rt;
            if(rt){
                VRCAsyncGPUReadback.Request(rt, 0, (IUdonEventReceiver)this);
            }
            else{
                Debug.LogError("SourceTexture None.");
            }
        }

        public double GetStreamTimestamp(){
            return GetUnixTimestamp() - streamUnixDiffTime;
        }

        public RenderTexture GetStreamTimestampSourceTexture(){
            return sourceTexture;
        }

        private int GetColorsIndex(int x, int y, int width, int height, bool invert_y)
        {
            if(invert_y){
                int flippedY = (height - 1) - y;
                return flippedY * width + x;
            }
            else{
                return y * width + x;
            }
        }

        public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback error!");
                return;
            }
            else
            {
                var px = new Color32[sourceTexture.width * sourceTexture.height];
                bool result = request.TryGetData(px);
                if(!result){
                    return;
                }
                // Debug.Log("GPU readback success: " + result);
                // Debug.Log("GPU readback size: " + SourceTexture.width + " x " + SourceTexture.height);

                Color32[] dpx = new Color32[64];
                for(int i = 0; i < 64; i++){
                    int pxIndex = GetColorsIndex(BitSize * i + BitSize / 2, BitSize / 2, sourceTexture.width, sourceTexture.height, InvertTexture_Y);
                    dpx[i] = px[pxIndex];
                }

                double streamTime = DecordStreamData(dpx);
                if(Math.Abs(GetUnixTimestamp() - streamTime) > 60){ return; }
                streamUnixDiffTime = GetUnixTimestamp() - (streamTime + StreamTimelineOffset);
            }
        }

        private double DecordStreamData(Color32[] px){
            long value = 0;
            for(int i = 0; i < 64; i++){
                if(px[i].b > 128){
                    value |= (1L << (63 - i));
                }
                // Debug.Log(i + " | " + pxIndex);
            }
            return (double)value / 1000;
        }

        //======================================================//
        // return preferential Timestamp
        public double GetTimestamp(){
            if(UseStreamTimestamp){
                return GetStreamTimestamp();
            }
            else{
                return GetUnixTimestamp();
            }
        }
    }
}
