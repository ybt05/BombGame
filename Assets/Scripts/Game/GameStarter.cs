using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class GameStarter : NetworkBehaviour
{
    public GameObject playerPrefab;
    public GridManager grid;

    public void SpawnPlayer(ulong clientId)
    {
        var sortedClients = NetworkManager.Singleton.ConnectedClientsList
            .OrderBy(c => c.ClientId)
            .ToList();

        int index = sortedClients.FindIndex(c => c.ClientId == clientId);

        if (grid.spawnPoints.Count == 0)
        {
            Debug.LogError("SpawnPointsが空です");
            return;
        }

        Vector3 pos = grid.GetSpawnPoint(index % grid.spawnPoints.Count);
        pos = new Vector3(pos.x, 1.5f, pos.z);

        GameObject player = Instantiate(playerPrefab, pos, Quaternion.identity);

        player.name = "Player_" + clientId;


        NetworkObject netObj = player.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId, true);

            if (netObj.IsOwner) // ✅ 追加
            {
                CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                if (cam != null)
                {
                    cam.target = player.transform;
                }
            }
        }

    }

}