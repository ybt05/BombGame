using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
public class TitleManager : MonoBehaviour
{
    public GameObject settingPanel;
    public GameObject mainPanel;
    void Start()
    {
        mainPanel.SetActive(true);
        settingPanel.SetActive(false);
    }

    // シングルプレイ
    public void OnClickSingle()
    {
        GameMode.IsSingle = true;
        SceneManager.LoadScene("GameScene");
    }

    // マルチプレイ
    public void OnClickMulti()
    {

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown(); // ✅ 念のため
        }

        GameMode.IsSingle = false;
        SceneManager.LoadScene("LobbyScene");
    }

    // 設定
    public void OnClickSetting()
    {
        mainPanel.SetActive(false);
        settingPanel.SetActive(true);

    }

    // 設定閉じる
    public void CloseSetting()
    {
        mainPanel.SetActive(true);
        settingPanel.SetActive(false);
    }

    // ゲーム終了
    public void OnClickExit()
    {
        Debug.Log("Game Exit");
        Application.Quit();
    }
    void ShowMain()
    {
        mainPanel.SetActive(true);
        settingPanel.SetActive(false);
    }
}