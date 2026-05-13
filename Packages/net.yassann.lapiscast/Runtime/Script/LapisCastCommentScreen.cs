
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.UdonNetworkCalling;
using LapisCast;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LapisCastCommentScreen : LapisCastBehaviour
{
    public string CommentScreenSpaceName = "default";
    public uint MaxCommentLength = 200;
    public GameObject CommnetTemplate;
    public Vector3 CommentObjectSpeed = new Vector3(-1, 0, 0);
    public Transform CommnetsParent;
    public LapisCastCommentScreenCommentSpawnCursor CommnetCursor;

    void Start()
    {
        LapisCastBehaviourInit(CommentScreenSpaceName);
        // 確認用に適用後のスペース名を変数に再適用
        CommentScreenSpaceName = GetSpaceName();
    }

    public override void OnLapisCastEvent(double timestamp, string keyname, DataToken value, bool sameinstance)
    {
        // 同じインスタンスからのメッセージは無視する
        if (!sameinstance)
            CreateNewCommnet(value.String);
    }

    // 新しいコメントを生成する
    // インスタンス内に同期
    public void AdventLocalInstanceMessage(string comment)
    {
        SendCustomNetworkEvent(NetworkEventTarget.All, "OnAdventLocalInstanceMessage", comment);
    }

    // コメント生成のイベントを受けとる
    // Lapisに送信
    [NetworkCallable]
    public void OnAdventLocalInstanceMessage(string comment)
    {
        SendLapisCast("c", comment);
        CreateNewCommnet(comment);
    }

    // スクリーン上にコメントオブジェクトを生成
    public void CreateNewCommnet(string commnet){
        GameObject commnetObject = Instantiate(CommnetTemplate);
        commnetObject.SetActive(true);
        LapisCastCommentDriver commentDriver = commnetObject.GetComponent<LapisCastCommentDriver>();
    
        Vector3 localSpawnPoint = CommnetCursor.GenerateLocalSpawnPoint();
        
        Vector3 moveDir = Vector3.Scale(transform.localScale, CommentObjectSpeed);

        string commnetText = commnet.Length > MaxCommentLength ? commnet.Substring(0, (int)MaxCommentLength): commnet;
        float addtionalSpeed = 1 * commnetText.Length/100;

        commentDriver.ConfigCommnetText(commnetText, CommnetCursor.transform.localPosition + localSpawnPoint, CommnetCursor.transform.localRotation, moveDir, CommnetsParent, addtionalSpeed);
    }

    // Send Test Commnet
    public void SendTestCommnet(){
        AdventLocalInstanceMessage("Hello LapisCast!");
    }
}
