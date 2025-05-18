using UnityEngine;

namespace Unity.FPS.zzy.player
{
public class zzyRoute : MonoBehaviour
{
    //旋转类：使得组件可以围绕父组件匀速旋转
    public Transform target; //目标对象
    public float speed = 100; //旋转速度

    void LateUpdate()
    {
        if (target != null)
        {
            transform.LookAt(target); // 使得自身朝向目标对象
            transform.RotateAround(target.position, Vector3.up, speed * Time.deltaTime); // 围绕目标对象旋转
        }  
    } 

    // Start is called once before the first execution of Update after the MonoBehaviour is created



    void Start()
    {
        //父组件
        target=transform.parent;
        //初始化位置    
    }

    // Update is called once per frame
    void Update()
    {
        LateUpdate();
    }
}
}