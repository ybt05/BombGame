using UnityEngine;
using System.Collections;
using Unity.Netcode;


public class Item : NetworkBehaviour
{
    public ItemType type;

    private Vector3 spawnPos;

    void Start()
    {
        spawnPos = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameMode.IsSingle)
        {
            Apply(other.gameObject);
            StartCoroutine(Respawn());
            SetItemActive(false);
        }
        else
        {
            if (!IsServer) return;
            Apply(other.gameObject);
            StartCoroutine(Respawn());
            SetItemActive(false);
        }
    }

    void Apply(GameObject player)
    {
        if (!GameMode.IsSingle && !NetworkManager.Singleton.IsServer) return;
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats == null) return;

        switch (type)
        {
            case ItemType.BombUp:
                stats.bombCount.Value = Mathf.Min(stats.bombCount.Value + 1, 8);
                break;

            case ItemType.BombDown:
                stats.bombCount.Value = Mathf.Max(stats.bombCount.Value - 1, 1);
                break;

            case ItemType.PowerUp:
                stats.power.Value = Mathf.Min(stats.power.Value + 1, 8);
                break;

            case ItemType.PowerDown:
                stats.power.Value = Mathf.Max(stats.power.Value - 1, 1);
                break;

            case ItemType.SpeedUp:
                stats.speed.Value = Mathf.Min(stats.speed.Value + 1, 8);
                break;

            case ItemType.SpeedDown:
                stats.speed.Value = Mathf.Max(stats.speed.Value - 1, 1);
                break;

            case ItemType.Kick:
                stats.canKick.Value = true;
                break;

            case ItemType.Punch:
                stats.canPunch.Value = true;
                break;

            case ItemType.Jump:
                stats.canJump.Value = true;
                break;

            case ItemType.RemoteBomb:
                stats.remoteBomb.Value = Mathf.Min(stats.remoteBomb.Value + 2, 10);
                break;

            case ItemType.PierceBomb:
                stats.pierceBomb.Value = Mathf.Min(stats.pierceBomb.Value + 2, 10);
                break;
        }
    }
    void SetItemActive(bool active)
    {
        if (GameMode.IsSingle)
        {
            gameObject.SetActive(active);
        }
        else
        {
            if (NetworkManager.Singleton.IsServer)
            {
                SetItemActiveClientRpc(active);
            }
        }
    }
    [ClientRpc]
    void SetItemActiveClientRpc(bool active)
    {
        gameObject.SetActive(active);
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(10f);

        if (!GameMode.IsSingle && !NetworkManager.Singleton.IsServer) yield break; // ✅ 追加

        transform.position = spawnPos;
        SetItemActive(true);
    }


}
