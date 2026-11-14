using UnityEngine;
using UnityEditor;
using LapisCast;
using VRC.SDKBase;

namespace LapisCastEditor{
    [CustomEditor(typeof(LapisCastCore))]
    [CanEditMultipleObjects]
    public class LapisCastCoreEditor : Editor {
        // SerializedProperty の定義
        private SerializedProperty InstanceURL;
        private SerializedProperty AutoTimeAjustFirst;
        private SerializedProperty StartWaiting;
        private SerializedProperty LoadingInterval;
        private SerializedProperty MaxEventDelay;
        private SerializedProperty EnableLapisCast;
        private SerializedProperty EnableLapisCastEventExec;
        private SerializedProperty EnableLapisCastEventOutput;
        private SerializedProperty DisableLogOnNonWindows;
        private SerializedProperty GimmickDebugMode;
        private SerializedProperty StandaloneTimelineOffset;
        private SerializedProperty DefaultStandaloneTimelineOffset;
        private SerializedProperty StreamTimelineOffset;
        private SerializedProperty DefaultStreamTimelineOffset;
        private SerializedProperty UseStreamTimestamp;
        private SerializedProperty ForceFallbackStandaloneTime;

        private void OnEnable()
        {
            // 各プロパティの割り当て
            InstanceURL = serializedObject.FindProperty("InstanceURL");
            AutoTimeAjustFirst = serializedObject.FindProperty("AutoTimeAjustFirst");
            StartWaiting = serializedObject.FindProperty("StartWaiting");
            LoadingInterval = serializedObject.FindProperty("LoadingInterval");
            MaxEventDelay = serializedObject.FindProperty("MaxEventDelay");
            EnableLapisCast = serializedObject.FindProperty("EnableLapisCast");
            EnableLapisCastEventExec = serializedObject.FindProperty("EnableLapisCastEventExec");
            EnableLapisCastEventOutput = serializedObject.FindProperty("EnableLapisCastEventOutput");
            DisableLogOnNonWindows = serializedObject.FindProperty("DisableLogOnNonWindows");
            GimmickDebugMode = serializedObject.FindProperty("GimmickDebugMode");
            StandaloneTimelineOffset = serializedObject.FindProperty("StandaloneTimelineOffset");
            DefaultStandaloneTimelineOffset = serializedObject.FindProperty("_DefaultStandaloneTimelineOffset");
            StreamTimelineOffset = serializedObject.FindProperty("StreamTimelineOffset");
            DefaultStreamTimelineOffset = serializedObject.FindProperty("_DefaultStreamTimelineOffset");
            UseStreamTimestamp = serializedObject.FindProperty("useStreamTimestamp");
            ForceFallbackStandaloneTime = serializedObject.FindProperty("forceFallbackStandaloneTime");
        }

        public override void OnInspectorGUI()
        {
            // データの同期開始
            serializedObject.Update();

            EditorGUILayout.LabelField("LapisCastインスタンスのURL", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(InstanceURL, new GUIContent("InstanceURL", "受信するLapisCastインスタンスURL"));
            
            SerializedProperty urlProp = InstanceURL.FindPropertyRelative("url");
            if (urlProp != null && (string.IsNullOrEmpty(urlProp.stringValue) || urlProp.stringValue == "https://lapis.yassann.net/lapiscast/public/{instanceid-here}"))
            {
                EditorGUILayout.HelpBox("LapisCast インスタンスURLが設定されていません。", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(AutoTimeAjustFirst);
            EditorGUILayout.PropertyField(StartWaiting);
            EditorGUILayout.PropertyField(LoadingInterval);
            EditorGUILayout.PropertyField(MaxEventDelay);
            EditorGUILayout.PropertyField(EnableLapisCast);
            EditorGUILayout.PropertyField(EnableLapisCastEventExec);
            EditorGUILayout.PropertyField(EnableLapisCastEventOutput);
            EditorGUILayout.PropertyField(DisableLogOnNonWindows);

            EditorGUILayout.PropertyField(StandaloneTimelineOffset, new GUIContent("StandaloneTimelineOffset", "LapisCast単独使用の際のタイムソースオフセットです。"));
            EditorGUILayout.PropertyField(StreamTimelineOffset, new GUIContent("StreamTimelineOffset", "配信のタイムソースを使用する際のオフセットです。"));
            EditorGUILayout.PropertyField(UseStreamTimestamp, new GUIContent("UseStreamTimestamp", "デフォルトで動画配信内のタイムソースを使用するか"));
            EditorGUILayout.PropertyField(ForceFallbackStandaloneTime, new GUIContent("ForceFallbackStandaloneTime", "StreamTimelineの時間軸を強制的にUnixTimeに固定します"));

            if (GimmickDebugMode != null) GimmickDebugMode.boolValue = false;

            if (DefaultStandaloneTimelineOffset != null && StandaloneTimelineOffset != null)
                DefaultStandaloneTimelineOffset.floatValue = StandaloneTimelineOffset.floatValue;
            
            if (DefaultStreamTimelineOffset != null && StreamTimelineOffset != null)
                DefaultStreamTimelineOffset.floatValue = StreamTimelineOffset.floatValue;

            // 変更をコンポーネントへ適用（Undoの記録含む）
            serializedObject.ApplyModifiedProperties();
        }
    }
}