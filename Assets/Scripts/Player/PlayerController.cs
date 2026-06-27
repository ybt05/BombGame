using UnityEngine;
using Unity.Netcode;
using System.Collections;
public class PlayerController : NetworkBehaviour
{
    public float speed = 5f;
    private Rigidbody rb;
    private Vector3 move;
    private PlayerHealth health;

    private PlayerStats stats;
    public bool ignoreBombCollision = false;
    private bool isJumping = false;
    public Vector3 externalVelocity;
    public NetworkVariable<Vector3> netMove =
        new NetworkVariable<Vector3>(
            writePerm: NetworkVariableWritePermission.Owner
        );



    void Start()
    {
        rb = GetComponent<Rigidbody>();
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
    }


    void Update()
    {
        if (!GameMode.IsSingle && !IsOwner) return;
        if (!GameManager.Instance) return;

        if (!GameManager.Instance.IsPlaying) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");


        Vector3 input = new Vector3(h, 0, v);

        if (GameMode.IsSingle)
        {
            move = input;
        }
        else
        {
            netMove.Value = input;
        }


        // 右クリックで切替
        if (Input.GetMouseButtonDown(1))
        {
            ChangeBomb();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ExplodeAllRemote();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryKick();
        }


        if (Input.GetKeyDown(KeyCode.F))
        {
            TryPunch();
        }
        if (Input.GetKey(KeyCode.Space))
        {
            TryJump();
        }

    }
    void TryKick()
    {
        if (!stats.canKick.Value) return;

        Vector3 dir = GetInputDirection();
        if (dir == Vector3.zero) return;

        if (GameMode.IsSingle)
        {
            TryKickServer(dir);
        }
        else
        {
            KickServerRpc(dir);
        }
    }
    void TryPunch()
    {
        if (!stats.canPunch.Value) return;

        Vector3 dir = GetInputDirection();
        if (dir == Vector3.zero) return;

        if (GameMode.IsSingle)
        {
            TryPunchServer(dir);
        }
        else
        {
            PunchServerRpc(dir);
        }
    }

    Vector3 GetInputDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(h) > Mathf.Abs(v))
            return new Vector3(Mathf.Sign(h), 0, 0);

        if (Mathf.Abs(v) > 0)
            return new Vector3(0, 0, Mathf.Sign(v));

        return Vector3.zero;
    }
    IEnumerator JumpTime()
    {
        isJumping = true;
        ignoreBombCollision = true;

        yield return new WaitForSeconds(0.5f);

        ignoreBombCollision = false;
        isJumping = false;

        Collider[] bombs = Physics.OverlapSphere(transform.position, 2f);
        foreach (var b in bombs)
        {
            if (b.CompareTag("Bomb"))
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), b, false);
            }
        }
    }



    void TryJump()
    {
        if (!stats.canJump.Value) return;
        if (isJumping) return; // ✅ 追加

        StartCoroutine(JumpTime());
    }
    void OnCollisionStay(Collision col)
    {
        if (ignoreBombCollision && col.gameObject.CompareTag("Bomb"))
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), col.collider, true);
        }
    }

    void FixedUpdate()
    {
        if (!GameMode.IsSingle && !IsServer && !IsOwner) return;
        if (health != null && health.IsDead()) return;

        float moveSpeed = stats.speed.Value * 2f;

        Vector3 input = GameMode.IsSingle
            ? move.normalized * moveSpeed
            : netMove.Value.normalized * moveSpeed;

        Vector3 moveX = new Vector3(input.x, 0, 0);
        Vector3 moveZ = new Vector3(0, 0, input.z);

        Vector3 finalVelocity = new Vector3(0, rb.linearVelocity.y, 0);

        if (moveX != Vector3.zero && !Physics.Raycast(transform.position, moveX.normalized, 0.6f))
        {
            finalVelocity.x = moveX.x;
        }

        if (moveZ != Vector3.zero && !Physics.Raycast(transform.position, moveZ.normalized, 0.6f))
        {
            finalVelocity.z = moveZ.z;
        }

        finalVelocity.x += externalVelocity.x;
        finalVelocity.z += externalVelocity.z;

        rb.linearVelocity = finalVelocity;

        if (IsServer || GameMode.IsSingle)
        {
            externalVelocity = Vector3.zero;
        }
    }

    [ServerRpc]
    void ChangeBombServerRpc(BombType type, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        stats.currentBombType.Value = (int)type;
    }
    void ChangeBomb()
    {
        BombType next = (BombType)stats.currentBombType.Value;

        for (int i = 0; i < 3; i++)
        {
            next++;
            if ((int)next > 2)
                next = 0;

            // ✅ Normalは常にOK
            if (next == BombType.Normal)
            {
                if (GameMode.IsSingle)
                {
                    stats.currentBombType.Value = (int)next;
                }
                else
                {
                    ChangeBombServerRpc(next);
                }
                return;
            }
            // ✅ Remote持ってるかチェック
            if (next == BombType.Remote && stats.remoteBomb.Value > 0)
            {
                if (GameMode.IsSingle)
                {
                    stats.currentBombType.Value = (int)next;
                }
                else
                {
                    ChangeBombServerRpc(next);
                }
                return;
            }

            // ✅ Pierce持ってるかチェック
            if (next == BombType.Pierce && stats.pierceBomb.Value > 0)
            {

                if (GameMode.IsSingle)
                {
                    stats.currentBombType.Value = (int)next;
                }
                else
                {
                    ChangeBombServerRpc(next);
                }

                return;
            }
        }
    }

    void ExplodeAllRemote()
    {
        if (GameMode.IsSingle)
        {
            ExplodeRemoteServer();
        }
        else
        {
            ExplodeRemoteServerRpc();
        }
    }
    [ServerRpc]
    void ExplodeRemoteServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        ExplodeRemoteServer();
    }
    void ExplodeRemoteServer()
    {
        Bomb[] bombs = FindObjectsByType<Bomb>();

        foreach (var b in bombs)
        {
            if (b.ownerId == OwnerClientId || GameMode.IsSingle)
            {
                b.ExplodeRemote();
            }
        }
    }

    [ServerRpc]
    void KickServerRpc(Vector3 dir, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        TryKickServer(dir);
    }

    void TryKickServer(Vector3 _)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.2f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Bomb"))
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;

                // 軸揃え（グリッド用）
                dir = GetAxisDir(dir);

                hit.GetComponent<Bomb>()?.Kick(dir);
            }
        }
    }
    [ServerRpc]
    void PunchServerRpc(Vector3 dir, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

    }

    void TryPunchServer(Vector3 _)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.2f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Bomb"))
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;

                dir = GetAxisDir(dir);

                hit.GetComponent<Bomb>()?.Punch(dir);
            }
        }
    }
    Vector3 GetAxisDir(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
            return new Vector3(Mathf.Sign(dir.x), 0, 0);

        return new Vector3(0, 0, Mathf.Sign(dir.z));
    }

}
