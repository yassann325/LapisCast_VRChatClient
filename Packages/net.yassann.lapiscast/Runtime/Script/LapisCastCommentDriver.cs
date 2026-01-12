
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public enum FixXSide{
    Center,
    FixLeft,
    FixRight
}

public enum FixYSide{
    Center,
    FixUp,
    FixBottom
}

public class LapisCastCommentDriver : UdonSharpBehaviour
{
    [SerializeField]
    private TMP_Text text;

    [SerializeField]
    private FixXSide fixXSide = FixXSide.FixLeft;
    [SerializeField]
    private FixYSide fixYSide = FixYSide.Center;

    private float startTime = 0;
    [SerializeField]
    private float lifetime = 5;
    [SerializeField]
    private float speed = 0;
    private Vector3 moveDir = Vector3.left;

    void Start()
    {
        if(!text){
            text = gameObject.GetComponent<TMP_Text>();
        }
        startTime = Time.time;
    }

    void Update()
    {
        if(Time.time - startTime > lifetime){
            Destroy(gameObject);
        }
        transform.Translate(moveDir * speed * Time.deltaTime);  
    }

    public void ConfigCommnetText(string commnet, Vector3 startLcoalPos, Quaternion startLcoalRot, Vector3 move, Transform parent, float addtionalSpeed){
        text.text = commnet;
        moveDir = move;
        speed += addtionalSpeed;

        RectTransform textTransform = text.gameObject.GetComponent<RectTransform>();
        Vector2 textSize = textTransform.sizeDelta;

        transform.localPosition = startLcoalPos;
        transform.localRotation = startLcoalRot;

        if(fixXSide == FixXSide.FixLeft){
            transform.localPosition = new Vector3(transform.localPosition.x + textSize.x / 2, transform.localPosition.y, transform.localPosition.z);
        }
        else if(fixXSide == FixXSide.FixRight){
            transform.localPosition = new Vector3(transform.localPosition.x - textSize.x / 2, transform.localPosition.y, transform.localPosition.z);
        }

        if(fixYSide == FixYSide.FixUp){
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - textSize.y / 2, transform.localPosition.z);
        }
        else if(fixYSide == FixYSide.FixBottom){
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + textSize.y / 2, transform.localPosition.z);
        }

        if(parent){
            transform.SetParent(parent, false);
        }
    }
}
