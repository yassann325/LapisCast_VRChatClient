
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using LapisCast;

public class LapisCastCommentScreen : LapisCastBehaviour
{
    [SerializeField]
    private string CommentScreenSpaceName = "default";
    [SerializeField]
    private uint MaxCommentLength = 200;
    [SerializeField]
    private GameObject CommnetTemplate;
    [SerializeField]
    private Vector3 CommentObjectSpeed = new Vector3(-1, 0, 0);
    [SerializeField]
    private Transform CommnetsParent;
    [SerializeField]
    private LapisCastCommentScreenCommentSpawnCursor CommnetCursor;

    void Start()
    {
        LapisCastBehaviourInit(CommentScreenSpaceName);
        // 確認用に適用後のスペース名を変数に再適用
        CommentScreenSpaceName = GetSpaceName();
    }

    public override void OnLapisCastEvent(string spanename, string keyname, DataToken value, bool sameinstance)
    {
        CreateNewCommnet(value.String);
    }

    // コメント生成
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

    // Test
    public void SendTestCommnet(){
        CreateNewCommnet("Hello LapisCast!");
    }
}
