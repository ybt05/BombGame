using UnityEngine;
using System.Collections;
using Unity.Netcode;

public enum BombType
{
    Normal,
    Remote,
    Pierce
}


public class Bomb : NetworkBehaviour
{
    public float explodeTime = 3f;
    public int power = 1;
    public ulong ownerId;
    public string ownerName;

    public BombType type = BombType.Normal;

    public bool isMoving = false;
    private Vector3 moveDir;
    public float moveSpeed = 5f;
    private bool isExploded = false;

    private Collider bombCollider;
    private Collider ownerCollider;
    private bool ignoreActive = false;
    public BombSpawner ownerSpawner;
    public GameObject explosionEffect;


    public override void OnNetworkSpawn()
    {
        bombCollider = GetComponent<Collider>();

        if (!GameMode.IsSingle)
        {
            SetupOwnerCollider(); // ✅ マルチ用
        }

        if (IsServer)
        {
            StartCoroutine(ExplodeTimer()); // ✅ サーバーのみ
        }
    }
    void Start()
    {
        bombCollider = GetComponent<Collider>();

        if (GameMode.IsSingle)
        {
            SetupOwnerCollider(); // ✅ シングルだけここ
            StartCoroutine(ExplodeTimer()); // ✅ シングル用タイマー
        }
    }
    void SetupOwnerCollider()
    {
        if (!GameMode.IsSingle && NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.ContainsKey(ownerId))
        {
            var player = NetworkManager.Singleton.ConnectedClients[ownerId].PlayerObject;
            if (player != null)
            {
                ownerCollider = player.GetComponent<Collider>();
            }
        }
        else
        {
            ownerCollider = FindAnyObjectByType<PlayerController>()?.GetComponent<Collider>();
        }

        if (ownerCollider != null)
        {
            Physics.IgnoreCollision(ownerCollider, bombCollider, true);
            ignoreActive = true;
        }
    }
    IEnumerator ExplodeTimer()
    {
        yield return new WaitForSeconds(explodeTime);
        ExplodeNow();
    }

