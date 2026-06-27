using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class BreakableWall : MonoBehaviour
{
    public GameObject[] itemPrefabs; // Inspectorで順番に入れる

    private Vector3 spawnPos;

    void Start()
    {
        spawnPos = transform.position;
    }


    private bool isBroken = false;

    public void Break()
    {
        if (isBroken) return; // ✅ 追加
        isBroken = true;      // ✅ 追加

        if (!GameMode.IsSingle && !NetworkManager.Singleton.IsServer) return;

        StartCoroutine(BreakDelay());
    }
    IEnumerator BreakDelay()
    {
        yield return null;

        if (Random.value < 0.7f)
        {
            SpawnItem();
        }

        if (GameMode.IsSingle)
        {
            Destroy(gameObject);
        }
        else
        {
            if (TryGetComponent<NetworkObject>(out var net))
            {
                net.Despawn();
            }
        }
    }

    [ClientRpc]
    void SetActiveClientRpc(bool active)
    {
        gameObject.SetActive(active);
    }

    void SpawnItem()
    {
        if (itemPrefabs.Length == 0) return;

        int index = GetRandomItemIndex();

        GameObject item = Instantiate(
            itemPrefabs[index],
            spawnPos + Vector3.up * 0.1f,
            Quaternion.Euler(0f, 0f, 180f)
        );

        if (!GameMode.IsSingle)
        {
            item.GetComponent<NetworkObject>().Spawn(true);
        }
    }

    int GetRandomItemIndex()
    {
        float r = Random.value * 100f;

        if (r < 18f) return 0;   // BombUp 18%
        if (r < 21f) return 1;   // BombDown 3%
        if (r < 39f) return 2;   // PowerUp 18%
        if (r < 42f) return 3;   // PowerDown 3%
        if (r < 57f) return 4;   // SpeedUp 15%
        if (r < 60f) return 5;   // SpeedDown 3%
        if (r < 70f) return 6;   // Kick 10%
        if (r < 80f) return 7;   // Punch 10%
        if (r < 90f) return 9;   // Remote 10%
        return 10; // Pierce 10%
    }
}