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
    public ulong clientId;


    void Start()
    {
        rends = GetComponentsInChildren<Renderer>();

        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
        initialSpawnPosition = transform.position;

    }
    public override void OnNetworkSpawn()
    {
        if (!GameMode.IsSingle)
        {
            // ✅ プレイヤー
            if (GetComponent<PlayerController>() != null)
            {
                clientId = OwnerClientId;
            }
            else
            {
                // ✅ CPUはユニークID
                clientId = (ulong)(100000 + Random.Range(1, 100000));
            }
        }
        else
        {
            clientId = (ulong)Random.Range(1, 100000);
        }
    }
    public void TakeDamage(ulong attackerId)
    {
        if (isDead || isInvincible) return;

        if (GameMode.IsSingle)
        {
            ProcessKill(attackerId);
            StartCoroutine(Death());
            return;
        }

        if (IsServer)
        {
            ProcessKill(attackerId);
            SetDeadClientRpc();
        }
        else
        {
            TakeDamageServerRpc(attackerId);
        }
    }

    [Rpc(SendTo.Server)]
    void TakeDamageServerRpc(ulong attackerId)
    {
        ProcessKill(attackerId);
        SetDeadClientRpc();
    }


    void ProcessKill(ulong attackerId)
    {
        ulong victimId = clientId;

        if (attackerId == victimId)
        {
            GameManager.Instance.AddDeath(victimId);
            GameManager.Instance.AddKill(attackerId, victimId);
            return;
        }

        GameManager.Instance.AddKill(attackerId, victimId);
    }
    IEnumerator Death()
    {
        isDead = true;
        SoundManager.Instance.PlaySE(SoundManager.Instance.deathSE);

        // ✅ 追加：即非表示
        foreach (var r in rends)
        {
            r.enabled = false;
        }

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