    public void ForceExplode()
    {
        if (GameMode.IsSingle)
        {
            StopAllCoroutines();
            ExplodeNow();
        }
        else
        {
            ForceExplodeServerRpc();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    void ForceExplodeServerRpc()
    {
        StopAllCoroutines();
        ExplodeNow();
    }
    BombSpawner FindOwnerSpawner()
    {
        BombSpawner[] spawners = FindObjectsByType<BombSpawner>();

        foreach (var s in spawners)
        {
            if (GameMode.IsSingle)
            {
                return s; // シングルは1人だけ
            }
            else if (s.OwnerClientId == ownerId)
            {
                return s;
            }
        }

        return null;
    }
    void ExplodeNow()
    {
        if (!IsServer && !GameMode.IsSingle) return;
        if (isExploded) return;
        isExploded = true;

        if (ownerSpawner != null)
        {
            ownerSpawner.BombDestroyed();
        }

        Vector3 basePos = new Vector3(
            Mathf.Round(transform.position.x),
            transform.position.y,
            Mathf.Round(transform.position.z)
        );
        SpawnExplosionEffect(basePos);

        Collider[] centerHits = Physics.OverlapBox(basePos, Vector3.one * 0.3f);

        foreach (var hit in centerHits)
        {
            if (hit.CompareTag("Player"))
            {
                var ph = hit.GetComponent<PlayerHealth>();
                if (ph != null)
                    ph.TakeDamage(ownerName);
            }
        }

        ExplodeDirection(Vector3.forward);
        ExplodeDirection(Vector3.back);
        ExplodeDirection(Vector3.left);
        ExplodeDirection(Vector3.right);

        // ✅ 最後に1回だけ消す
        if (GameMode.IsSingle)
        {
            Destroy(gameObject);
        }
        else if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
    void SpawnExplosionEffect(Vector3 pos)
    {
        if (GameMode.IsSingle)
        {
            SpawnExplosionEffectLocal(pos);
            return;
        }

        if (!IsServer) return;

        SpawnExplosionEffectClientRpc(pos);
    }
    void SpawnExplosionEffectLocal(Vector3 pos)
    {
        if (explosionEffect == null) return;

        GameObject effect = Instantiate(explosionEffect, pos, Quaternion.identity);
        Destroy(effect, 1f);
    }
    [ClientRpc]
    void SpawnExplosionEffectClientRpc(Vector3 pos)
    {
        if (explosionEffect == null) return;

        GameObject effect = Instantiate(explosionEffect, pos, Quaternion.identity);

        Destroy(effect, 1f);
    }

    void ExplodeDirection(Vector3 dir)
    {
        for (int i = 1; i <= power; i++)
        {

            Vector3 basePos = new Vector3(
                Mathf.Round(transform.position.x),
                transform.position.y,
                Mathf.Round(transform.position.z)
            );

            Vector3 pos = basePos + dir * i;
            SpawnExplosionEffect(pos);


            Collider[] hits = Physics.OverlapBox(pos, Vector3.one * 0.3f);

            bool stop = false;

            foreach (var hit in hits)
            {

                if (hit.CompareTag("Player"))
                {
                    PlayerHealth ph = hit.GetComponent<PlayerHealth>();

                    if (ph != null)
                    {
                        ph.TakeDamage(ownerName);
                    }
                }
                if (hit.CompareTag("Bomb"))
                {
                    if (hit.gameObject == gameObject) continue;
                    Bomb b = hit.GetComponent<Bomb>();
                    if (b != null)
                    {
                        b.ForceExplode(); // ✅ 連鎖爆発
                    }
                }
                if (hit.CompareTag("Breakable"))
                {
                    BreakableWall wall = hit.GetComponent<BreakableWall>();

                    if (wall != null)
                    {
                        wall.Break();
                    }
                    else
                    {
                        if (IsServer || GameMode.IsSingle)
                        {
                            Destroy(hit.gameObject);
                        }
                    }
                    // ✅ ここが重要！！
                    if (type == BombType.Pierce)
                        continue; // ←貫通して進む
                    stop = true; // ←通常ボムだけ止まる
                    SpawnExplosionEffect(pos);
                }
                if (hit.CompareTag("Wall"))
                {
                    if (type == BombType.Pierce) continue;
                    stop = true;
                    break;
                }

            }

            if (stop) break;
        }
    }
    void Update()
    {
        // ✅ プレイヤーが離れたら当たり判定戻す
        if ((IsServer || GameMode.IsSingle) && ignoreActive && ownerCollider != null)
        {
            float dist = Vector3.Distance(ownerCollider.transform.position, transform.position);

            if (dist > 1.2f) // ←調整可能
            {
                Physics.IgnoreCollision(ownerCollider, bombCollider, false);
                ignoreActive = false;
            }
        }

        // 既存の移動処理
        if ((IsServer || GameMode.IsSingle) && isMoving)
        {
            RaycastHit hit;

            if (Physics.Raycast(transform.position, moveDir, out hit, 0.6f))
            {
                if (hit.collider.CompareTag("Wall") ||
                    hit.collider.CompareTag("Breakable") ||
                    hit.collider.CompareTag("Bomb"))
                {
                    isMoving = false;
                    return;
                }
            }

            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
    }



    IEnumerator KickMove(Vector3 dir)
    {
        while (true)
        {
            RaycastHit hit;

            if (Physics.Raycast(transform.position, dir, out hit, 0.6f))
            {
                if (hit.collider.CompareTag("Wall") ||
                    hit.collider.CompareTag("Breakable") ||
                    hit.collider.CompareTag("Bomb"))
                {
                    yield break;
                }
            }

            transform.position += dir * 5f * Time.deltaTime;

            yield return null;
        }
    }

    public void Kick(Vector3 dir)
    {
        if (!GameMode.IsSingle && !IsServer) return;
        StartCoroutine(KickMove(dir));
    }



    public void Punch(Vector3 dir)
    {
        if (!GameMode.IsSingle && !IsServer) return;
        StartCoroutine(PunchMove(dir));
    }

    IEnumerator PunchMove(Vector3 dir)
    {
        Vector3 start = SnapToGrid(transform.position);
        Vector3 target = start + dir * 3f;

        float t = 0;
        float duration = 0.3f;

        while (t < 1)
        {
            t += Time.deltaTime / duration;

            // 放物線（ジャンプぽさ）
            float height = Mathf.Sin(t * Mathf.PI) * 1.5f;

            Vector3 pos = Vector3.Lerp(start, target, t);
            pos.y += height;

            transform.position = pos;

            yield return null;
        }

        // ✅ 着地位置補正（グリッド）
        transform.position = SnapToGrid(target);
    }
    Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x),
            pos.y,
            Mathf.Round(pos.z)
        );
    }

    public void ExplodeRemote()
    {
        if (type == BombType.Remote)
        {
            StopAllCoroutines();
            ExplodeNow();
        }
    }
    public void SetOwner(PlayerController player)
    {
        Collider playerCol = player.GetComponent<Collider>();
        Collider bombCol = GetComponent<Collider>();

        Physics.IgnoreCollision(playerCol, bombCol, true);

        StartCoroutine(EnableCollision(playerCol, bombCol));
    }
    IEnumerator EnableCollision(Collider playerCol, Collider bombCol)
    {
        yield return new WaitForSeconds(0.5f); // 少し離れる時間

        if (playerCol != null && bombCol != null)
        {
            Physics.IgnoreCollision(playerCol, bombCol, false);
        }
    }

}