using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;
public class TitleManager : MonoBehaviour
{
    public GameObject settingPanel;
    public GameObject mainPanel;

    [Header("Volume")]
    public Slider bgmSlider;
    public Slider seSlider;


    void Start()
    {
        mainPanel.SetActive(true);
        settingPanel.SetActive(false);

        // 保存された音量をロード
        float bgm = PlayerPrefs.GetFloat("BGM", 0.5f);
        float se = PlayerPrefs.GetFloat("SE", 0.5f);

        bgmSlider.value = bgm;
        seSlider.value = se;

        SoundManager.Instance.SetBGMVolume(bgm);
        SoundManager.Instance.SetSEVolume(se);

        // スライダー変更イベント登録
        bgmSlider.onValueChanged.AddListener(OnChangeBGM);
        seSlider.onValueChanged.AddListener(OnChangeSE);
    }

    // ===== 音量変更 =====
    void OnChangeBGM(float value)
    {
        SoundManager.Instance.SetBGMVolume(value);
        PlayerPrefs.SetFloat("BGM", value);
    }

    void OnChangeSE(float value)
    {
        SoundManager.Instance.SetSEVolume(value);
        PlayerPrefs.SetFloat("SE", value);
    }


    // シングルプレイ
    public void OnClickSingle()
    {
        GameMode.IsSingle = true;
        SoundManager.Instance.PlaySE(SoundManager.Instance.clickSE);
        SceneManager.LoadScene("GameScene");
    }

    // マルチプレイ
    public void OnClickMulti()
    {
        SoundManager.Instance.PlaySE(SoundManager.Instance.clickSE);
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
        SoundManager.Instance.PlaySE(SoundManager.Instance.clickSE);
        mainPanel.SetActive(false);
        settingPanel.SetActive(true);

    }

    // 設定閉じる
    public void CloseSetting()
    {
        SoundManager.Instance.PlaySE(SoundManager.Instance.clickSE);
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