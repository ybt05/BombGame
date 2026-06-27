using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = target.position;
        transform.position = new Vector3(pos.x, 15, pos.z-2);
        transform.rotation = Quaternion.Euler(80f, 0f, 0f);
    }
}
