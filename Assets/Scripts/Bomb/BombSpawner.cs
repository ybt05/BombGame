using UnityEngine;
using Unity.Netcode;
public class BombSpawner : NetworkBehaviour
{
    public GameObject bombPrefab;
    public int maxBomb = 1;
    private int currentBomb = 0;
    private PlayerStats stats;
    private PlayerHealth health;


    void Start()
    {
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
    }


    float lastPlaceTime = 0f;

    void Update()
    {
        if (!GameMode.IsSingle && !IsOwner) return;
        if (GameManager.Instance == null) return;
        if (!GameManager.Instance.IsPlaying) return;

        if (Input.GetMouseButtonDown(0) && Time.time - lastPlaceTime > 0.1f)
        {
            lastPlaceTime = Time.time;
            TryPlaceBomb();
        }
    }


    public void TryPlaceBomb()
    {
        Vector3 pos = new Vector3(
            Mathf.Round(transform.position.x),
            1,
            Mathf.Round(transform.position.z)
        );

        if (GameMode.IsSingle)
        {
            PlaceBombLocal(pos);
        }
        else
        {
            PlaceBombServerRpc(pos);
        }
    }

    void PlaceBombLocal(Vector3 pos)
    {
        if (currentBomb >= stats.bombCount.Value) return;

        // ✅ 無敵解除
        if (health != null)
        {
            health.CancelInvincible();
        }
        GameObject bomb = Instantiate(bombPrefab, pos, Quaternion.identity);

        Bomb b = bomb.GetComponent<Bomb>();
        b.power = stats.power.Value;
        b.ownerId = 0; // ✅ 追加（シングル対策）
        b.ownerName = health.playerName;
        b.ownerSpawner = this;
        SetBombType(b);

        currentBomb++;
    }

    [ServerRpc]
    void PlaceBombServerRpc(Vector3 pos, ServerRpcParams rpcParams = default)
    {
        // ✅ プレイヤー一致チェック
        if (OwnerClientId != rpcParams.Receive.SenderClientId) return;

        var serverStats = GetComponent<PlayerStats>();

        if (currentBomb >= serverStats.bombCount.Value) return;

        if (health != null)
        {
            health.CancelInvincible();
        }

        GameObject bomb = Instantiate(bombPrefab, pos, Quaternion.identity);

        Bomb b = bomb.GetComponent<Bomb>();
        b.power = stats.power.Value;
        b.ownerId = OwnerClientId;
        b.ownerName = health.playerName;
        b.ownerSpawner = this;

        SetBombType(b);

        bomb.GetComponent<NetworkObject>().Spawn();

        currentBomb++;
    }

    void SetBombType(Bomb b)
    {
        // ✅ Remote
        if (stats.currentBombType.Value == (int)BombType.Remote && stats.remoteBomb.Value > 0)
        {
            b.type = BombType.Remote;
            stats.remoteBomb.Value--;

            if (stats.remoteBomb.Value <= 0)
            {
                stats.currentBombType.Value = (int)BombType.Normal;
            }
        }
        // ✅ Pierce
        else if (stats.currentBombType.Value == (int)BombType.Pierce && stats.pierceBomb.Value > 0)
        {
            b.type = BombType.Pierce;
            stats.pierceBomb.Value--;

            if (stats.pierceBomb.Value <= 0)
            {
                stats.currentBombType.Value = (int)BombType.Normal;
            }
        }
        // ✅ Normal
        else
        {
            b.type = BombType.Normal;
        }
    }
    public void PlaceBombLocalAI()
    {
        if (currentBomb >= stats.bombCount.Value) return;

        Vector3 pos = new Vector3(
            Mathf.Round(transform.position.x),
            1,
            Mathf.Round(transform.position.z)
        );

        if (health != null)
        {
            health.CancelInvincible();
        }

        GameObject bomb = Instantiate(bombPrefab, pos, Quaternion.identity);

        Bomb b = bomb.GetComponent<Bomb>();
        b.power = stats.power.Value;
        b.ownerId = OwnerClientId;
        b.ownerName = health.playerName;
        b.ownerSpawner = this;

        SetBombType(b);

        if (!GameMode.IsSingle)
        {
            bomb.GetComponent<NetworkObject>().Spawn();
        }

        currentBomb++;
    }

    public void BombDestroyed()
    {
        currentBomb = Mathf.Max(0, currentBomb - 1);
    }
}