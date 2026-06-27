using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TMPro;

public class GameFlowManager : NetworkBehaviour
{
    public GridManager grid;
    public GameStarter starter;

    public TextMeshProUGUI countdownText;

    bool started = false;

    void Start()
    {
        if (GameMode.IsSingle && !started)
        {
            started = true;
            StartCoroutine(GameFlow());
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!GameMode.IsSingle && IsServer && !started)
        {
            started = true;
            StartCoroutine(GameFlow());
        }
    }


    IEnumerator GameFlow()
    {
        SoundManager.Instance.PlayBGM();
        yield return new WaitForSeconds(0.5f);

        SpawnAllPlayers();

        SpawnCPUIfNeeded();

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(Countdown());

        StartGame();
    }

    void StartGame()
    {
        if (GameMode.IsSingle)
        {
            EnableAllAI();
            GameManager.Instance.StartGameFromFlow();
        }
        else
        {
            if (IsServer) // ✅ 追加
            {
                EnableAllAI(); // ✅ サーバーでCPU有効化
                GameManager.Instance.StartGameFromFlow(); // ✅ サーバーで開始
            }

            StartGameClientRpc(); // ✅ クライアントにも通知
        }
    }
    void EnableAllAI()
    {
        if (!GameMode.IsSingle && !IsServer) return; // ✅ 追加

        SimpleAI[] ais = FindObjectsByType<SimpleAI>();

        foreach (var ai in ais)
        {
            ai.canAct = true;
        }
    }

    void SpawnCPUIfNeeded()
    {
        if (GameMode.IsSingle) return;
        if (!IsServer) return;

        int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;

        LobbyManager lobby = FindAnyObjectByType<LobbyManager>();
        if (lobby != null && !lobby.IsCPUEnabled()) return;

        int cpuCount = 10 - playerCount;

        if (cpuCount <= 0) return;


        if (LobbyData.CPUEnabled)
        {
            GameManager.Instance.SpawnCPU(cpuCount);
        }
        
    }



    void SpawnAllPlayers()
    {
        if (GameMode.IsSingle)
        {
            // ✅ シングル：自分1人だけ
            SpawnSinglePlayer();
        }
        else
        {
            // ✅ マルチ
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                starter.SpawnPlayer(client.ClientId);
            }
        }
    }
    void SpawnSinglePlayer()
    {
        // ✅ 自分プレイヤー
        Vector3 pos = grid.GetSpawnPoint(0);
        pos.y = 1.5f;

        GameObject player = Instantiate(starter.playerPrefab, pos, Quaternion.identity);
        player.GetComponent<PlayerHealth>().playerName = "Player";

        // ✅ カメラ追従
        CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
        if (cam != null)
        {
            cam.target = player.transform;
        }

        // ✅ CPU生成（残り9体）
        for (int i = 1; i < 10; i++)
        {
            Vector3 cpuPos = grid.GetSpawnPoint(i);
            cpuPos.y = 1.5f;

            GameObject cpu = Instantiate(starter.playerPrefab, cpuPos, Quaternion.identity);

            cpu.name = "CPU" + i;
            cpu.GetComponent<Rigidbody>().freezeRotation = true;

            // Player操作を削除
            Destroy(cpu.GetComponent<PlayerController>());

            // AI追加
            if (cpu.GetComponent<SimpleAI>() == null)
            {
                cpu.AddComponent<SimpleAI>();
            }

            cpu.GetComponent<PlayerHealth>().playerName = cpu.name;
        }
        GameManager.Instance.InitScores();
    }

    void ShowCountdown(string text)
    {
        if (GameMode.IsSingle)
        {
            // ✅ シングル用
            if (countdownText != null)
                countdownText.text = text;
        }
        else
        {
            // ✅ マルチ用
            CountdownClientRpc(text);
        }
    }

    IEnumerator Countdown()
    {
        ShowCountdown("3");
        yield return new WaitForSeconds(1f);

        ShowCountdown("2");
        yield return new WaitForSeconds(1f);

        ShowCountdown("1");
        yield return new WaitForSeconds(1f);

        ShowCountdown("GO!");
        yield return new WaitForSeconds(1f);

        ShowCountdown("");
    }


    [ClientRpc]
    void CountdownClientRpc(string text)
    {
        if (countdownText != null)
        {
            countdownText.text = text;
        }
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        if (IsServer) // ✅ 追加
        {
            EnableAllAI();
        }

        GameManager.Instance.StartGameFromFlow();
    }
}