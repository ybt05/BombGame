using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    public string playerName;
    public Transform spawnPoint;

    private bool isDead = false;
    private bool isInvincible = false;
    private Renderer[] rends;
    private Coroutine invincibleRoutine;
    private Vector3 initialSpawnPosition;


    void Start()
    {
        rends = GetComponentsInChildren<Renderer>();

        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
        initialSpawnPosition = transform.position;

    }

    public void TakeDamage(string attackerName)
    {
        if (isDead || isInvincible) return;

        if (GameMode.IsSingle)
        {
            ProcessKillByName(attackerName);
            StartCoroutine(Death());
            return;
        }

        if (IsServer)
        {
            ProcessKillByName(attackerName);
            SetDeadClientRpc();
        }
        else
        {
            TakeDamageServerRpc(attackerName);
        }
    }


    [Rpc(SendTo.Server)]
    void TakeDamageServerRpc(string attackerName)
    {
        ProcessKillByName(attackerName);
        SetDeadClientRpc(); // ✅ 全員に通知
    }


    void ProcessKillByName(string attackerName)
    {
        if (attackerName == playerName)
        {
            GameManager.Instance.AddDeath(playerName);
            // ✅ これ追加！！
            GameManager.Instance.AddKill(playerName, playerName);
            return;
        }

        GameManager.Instance.AddKill(attackerName, playerName);
    }


    void ProcessKill(ulong attackerId)
    {
        // ✅ Networkがない場合安全ガード
        if (NetworkManager.Singleton == null)
            return;

        string killerName = "Unknown";

        // ✅ attackerが存在する場合
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(attackerId))
        {
            var attackerClient = NetworkManager.Singleton.ConnectedClients[attackerId];

            if (attackerClient != null && attackerClient.PlayerObject != null)
            {
                var attackerPH = attackerClient.PlayerObject.GetComponent<PlayerHealth>();

                if (attackerPH != null)
                {
                    killerName = attackerPH.playerName;
                }
            }
        }

        // ✅ 自分自身（自爆）
        if (killerName == playerName)
        {
            GameManager.Instance.AddDeath(playerName);
            return;
        }

        GameManager.Instance.AddKill(killerName, playerName);
    }




    IEnumerator Death()
    {
        isDead = true;

        yield return new WaitForSeconds(5f);

        if (GameMode.IsSingle || IsServer)
        {
            Respawn();
        }
    }


    void Respawn()
    {
        if (GameMode.IsSingle)
        {
            transform.position = initialSpawnPosition;

            foreach (var r in rends)
            {
                r.enabled = true;
            }

            isDead = false;
        }
        else if (IsServer)
        {
            RespawnClientRpc(initialSpawnPosition); // ✅
        }

        if (invincibleRoutine != null)
        {
            StopCoroutine(invincibleRoutine);
        }

        invincibleRoutine = StartCoroutine(InvincibleTime());
    }


    IEnumerator InvincibleTime()
    {
        isInvincible = true;

        float time = 10f;
        float timer = 0;

        while (timer < time)
        {
            // ✅ 途中で解除されたら即終了
            if (!isInvincible)
            {
                yield break;
            }

            timer += Time.deltaTime;

            foreach (var r in rends)
            {
                r.enabled = !r.enabled;
            }

            yield return null;
        }

        foreach (var r in rends)
        {
            r.enabled = true;
        }

        isInvincible = false;
        invincibleRoutine = null;
    }
    public void CancelInvincible()
    {
        isInvincible = false;

        // ✅ コルーチン止める
        if (invincibleRoutine != null)
        {
            StopCoroutine(invincibleRoutine);
            invincibleRoutine = null;
        }

        foreach (var r in rends)
        {
            r.enabled = true;
        }
    }
    public bool IsDead()
    {
        return isDead;
    }
    [ClientRpc]
    void SetDeadClientRpc()
    {
        isDead = true; // ✅ 追加

        foreach (var r in rends)
            r.enabled = false;

        StartCoroutine(Death()); // ✅ ここで統一
    }
    [ClientRpc]
    void RespawnClientRpc(Vector3 pos)
    {
        transform.position = pos;

        foreach (var r in rends)
        {
            r.enabled = true;
        }

        isDead = false;
    }
}
