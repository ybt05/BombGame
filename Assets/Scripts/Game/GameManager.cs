using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.SceneManagement;


public struct ScoreData : INetworkSerializable, System.IEquatable<ScoreData>
{
    public FixedString64Bytes name;
    public int score;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref score);
    }

    // ✅ これ追加（超重要）
    public bool Equals(ScoreData other)
    {
        return name.Equals(other.name) && score == other.score;
    }
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Time")]
    public float matchTime = 180f;
    private float timer;
    private bool isPlaying = false;
    public bool IsPlaying => isPlaying;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public GameObject playerPrefab;
    public TextMeshProUGUI scoreText;
    public KillLogManager killLog;
    public GridManager grid;

    public NetworkList<ScoreData> scores = new NetworkList<ScoreData>();


    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        resultPanel.SetActive(false);
    }
    public void InitScores()
    {
        if (!GameMode.IsSingle && !IsServer) return;

        scores.Clear();

        var allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude);

        HashSet<string> added = new HashSet<string>();

        foreach (var p in allPlayers)
        {
            if (added.Contains(p.playerName)) continue;

            added.Add(p.playerName);

            scores.Add(new ScoreData
            {
                name = p.playerName,
                score = 0
            });
        }

        UpdateScoreUI();
    }

    System.Collections.IEnumerator StartCountdown()
    {
        isPlaying = false;

        Debug.Log("3");
        yield return new WaitForSeconds(1);
        Debug.Log("2");
        yield return new WaitForSeconds(1);
        Debug.Log("1");
        yield return new WaitForSeconds(1);

        StartGame();
    }

    void StartGame()
    {
        timer = matchTime;
        isPlaying = true;
        InitScores();
    }

    void Update()
    {
        if (isPlaying)
        {
            timer -= Time.deltaTime;
        }

        UpdateTimerUI();

        if (isPlaying && timer <= 0)
        {
            if (GameMode.IsSingle)
            {
                EndGame();
            }
            else if (IsServer)
            {
                EndGameClientRpc();
            }
        }
    }
    [ClientRpc]
    void EndGameClientRpc()
    {
        EndGame();
    }

    void UpdateTimerUI()
    {
        int min = Mathf.FloorToInt(timer / 60);
        int sec = Mathf.FloorToInt(timer % 60);

        timerText.text = $"{min:00}:{sec:00}";
    }
    void UpdateScoreUI()
    {
        string text = "";

        foreach (var s in scores)
        {
            text += $"{s.name} : {s.score}\n";
        }

        scoreText.text = text;
    }


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            scores.Clear();
        }
        scores.OnListChanged += OnScoresChanged;

        UpdateScoreUI(); // ✅ 初期表示も忘れず
    }
    void OnScoresChanged(NetworkListEvent<ScoreData> changeEvent)
    {
        UpdateScoreUI();
    }
    void AddKillLocal(string killer)
    {
        for (int i = 0; i < scores.Count; i++)
        {
            if (scores[i].name.ToString() == killer)
            {
                ScoreData data = scores[i];
                data.score++;
                scores[i] = data;
                return;
            }
        }

        scores.Add(new ScoreData
        {
            name = killer,
            score = 1
        });

        UpdateScoreUI();
    }
    public void AddKill(string killer, string victim)
    {
        if (!IsPlaying) return;
        bool isSuicide = (killer == victim);

        // ✅ シングルは普通の処理
        if (GameMode.IsSingle)
        {

            if (!isSuicide)
                AddKillLocal(killer);
            killLog?.AddLog(killer, victim);
            return;
        }

        if (!IsServer) return;

        if (!isSuicide)
        {
            bool found = false;
            for (int i = 0; i < scores.Count; i++)
            {
                if (scores[i].name.ToString() == killer)
                {
                    ScoreData data = scores[i];
                    data.score++;
                    scores[i] = data;

                    found = true;
                    break;
                }
            }

            if (!found)
            {
                scores.Add(new ScoreData
                {
                    name = killer,
                    score = 1
                });
            }
        }

        if (killLog != null)
        {
            AddKillLogClientRpc(killer, victim);
        }
    }


    void EndGame()
    {
        isPlaying = false;

        ShowResult();
    }

    void ShowResult()
    {
        resultPanel.SetActive(true);

        List<ScoreData> list = new List<ScoreData>();

        foreach (var s in scores)
        {
            list.Add(s);
        }

        var ranking = list.OrderByDescending(x => x.score);

        string text = "";
        int rank = 1;

        foreach (var entry in ranking)
        {
            if (rank > 5) break; // ←★これ追加！

            text += $"{rank} {entry.name} : {entry.score}p\n";
            rank++;
        }

        resultText.text = text;
    }

    public void OnClickOK()
    {
        if (GameMode.IsSingle)
        {
            SceneManager.LoadScene("TitleScene");
        }
        else
        {
            SceneManager.LoadScene("LobbyScene");
        }
    }

    public void SpawnCPU(int count)
    {
        if (!IsServer) return; // ✅ サーバーだけ

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = grid.GetSpawnPoint(i + 1);

            GameObject cpu = Instantiate(playerPrefab, pos, Quaternion.identity);

            cpu.name = "CPU" + i;

            // Player操作削除
            Destroy(cpu.GetComponent<PlayerController>());

            // AI追加
            if (cpu.GetComponent<SimpleAI>() == null)
            {
                cpu.AddComponent<SimpleAI>();
            }

            // 名前設定
            cpu.GetComponent<PlayerHealth>().playerName = cpu.name;

            // ✅ 最重要！！
            cpu.GetComponent<NetworkObject>().Spawn(true);
        }
        InitScores();
    }

    public void StartGameFromFlow()
    {
        timer = matchTime;
        isPlaying = true;

        if (GameMode.IsSingle || IsServer)
        {
            InitScores(); // ✅ 必須
        }
    }

    [ClientRpc]
    void AddKillLogClientRpc(string killer, string victim)
    {
        killLog?.AddLog(killer, victim);
    }

    public void AddDeath(string player)
    {
        if (!IsPlaying) return;
        if (!GameMode.IsSingle && !IsServer) return;

        for (int i = 0; i < scores.Count; i++)
        {
            if (scores[i].name.ToString() == player)
            {
                ScoreData data = scores[i];
                data.score--;
                scores[i] = data;
                UpdateScoreUI();
                return;
            }
        }
    }



}
