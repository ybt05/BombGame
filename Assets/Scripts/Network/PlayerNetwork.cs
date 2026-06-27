using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections;
using System.Collections;

public class PlayerNetwork : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> playerName =
        new NetworkVariable<FixedString64Bytes>();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SetNameServerRpc(LobbyData.MyName);
            StartCoroutine(SetCamera());
        }

        playerName.OnValueChanged += OnNameChanged;

        // ✅ 初期反映（重要）
        UpdateNameUI(playerName.Value.ToString());
    }
    IEnumerator SetCamera()
    {
        CameraFollow cam = null;

        while (cam == null)
        {
            if (Camera.main != null)
            {
                cam = Camera.main.GetComponent<CameraFollow>();
            }

            yield return null;
        }

        cam.target = transform;
    }
    void OnNameChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
    {
        UpdateNameUI(newName.ToString());
    }

    void UpdateNameUI(string name)
    {
        PlayerHealth ph = GetComponent<PlayerHealth>();

        if (ph != null)
        {
            ph.playerName = name;
        }
    }


    [ServerRpc]
    void SetNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return; // ✅ 追加

        playerName.Value = name;
    }

}
