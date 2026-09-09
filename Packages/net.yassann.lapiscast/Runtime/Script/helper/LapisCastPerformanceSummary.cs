
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using VRC.SDK3.UdonNetworkCalling;  
using VRC.Udon.Common.Interfaces;

namespace LapisCast{
    public partial class LapisCastCore
    {
        // ======================================== //
        // 各プレーヤーの安定度を計測する

        [UdonSynced, FieldChangeCallback(nameof(PerformanceReaderPlayerId))]
        private int performanceReaderPlayerId = -1;
        private int PerformanceReaderPlayerId
        {
            get => performanceReaderPlayerId;
            set { performanceReaderPlayerId = value; OnChangePerformanceReader(GetPerformanceReaderPlayer()); }
        }

        // LapisCast 受信状態
        private float lapiscastAccessStatus = 1; // 2~-2

        // 全体平均用
        private DataDictionary ClinetsPerformanceData = new DataDictionary();
        float[] fpsList = new float[30];
        private float totallingTime = 0;
        private float readerPickupTime = 0;


        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            ClinetsPerformanceData.Remove(player.playerId);
        }

        private int GetPerformanceReaderPlayerId()
        {
            DataList playerIds = ClinetsPerformanceData.GetKeys();
            int readerPlayerId = Networking.LocalPlayer.playerId;
            float readerFPS = 0;
            for (int i = 0; i < playerIds.Count; i++)
            {
                if (readerFPS < ClinetsPerformanceData[playerIds[i]].Float)
                {
                    readerPlayerId = playerIds[i].Int;
                    readerFPS = ClinetsPerformanceData[playerIds[i]].Float;
                }
            }
            return readerPlayerId;
        }

        private void PerformanceSummaryUpdate()
        {
            // タイムスケール（Time.timeScale）の影響を受けない生の時間を使用
            fpsList[(int)(Time.unscaledTime * 2) % fpsList.Length] = 1f / Time.unscaledDeltaTime;

            // 定期的に全員に自分のFPSを送る
            if (totallingTime > 20)
            {
                float avgFPS = 0;
                for (int i = 0;i < fpsList.Length; i++){ avgFPS += fpsList[i]; }
                avgFPS /= fpsList.Length;

                // WINクライアントではない時は-15してPC側を優先するようにする
                #if !(UNITY_EDITOR || UNITY_STANDALONE_WIN)
                    avgFPS -= 15;
                #endif

                NetworkCalling.SendCustomNetworkEvent((IUdonEventReceiver)this, NetworkEventTarget.All, nameof(LapisSystem_PlayerPerformanceAd), Networking.LocalPlayer.playerId, avgFPS + (lapiscastAccessStatus * 10));
                totallingTime = 0;
            }
            totallingTime += Time.deltaTime;

            // Masterは定期的に理想的な同期クライアントを選びなおす
            if (readerPickupTime > 33)
            {
                // Masterは定期的に理想的な同期クライアントを選びなおす
                if (Networking.IsOwner(Networking.LocalPlayer, gameObject))
                {
                    // 現在のリーダーのPlayerId
                    if(ClinetsPerformanceData.TryGetValue(Networking.GetOwner(gameObject).playerId, out DataToken currentReaderPerformanceValue))
                    {
                        // リーダー候補のPlayerId
                        int newReaderPlayerid = GetPerformanceReaderPlayerId();
                        // 現在のリーダーがある程度安定していればそのままにする
                        if (ClinetsPerformanceData[newReaderPlayerid].Float > currentReaderPerformanceValue.Float + 10)
                        {
                            PerformanceReaderPlayerId = newReaderPlayerid;
                            RequestSerialization();
                        }
                    }
                }
                readerPickupTime = 0;
            }
            readerPickupTime += Time.deltaTime;
        }

        // 自身のパフォーマンスをお互いに報告しあう
        [NetworkCallable (maxEventsPerSecond: 25)]  
        public void LapisSystem_PlayerPerformanceAd(int playerId, float performanceValue)  { ClinetsPerformanceData.SetValue(playerId, performanceValue); }


        public VRCPlayerApi GetPerformanceReaderPlayer() { 
            VRCPlayerApi p = VRCPlayerApi.GetPlayerById(PerformanceReaderPlayerId);
            if (Utilities.IsValid(p)) {
                return p;
            }
            else {
                return  Networking.GetOwner(gameObject);
            }
        }

        private void OnChangePerformanceReader(VRCPlayerApi newReaderPlayer)
        {
            for(int i = 0; i < lapisCastBehaviours.Length; i++){
                if (Utilities.IsValid(lapisCastBehaviours[i]))
                {
                    lapisCastBehaviours[i].OnChangePerformanceReader(newReaderPlayer);
                }
            }
        }

    }
}