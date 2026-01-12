
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LapisCastCommentScreenCommentSpawnCursor : UdonSharpBehaviour
{
    public Vector3 size = Vector3.one;
    public bool cacheBoundingBox = true;
    private Bounds boundBox;

    void Start()
    {
        if(cacheBoundingBox){
            boundBox = new Bounds(transform.position, Vector3.Scale(size, transform.lossyScale));
        }
    }

    //ボックスをワールドに描画
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Bounds bounds = new Bounds(transform.position, Vector3.Scale(size, transform.lossyScale));
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
    

    public Vector3 GenerateGlobalSpawnPoint(){
        if(!cacheBoundingBox){
            boundBox = new Bounds(transform.position, Vector3.Scale(size, transform.lossyScale));
        }
        Vector3 max = boundBox.max;
        Vector3 min = boundBox.min;
        float x = Random.Range(min.x , max.x);
        float y = Random.Range(min.y , max.y);
        float z = Random.Range(min.z , max.z);
        return new Vector3(x, y, z);
    }

    public Vector3 GenerateLocalSpawnPoint(){
        return transform.InverseTransformPoint(GenerateGlobalSpawnPoint());
    }
}
