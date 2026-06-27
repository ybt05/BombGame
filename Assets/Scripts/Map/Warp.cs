using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class Warp : MonoBehaviour
{
    public static List<Warp> warps = new List<Warp>();
    Dictionary<GameObject, float> lastWarpTimes = new Dictionary<GameObject, float>();

    void Awake()
    {
        warps.Add(this);
    }

    void OnDestroy()
    {
        warps.Remove(this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!GameMode.IsSingle && !NetworkManager.Singleton.IsServer) return;

        if (!other.CompareTag("Player") && !other.CompareTag("Bomb")) return;

        if (lastWarpTimes.ContainsKey(other.gameObject))
        {
            if (Time.time - lastWarpTimes[other.gameObject] < 0.3f) return;
        }

        foreach (var w in warps)
        {
            if (w != this)
            {
                Vector3 targetPos = w.transform.position;
                targetPos.y += 1.5f;

                other.transform.position = targetPos;

                lastWarpTimes[other.gameObject] = Time.time;

                break;
            }
        }
    }
}
