using UnityEngine;
using Unity.Netcode;
public class SimpleAI : NetworkBehaviour
{
    public float moveSpeed = 3f;
    public float decisionInterval = 0.5f;

    private Rigidbody rb;
    private Vector3 moveDir;
    private float timer;

    private Transform target;
    private float lastBombTime = 0f;
    public float bombInterval = 2f;
    public bool canAct = false;



    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        PickRandomDirection();
    }


    void Update()
    {
        if (!GameMode.IsSingle && !IsServer) return;
        if (!canAct) return;
        if (!GameManager.Instance) return;
        if (!GameManager.Instance.IsPlaying) return;

        timer += Time.deltaTime;

        if (timer >= decisionInterval)
        {
            timer = 0;
            Think();
        }
    }


    void FixedUpdate()
    {
        if (!GameMode.IsSingle && !IsServer) return; // ✅ 追加
        if (!canAct)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 velocity = moveDir * moveSpeed;

        rb.linearVelocity = new Vector3(
            velocity.x,
            rb.linearVelocity.y, // ✅ これに変更！！
            velocity.z
        );
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Wall") || col.gameObject.CompareTag("Breakable"))
        {
            PickRandomDirection();
        }
    }

    void Think()
    {
        // ① 危険回避
        if (IsDanger())
        {
            Escape();
            return;
        }

        // ② プレイヤー探す
        FindNearestPlayer();

        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.position);

            // ③ 近いなら爆弾
            if (dist < 2f && Time.time - lastBombTime > bombInterval)
            {
                lastBombTime = Time.time;
                PlaceBomb();
                Escape(); // ✅ 追加
                return;   // ✅ これ重要
            }

            // ④ 追いかける

            Vector3 dir = (target.position - transform.position).normalized;

            // 壁チェック
            RaycastHit hit;
            if (Physics.Raycast(transform.position, dir, out hit, 1f))
            {
                if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Breakable"))
                {
                    PickRandomDirection();
                    return;
                }
            }
            moveDir = dir;
        }
        else
        {
            // ⑤ ランダム
            PickRandomDirection();
        }
    }

    // =========================

    void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        float minDist = 999f;
        target = null;

        foreach (var p in players)
        {
            if (p == gameObject) continue;

            float dist = Vector3.Distance(transform.position, p.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                target = p.transform;
            }
        }
    }

    // =========================

    void PickRandomDirection()
    {
        moveDir = new Vector3(
            Random.Range(-1, 2),
            0,
            Random.Range(-1, 2)
        ).normalized;
    }

    // =========================

    void PlaceBomb()
    {
        if (!GameMode.IsSingle && !IsServer) return;

        BombSpawner spawner = GetComponent<BombSpawner>();

        if (spawner != null)
        {
            spawner.PlaceBombLocalAI(); // ✅ 新しく作る
        }
    }

    // =========================

    bool IsDanger()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 3f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Bomb"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);

                if (dist < 2.5f) // 爆発範囲っぽく
                    return true;
            }
        }

        return false;
    }


    void Escape()
    {
        Vector3[] dirs = new Vector3[]
        {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
        };

        float maxDist = -1f;
        Vector3 bestDir = Vector3.zero;

        foreach (var dir in dirs)
        {
            Vector3 checkPos = transform.position + dir * 1f;

            // 壁チェック
            Collider[] hits = Physics.OverlapBox(checkPos, new Vector3(0.4f, 0.5f, 0.4f));

            bool blocked = false;

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Wall") || hit.CompareTag("Breakable"))
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked) continue;

            // 一番ボムから遠くなる方向を選ぶ
            float dist = 0f;

            Collider[] bombs = Physics.OverlapSphere(transform.position, 3f);

            foreach (var b in bombs)
            {
                if (b.CompareTag("Bomb"))
                {
                    dist = Vector3.Distance(transform.position + dir, b.transform.position);
                    break;
                }
            }

            if (dist > maxDist)
            {
                maxDist = dist;
                bestDir = dir;
            }
        }

        if (bestDir != Vector3.zero)
        {
            moveDir = bestDir;
            return;
        }

        // fallback
        PickRandomDirection();
    }

}
