using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
public class PlayerColor : NetworkBehaviour
{
    public Renderer rend;
    public NetworkVariable<Color> netColor = new NetworkVariable<Color>();

    // ✅ 全プレイヤー共通で使う色リスト
    private static List<Color> colorPool = new List<Color>()
    {
        Color.red,
        Color.blue,
        Color.yellow,
        Color.green,
        new Color(0.5f, 0f, 0.5f), // 紫
        Color.cyan,
        Color.magenta,             // ピンク
        new Color(0.5f, 1f, 0f),   // 黄緑
        new Color(1f, 0.5f, 0f),   // オレンジ
        Color.white
    };

    void Start()
    {
        if (GameMode.IsSingle)
        {
            ApplyColor(GetRandomColor());
        }
    }
    public override void OnNetworkSpawn()
    {
        if (GameMode.IsSingle)
        {
            return;
        }

        if (IsServer && netColor.Value == default)
        {
            netColor.Value = GetRandomColor();
        }

        ApplyColor(netColor.Value);

        netColor.OnValueChanged += (oldC, newC) =>
        {
            ApplyColor(newC);
        };
    }
    void ApplyColor(Color color)
    {
        if (rend == null)
        {
            rend = GetComponentInChildren<Renderer>();
            if (rend == null) return;
        }
        rend.material.color = color;
    }

    static Color GetRandomColor()
    {
        if (colorPool.Count == 0)
        {
            // 万が一足りなかったらランダム（保険）
            return Random.ColorHSV();
        }

        int index = Random.Range(0, colorPool.Count);

        Color c = colorPool[index];
        colorPool.RemoveAt(index); // ✅ 使った色を削除

        return c;
    }
}