
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Rendering;
using VRC.Udon.Common.Interfaces;
using System;
using VRC.SDK3.Video.Components.AVPro;

using HoshinoLabs.IwaSync3.Udon;

// 動画中のタイムスタンプを取得してLapisCastに適用する
// ストリームからタイムスタンプが検出され、かつ ずれがある際には自動リロード

namespace LapisCast{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LapisCastStreamClockConnector : LapisCastBehaviour
    {
        [SerializeField]
        private bool ReadTimestampEnabled = true;
        [SerializeField]
        VideoCore core; // IwaSync プレイヤー

        [Space(20)]
        [SerializeField]
        private float ClockUpdateInterval = 3f; // ストリームからタイムスタンプを読み出すインターバル時間 (秒)
        [SerializeField]
        private float ReadTimestampWait = 2f; // 動画再生が始まってからタイムスタンプを読み出すまでの遅延時間 (秒)
        [SerializeField]
        private bool ForceApplyTime = false; // 補完や増加速度の検証をせずに常にタイムスタンプを適用する
        [SerializeField]
        private uint TextureSamplingHeight = 4; // プレーヤーからタイムスタンプを読み出す際のサンプリングする高さ(px)
        [SerializeField]
        private Material FlipAjustMaterial;// プレーヤーからの画像の上下反転補正用のマテリアル
        [SerializeField]
        private RenderTexture FlipAjustRTexture; // プレーヤーからの画像の上下反転補正用のコピーテクスチャ
        private double Stream_Unity_DiffTime = 0;
        private ushort StreamTime_Mismatch_Count = 0; // 読み出したデータのタイムスタンプが想定値からずれすぎていた回数
        private float LastClockCheckTime = 0f;
        private float VideoPlayer_PlayingTime = 0; // isPlaying がtrueになってからの経過時間

        [Space(20)]
        [SerializeField]
        private bool AutoVideoPlayerReload = true; // タイムスタンプのずれを検出した際に自動でReloadリクエストをトリガーするか
        [SerializeField]
        private ushort ReloadTriggerFailCount = 3; // タイムスタンプのずれによって再読み込みする閾値

        // =========================== //

        void Start()
        {
            LapisCastBehaviourInit();
            LastClockCheckTime = Time.unscaledTime;
        }

        void Update()
        {
            // Texture Decoader
            if (ReadTimestampEnabled && Utilities.IsValid(core))
            {
                if (core.isPlaying)
                {
                    VideoPlayer_PlayingTime += Time.unscaledDeltaTime;

                    // 動画プレイヤーがロード直後のタイミングで読み取らない
                    if (Stream_Unity_DiffTime < 0.0001 && VideoPlayer_PlayingTime < 2)
                    {
                        return;
                    }

                    var mainTexture = core.texture;
                    if (Utilities.IsValid(mainTexture))
                    {
                        if(Time.unscaledTime - LastClockCheckTime > ClockUpdateInterval){
                            LastClockCheckTime = Time.unscaledTime;
                            RequestReadTimeDataTexture(mainTexture);
                        }
                    } 
                }
                else
                {
                    ResetStreamTimeInfo();
                }
            }

            // VideoPlayer Reload Request
            if (AutoVideoPlayerReload && Utilities.IsValid(core))
            {
                // タイムスタンプにずれが蓄積したときのリロード
                if (core.isPlaying) {
                    if (StreamTime_Mismatch_Count >= Mathf.Max(ReloadTriggerFailCount, 1))
                    {
                        VideoPlayerReload();
                    }
                }
            }
        }

        private void VideoPlayerReload()
        {
            if(!AutoVideoPlayerReload) return;
            core.Reload();
            ResetStreamTimeInfo();
        }

        private void ResetStreamTimeInfo()
        {
            Stream_Unity_DiffTime = 0;
            StreamTime_Mismatch_Count = 0;
            VideoPlayer_PlayingTime = 0;
        }

        // =========================== //

        public void RequestReadTimeDataTexture(Texture texture)
        {
            // Ajust Platform Texture Upside
            VRCGraphics.Blit(texture, FlipAjustRTexture, FlipAjustMaterial);
            // GPU ReadBack Sampling
            VRCAsyncGPUReadback.Request(FlipAjustRTexture, 0, (IUdonEventReceiver)this);
        }

        public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                // Debug.LogError("GPU readback error!");
                return;
            }
            else
            {
                var px = new Color32[FlipAjustRTexture.width * FlipAjustRTexture.height];
                bool result = request.TryGetData(px);
                if(!result){
                    return;
                }
                // Debug.Log("GPU readback success: " + result);
                // Debug.Log("GPU readback size: " + FlipAjustRTexture.width + " x " + FlipAjustRTexture.height);

                SamplingTimestampData(ReadTimestampPixel(px));
            }
        }


        // Color32[]からlong部分を読み取り
        private Color32[] ReadTimestampPixel(Color32[] px)
        {
            Color32[] dpx = new Color32[65];
            for(int i = 0; i < dpx.Length; i++){
                // テスクチャ下部の場所をサンプリングする
                // x: 0 ~ width  y: 4
                int pxIndex = (FlipAjustRTexture.width * i / dpx.Length + FlipAjustRTexture.width / (dpx.Length * 2)) + (int)TextureSamplingHeight * FlipAjustRTexture.width;
                dpx[i] = px[pxIndex];
            }

            // longとしてデコードできるタイムスタンプかをを検証
            bool checkok = false;
            for (int i = 0; i < dpx.Length; i++)
            {
                if (i == 64)
                {
                    if (dpx[i].r > 240 && dpx[i].g < 128 && dpx[i].b < 128)
                    {
                       checkok = true; 
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (dpx[i].r < 220 || dpx[i].g < 100)
                    {
                        break;
                    }
                }
            }

            if (checkok)
            {
                return dpx;
            }
            else{
                return new Color32[0];
            }
        }

        // Color32[64]からlongに変換して適用
        private void SamplingTimestampData(Color32[] longDataPixels)
        {
            if (longDataPixels.Length != 65)
            {
                return;
            }

            long value = 0;
            for(int i = 0; i < 64; i++){
                if(longDataPixels[i].b > 128){
                    value |= (1L << (63 - i));
                }
                // Debug.Log(i + " | " + pxIndex);
            }
            // Current Stream Timestamp
            double streamTime = (double)value / 1000;
            double stream_Unity_DiffTime = streamTime - Time.time;

            if (ForceApplyTime)
            {
                GetLapisCastCore().AdjustStreamTimelineClock(streamTime, true);
            }
            else
            {      
                // プレイヤーがActiveになった最初の1度は関係なく適用
                if (Stream_Unity_DiffTime < 0.0001)
                {
                    GetLapisCastCore().AdjustStreamTimelineClock(streamTime, true);
                    Stream_Unity_DiffTime = stream_Unity_DiffTime;
                }
                // ゲーム内経過時間と読み出したタイムスタンプが同期していれば適用
                else if (Math.Abs(Stream_Unity_DiffTime - stream_Unity_DiffTime) < 0.1f)
                {
                    GetLapisCastCore().AdjustStreamTimelineClock(streamTime, false);
                    Stream_Unity_DiffTime = stream_Unity_DiffTime;

                    // 正常値が読み出せた場合は StreamTime_Mismatch_Count をリセット
                    StreamTime_Mismatch_Count = 0;
                }
                // ラグなどでずれが蓄積した場合
                else
                {
                    StreamTime_Mismatch_Count++;
                }
            }
        }
    }
}
