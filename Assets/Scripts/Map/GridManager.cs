using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
public class GridManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject breakablePrefab;
    public GameObject wallPrefab;
    public GameObject beltUpPrefab;
    public GameObject beltDownPrefab;
    public GameObject beltLeftPrefab;
    public GameObject beltRightPrefab;
    public GameObject warpPrefab;
    public GameObject transparentWallPrefab;

    [Header("Map Settings")]
    public Vector2Int mapSize;
    public int[,] map;

    public List<Vector3> spawnPoints = new List<Vector3>();

    void Awake()
    {
        GenerateMapData(); // ✅ 手作りマップ
    }

    void Start()
    {
        if (GameMode.IsSingle)
        {
            StartCoroutine(Generate());
        }
        else if (NetworkManager.Singleton.IsServer)
        {
            StartCoroutine(Generate());
        }
    }

    // ===============================
    // ✅ テキストマップ生成（これが本体）
    // ===============================
    void GenerateMapData()
    {
        string[] mapText =
        {
            "4444444444444444444444444444444444",
            "4444444444444444444444444444444444",
            "4444444444444444444444444444444444",
            "4441004000000000200000400000001444",
            "4440004222444444400000422244444444",
            "4440002000000000200000400002000444",
            "4444444444442224444444400004000444",
            "4440000000000004000000200004000444",
            "4442444422444444440000444444444444",
            "4441000000000000040000000000000444",
            "4444444444422242244422444442244444",
            "4440000000000040000000400000000444",
            "4442444422444244444444410000000444",
            "4440014000048888888620444444442444",
            "4440004000045444444640410004000444",
            "4440000000045422224640400004000444",
            "4442444444445422224640400002000444",
            "4440000000025422224642444444224444",
            "4444444444445422224640000000000444",
            "4440000000045422224642244444442444",
            "4440000000025400004640040000000444",
            "4442444244445440044640040000001444",
            "4441000000045777777744442244444444",
            "4444442422244444444440000020000444",
            "4440000400000004000020000040000444",
            "4440000400000004000040000040044444",
            "4442444404240002000044444440000444",
            "4440000004042444444020000044442444",
            "4444444424040000004040000020000444",
            "4440000004244444424244444444442444",
            "4441000004000000004000000000001444",
            "4444444444444444444444444444444444",
            "4444444444444444444444444444444444",
            "4444444444444444444444444444444444"
        };

        int width = mapText.Length;
        int height = mapText[0].Length;

        map = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                map[x, z] = mapText[x][z] - '0';
            }
        }

        mapSize = new Vector2Int(width, height);
    }

    // ===============================
    // ✅ 実際に生成
    // ===============================
    IEnumerator Generate()
    {
        if (!GameMode.IsSingle && !NetworkManager.Singleton.IsServer)
            yield break;

        spawnPoints.Clear();

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int tile = map[x, z];

                Vector3 groundPos = new Vector3(x, 0, z);

                // =========================
                // ✅ 床
                // =========================
                if (tile != 5 && tile != 6 && tile != 7 && tile != 8 && tile != 9)
                {
                    SpawnNetObject(wallPrefab, groundPos);
                }

                // =========================
                // ✅ ギミック
                // =========================
                switch (tile)
                {
                    case 5:
                        SpawnNetObject(beltRightPrefab, groundPos);
                        break;
                    case 6:
                        SpawnNetObject(beltLeftPrefab, groundPos);
                        break;
                    case 7:
                        SpawnNetObject(beltUpPrefab, groundPos);
                        break;
                    case 8:
                        SpawnNetObject(beltDownPrefab, groundPos);
                        break;
                    case 9:
                        SpawnNetObject(warpPrefab, groundPos);
                        break;
                }

                // =========================
                // ✅ 上段
                // =========================
                Vector3 upperPos = new Vector3(x, 1, z);

                switch (tile)
                {
                    case 1:
                        spawnPoints.Add(groundPos);
                        break;

                    case 2:
                    case 3:
                        SpawnNetObject(breakablePrefab, upperPos);
                        break;

                    case 4:
                        SpawnNetObject(wallPrefab, upperPos);

                        SpawnNetObject(transparentWallPrefab, upperPos + Vector3.up * 1f);
                        SpawnNetObject(transparentWallPrefab, upperPos + Vector3.up * 2f);
                        break;
                }

                // =========================
                // ✅ 軽量化
                // =========================
                if ((x * height + z) % 200 == 0)
                {
                    yield return null;
                }
            }
        }
    }
    void SpawnNetObject(GameObject prefab, Vector3 pos)
    {
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);

        if (!GameMode.IsSingle)
        {
            obj.GetComponent<NetworkObject>().Spawn();
        }
    }

    // ===============================
    // ✅ スポーン取得
    // ===============================
    public Vector3 GetSpawnPoint(int index)
    {
        if (spawnPoints.Count == 0) return Vector3.zero;
        return spawnPoints[index % spawnPoints.Count];
    }
}