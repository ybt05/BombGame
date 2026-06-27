using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Collections;
using System.Linq;


public class LobbyManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject selectPanel;
    public GameObject hostPanel1;
    public GameObject hostPanel2;
    public GameObject clientPanel1;
    public GameObject clientPanel2;

    [Header("Input")]
    public TMP_InputField hostNameInput;
    public TMP_InputField clientNameInput;
    public TMP_InputField roomCodeInput;

    [Header("Lobby UI")]
    public TextMeshProUGUI hostRoomCodeText;
    public TextMeshProUGUI playerListText1;
    public TextMeshProUGUI playerListText2;


    [Header("Settings")]
    public Toggle cpuToggle;

    private string myName;
    private string roomCode;
    public RelayManager relayManager;
    private Dictionary<ulong, string> players = new Dictionary<ulong, string>();

    // =========================


    void Start()
    {
        ShowSelect();

        // ✅ 追加
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }


    // =========================
    void ShowSelect()
    {
        selectPanel.SetActive(true);
        hostPanel1.SetActive(false);
        hostPanel2.SetActive(false);
        clientPanel1.SetActive(false);
        clientPanel2.SetActive(false);
    }

    // =========================
    // ホスト
    public void OnClickHost()
    {
        SoundManager.Instance.PlaySE(SoundManager.Instance.clickSE);
        selectPanel.SetActive(false);
        hostPanel1.SetActive(true);
    }

    public async void OnClickHostConfirm()
    {
        SoundManager.Instance.PlaySE(SoundManager.Instance.clickSE);
        myName = hostNameInput.text;
        LobbyData.MyName = myName;

        string code = await relayManager.CreateRoom(10);

        roomCode = code;
        hostRoomCodeText.text = "ROOM: " + roomCode;

        hostPanel1.SetActive(false);
        hostPanel2.SetActive(true);
    }




    void StartHost()
    {
        NetworkManager.Singleton.StartHost();

        roomCode = Random.Range(100000, 999999).ToString();

        hostRoomCodeText.text = "ROOM: " + roomCode;


        hostPanel1.SetActive(false);
        hostPanel2.SetActive(true);
    }

    // =========================
    // クライアント
    public void OnClickClient()
    {
        SoundManager.Instance.PlaySE(SoundManager.Instance.clickSE);
        selectPanel.SetActive(false);
        clientPanel1.SetActive(true);
    }

    public async void OnClickJoin()
    {
        SoundManager.Instance.PlaySE(SoundManager.Instance.clickSE);
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClientsList.Count >= 10)
        {
            Debug.Log("満員です");
            return;
        }

        myName = clientNameInput.text;
        LobbyData.MyName = myName;

        string code = roomCodeInput.text;

        await relayManager.JoinRoom(code);

        clientPanel1.SetActive(false);
        clientPanel2.SetActive(true);
    }


    void StartClient(string code)
    {
        // ※今回は簡易（同LAN）
        NetworkManager.Singleton.StartClient();

        clientPanel1.SetActive(false);
        clientPanel2.SetActive(true);
    }

    // =========================
    // CPU ON/OFF
    public bool IsCPUEnabled()
    {
        return cpuToggle != null && cpuToggle.isOn;
    }

    // =========================
    // ゲーム開始（ホストのみ）
    public void OnClickStartGame()
    {
        SoundManager.Instance.PlaySE(SoundManager.Instance.clickSE);
        if (!NetworkManager.Singleton.IsHost) return;

        players.Clear(); // ✅ 追加（初期化）

        LobbyData.CPUEnabled = cpuToggle.isOn; // ✅ 追加

        NetworkManager.Singleton.SceneManager.LoadScene(
            "GameScene",
            LoadSceneMode.Single
        );
    }

    // =========================
    // 解散
    public void OnClickDisband()
    {
        SoundManager.Instance.PlaySE(SoundManager.Instance.clickSE);
        NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene("TitleScene");
    }

    // =========================
    // 抜ける
    public void OnClickLeave()
    {
        SoundManager.Instance.PlaySE(SoundManager.Instance.clickSE);
        NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene("TitleScene");
    }

    public void OnClickBackToTitle()
    {
        SoundManager.Instance.PlaySE(SoundManager.Instance.clickSE);
        SceneManager.LoadScene("TitleScene");
    }



    void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        }
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("TitleScene");
            return;
        }

        // ✅ 他プレイヤー削除
        players.Remove(clientId);

        UpdatePlayerList();
    }
    void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        int currentPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;

        if (currentPlayers >= 10)
        {
            response.Approved = false;
            response.Reason = "Room Full";
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = false;
    }



    void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsClient)
        {
            SendNameServerRpc(LobbyData.MyName);
        }
    }
    [Rpc(SendTo.Server)]
    void SendNameServerRpc(string name, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        if (!players.ContainsKey(sender))
        {
            players[sender] = name;
        }

        UpdatePlayerListClientRpc(players.Values.ToArray());
    }
    [ClientRpc]
    void UpdatePlayerListClientRpc(string[] names)
    {
        string list = "";

        foreach (var n in names)
        {
            list += n + "\n";
        }

        playerListText1.text = list;
        playerListText2.text = list;
    }

    void UpdatePlayerList()
    {
        string list = "";

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                var ph = client.PlayerObject.GetComponent<PlayerHealth>();

                if (ph != null)
                {
                    list += ph.playerName + "\n";
                }
            }
        }

        playerListText1.text = list;
        playerListText2.text = list;
    }


    void AddPlayer(ulong clientId, string name)
    {
        if (!players.ContainsKey(clientId))
        {
            players.Add(clientId, name);
            UpdatePlayerList();
        }
    }

}
