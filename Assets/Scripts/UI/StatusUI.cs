using UnityEngine;
using TMPro;
using Unity.Netcode;

public class StatusUI : MonoBehaviour
{
    private PlayerStats playerStats;
    public TextMeshProUGUI text;

    void Update()
    {
        if (playerStats == null)
        {
            foreach (var p in FindObjectsByType<PlayerController>())
            {
                // ✅ シングルはそのまま取得
                if (GameMode.IsSingle)
                {
                    playerStats = p.GetComponent<PlayerStats>();
                    break;
                }

                // ✅ マルチだけOwnerチェック
                if (p.IsOwner)
                {
                    playerStats = p.GetComponent<PlayerStats>();
                    break;
                }
            }
        }

        if (playerStats == null) return;

        text.text =
            "Bomb: " + playerStats.bombCount.Value + "\n" +
            "Power: " + playerStats.power.Value + "\n" +
            "Speed: " + playerStats.speed.Value + "\n" +
            "Kick: " + (playerStats.canKick.Value ? "ON" : "OFF") + "\n" +
            "Punch: " + (playerStats.canPunch.Value ? "ON" : "OFF") + "\n" +
            "Jump: " + (playerStats.canJump.Value ? "ON" : "OFF") + "\n" +
            "Remote: " + playerStats.remoteBomb.Value + "\n" +
            "Pierce: " + playerStats.pierceBomb.Value + "\n" +
            "CurrentBomb: " + (BombType)playerStats.currentBombType.Value;
    }

}