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
            StandaloneTimelineOffset = serializedObject.FindProperty("StandaloneTimelineOffset");
            DefaultStandaloneTimelineOffset = serializedObject.FindProperty("_DefaultStandaloneTimelineOffset");
            StreamTimelineOffset = serializedObject.FindProperty("StreamTimelineOffset");
            DefaultStreamTimelineOffset = serializedObject.FindProperty("_DefaultStreamTimelineOffset");
            UseStreamTimestamp = serializedObject.FindProperty("UseStreamTimestamp");
            ForceFallbackStandaloneTime = serializedObject.FindProperty("forceFallbackStandaloneTime");
        }

        public override void OnInspectorGUI()
        {
            LapisCastCore lapiscastcore = null;
            if (target is Component component)
            {
                lapiscastcore = component.GetComponent<LapisCastCore>();
                if (lapiscastcore == null) return;
            }

            // データの同期開始
            serializedObject.Update();

            EditorGUILayout.LabelField("LapisCastインスタンスのURL", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(InstanceURL, new GUIContent("InstanceURL", "受信するLapisCastインスタンスURL"));
            SerializedProperty urlProp = InstanceURL.FindPropertyRelative("url");
            if (urlProp != null && (string.IsNullOrEmpty(urlProp.stringValue) || urlProp.stringValue == "https://lapis.yassann.net/lapiscast/public/{instanceid-here}"))
            {
                EditorGUILayout.HelpBox("LapisCast インスタンスURLが設定されていません。", MessageType.Warning);
            }

            lapiscastcore.AutoTimeAjustFirst = EditorGUILayout.Toggle("AutoTimeAjustFirst", lapiscastcore.AutoTimeAjustFirst);
            lapiscastcore.StartWaiting = EditorGUILayout.FloatField("StartWaiting", lapiscastcore.StartWaiting);
            lapiscastcore.LoadingInterval = EditorGUILayout.FloatField("LoadingInterval", lapiscastcore.LoadingInterval);
            lapiscastcore.MaxEventDelay = EditorGUILayout.FloatField("MaxEventDelay", lapiscastcore.MaxEventDelay);
            lapiscastcore.EnableLapisCast = EditorGUILayout.Toggle("EnableLapisCast", lapiscastcore.EnableLapisCast);
            lapiscastcore.EnableLapisCastEventExec = EditorGUILayout.Toggle("EnableLapisCastEventExec", lapiscastcore.EnableLapisCastEventExec);
            lapiscastcore.EnableLapisCastEventOutput = EditorGUILayout.Toggle("EnableLapisCastEventOutput", lapiscastcore.EnableLapisCastEventOutput);
            lapiscastcore.DisableLogOnNonWindows = EditorGUILayout.Toggle("DisableLogOnNonWindows", lapiscastcore.DisableLogOnNonWindows);

            EditorGUILayout.PropertyField(StandaloneTimelineOffset, new GUIContent("StandaloneTimelineOffset", "LapisCast単独使用の際のタイムソースオフセットです。"));
            EditorGUILayout.PropertyField(StreamTimelineOffset, new GUIContent("StreamTimelineOffset", "配信のタイムソースを使用する際のオフセットです。"));
            EditorGUILayout.PropertyField(UseStreamTimestamp, new GUIContent("UseStreamTimestamp", "デフォルトで動画配信内のタイムソースを使用するか"));
            EditorGUILayout.PropertyField(ForceFallbackStandaloneTime, new GUIContent("ForceFallbackStandaloneTime", "StreamTimelineの時間軸を強制的にUnixTimeに固定します"));

            lapiscastcore.GimmickDebugMode = false;
            DefaultStandaloneTimelineOffset.floatValue = StandaloneTimelineOffset.floatValue;
            DefaultStreamTimelineOffset.floatValue = StreamTimelineOffset.floatValue;

            // 変更をコンポーネントへ適用（Undoの記録含む）
            serializedObject.ApplyModifiedProperties();
        }
    }
}