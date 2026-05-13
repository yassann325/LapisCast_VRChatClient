
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Rendering;
using VRC.Udon.Common.Interfaces;
using System;
using VRC.SDK3.Video.Components.Base;

// 動画中のタイムスタンプを取得してLapisCastに適用する
namespace LapisCast{
    public class LapisCastStreamClockConnector : LapisCastBehaviour
    {
        public float ClockUpdateInterval = 3f; // ストリームからタイムスタンプを読み出すインターバル時間 (秒)
        public short ReloadTriggerFailCount = 3; // タイムスタンプの増加幅が正常ではない場合にリロードを実行するまでのエラー回数
        public bool ForceApplyTime = false; // 補完や増加速度の検証をせずに常にタイムスタンプを適用する
        public bool DontTouchVideoPlayer = false; // AVProPlayerを直接制御しなくなる
        public MeshRenderer screenRenderer; // AVProPlayer VideoScreen の対象のメッシュ
        public Material FlipAjustMaterial;
        public RenderTexture FlipAjustRTexture;

        [Space(10)]
        public bool CallVideoPlayerControlEvent = false; // Play(), Stop() の際に外部のイベントを呼ぶようになる
        [Space(10)]
        public UdonSharpBehaviour[] OnPlayCallBehaviours;
        public string[] OnPlayCallEventNames;
        [Space(10)]
        public UdonSharpBehaviour[] OnStopCallBehaviours;
        public string[] OnStopCallEventNames;

        private float loopTimer = 0;
        private RenderTexture sourceTexture = null;
        private double Stream_Unity_DiffTime = 0;
        private short StreamTime_Mismatch_Count = 0;



        [Space(20)]
        public bool VideoPlayerEnabled = true;

        [SerializeField]
        private VRCUrl StreamURL = new VRCUrl("");
        [SerializeField]
        private BaseVRCVideoPlayer _avProVideoPlayer;
        [SerializeField]
        private float FirstLoadWait = 5f; // Joinしてからロードするまでの待機時間
        [SerializeField]
        private float RetryInterval = 20f; // ロードが失敗した際の待機時間

        private float _reloadTimer = 0f;

        // =========================== //
        void OnEnable()
        {
            ResetVideoReloadTimer();
        }

        void Start()
        {
            LapisCastBehaviourInit();
        }

        void Update()
        {
            // VideoPlayer
            if (VideoPlayerEnabled)
            {
                _reloadTimer += Time.deltaTime;
                if (!_avProVideoPlayer.IsPlaying)
                {              
                    if (_reloadTimer >= RetryInterval && StreamTime_Mismatch_Count == 0)
                    {
                        // Play Stream
                        Play();
                        _reloadTimer = 0;
                    }
                }
            }
            else
            {
                ResetVideoReloadTimer();
            }

            // Texture Decoader
            if (Utilities.IsValid(_avProVideoPlayer))
            {
                if (!_avProVideoPlayer.IsPlaying)
                {
                    Stream_Unity_DiffTime = 0;
                    StreamTime_Mismatch_Count = 0;
                }

                if (Utilities.IsValid(screenRenderer))
                {             
                    var mainTexture = screenRenderer.material.GetTexture("_MainTex");
                    if (Utilities.IsValid(mainTexture))
                    {
                        VRCGraphics.Blit(mainTexture, FlipAjustRTexture, FlipAjustMaterial);

                        if(loopTimer > ClockUpdateInterval){
                            loopTimer = 0;
                            if (_avProVideoPlayer.IsPlaying)
                            {
                                ReadTexture(FlipAjustRTexture);
                            }
                        }
                        loopTimer += Time.deltaTime;
                    }
                }

                // Get New Timestamp Reload
                if (ReloadTriggerFailCount >= 0 && StreamTime_Mismatch_Count >= ReloadTriggerFailCount)
                {
                    Play();
                }
            }
        }

        // =========================== //

        private void ResetVideoReloadTimer()
        {
            _reloadTimer = RetryInterval + Mathf.Max(0, FirstLoadWait) * -1;
        }

        private void Play()
        {
            Stop();

            if(!DontTouchVideoPlayer)
                _avProVideoPlayer.PlayURL(StreamURL);
            
            if (CallVideoPlayerControlEvent)
            {
                for (int i = 0; i < OnPlayCallBehaviours.Length; i++)
                {
                    OnPlayCallBehaviours[i].SendCustomEvent(OnPlayCallEventNames[i]);
                }
            }
        }

        private void Stop()
        {
            if (!DontTouchVideoPlayer && _avProVideoPlayer.IsPlaying)
            {
                _avProVideoPlayer.Stop();
            }

            if (CallVideoPlayerControlEvent)
            {
                for (int i = 0; i < OnStopCallBehaviours.Length; i++)
                {
                    OnStopCallBehaviours[i].SendCustomEvent(OnStopCallEventNames[i]);
                }
            }
        }

        // =========================== //

        public void ReadTexture(RenderTexture rt)
        {
            sourceTexture = rt;
            // GPU ReadBack Sampling
            VRCAsyncGPUReadback.Request(rt, 0, (IUdonEventReceiver)this);

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
                var px = new Color32[sourceTexture.width * sourceTexture.height];
                bool result = request.TryGetData(px);
                if(!result){
                    return;
                }
                // Debug.Log("GPU readback success: " + result);
                // Debug.Log("GPU readback size: " + SourceTexture.width + " x " + SourceTexture.height);

                SamplingTimestampData(ReadTimestampPixel(px));
            }
        }


        public RenderTexture GetStreamTimestampSourceTexture(){
            return sourceTexture;
        }

        // Color32[]からlong部分を読み取り
        public Color32[] ReadTimestampPixel(Color32[] px)
        {
            Color32[] dpx = new Color32[65];
            for(int i = 0; i < dpx.Length; i++){
                // テスクチャ下部の場所をサンプリングする
                // x: 0 ~ width  y: 4
                int pxIndex = (FlipAjustRTexture.width * i / dpx.Length + FlipAjustRTexture.width / (dpx.Length * 2)) + 4 * FlipAjustRTexture.width;
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
        public void SamplingTimestampData(Color32[] longDataPixels)
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
                if (Stream_Unity_DiffTime == 0)
                {
                    GetLapisCastCore().AdjustStreamTimelineClock(streamTime, true);
                    Stream_Unity_DiffTime = stream_Unity_DiffTime;
                }
                // ゲーム内経過時間と読み出したタイムスタンプが同期していれば適用
                else if (Math.Abs(Stream_Unity_DiffTime - stream_Unity_DiffTime) < 0.1f * ClockUpdateInterval)
                {
                    GetLapisCastCore().AdjustStreamTimelineClock(streamTime, false);
                    Stream_Unity_DiffTime = stream_Unity_DiffTime;
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
