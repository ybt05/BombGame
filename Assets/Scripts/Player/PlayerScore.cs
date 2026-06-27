using Unity.Netcode;

public class PlayerScore : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>();

    public void AddScore()
    {
        if (IsServer)
        {
            score.Value++;
        }
    }
}