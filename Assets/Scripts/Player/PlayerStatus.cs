using UnityEngine;
using Unity.Netcode;

public class PlayerStats : NetworkBehaviour
{
    public NetworkVariable<int> bombCount = new NetworkVariable<int>(1);
    public NetworkVariable<int> power = new NetworkVariable<int>(1);
    public NetworkVariable<int> speed = new NetworkVariable<int>(1);

    public NetworkVariable<bool> canKick = new NetworkVariable<bool>();
    public NetworkVariable<bool> canPunch = new NetworkVariable<bool>();
    public NetworkVariable<bool> canJump = new NetworkVariable<bool>();

    public NetworkVariable<int> remoteBomb = new NetworkVariable<int>();
    public NetworkVariable<int> pierceBomb = new NetworkVariable<int>();

    public NetworkVariable<int> currentBombType = new NetworkVariable<int>((int)BombType.Normal);
}
