using UnityEngine;
using Unity.Netcode;
public class Belt : MonoBehaviour
{
    public Vector3 moveDirection;
    public float speed = 3f;

    void OnTriggerStay(Collider other)
    {
        if (!GameMode.IsSingle && !NetworkManager.Singleton.IsServer) return;

        if (other.CompareTag("Player") || other.CompareTag("Bomb"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            Rigidbody rb = other.GetComponent<Rigidbody>();

            if (pc != null)
            {
                pc.externalVelocity = moveDirection * speed;
            }
            else if (rb != null)
            {
                if (GameMode.IsSingle || NetworkManager.Singleton.IsServer)
                {
                    rb.linearVelocity = new Vector3(
                        moveDirection.x * speed,
                        rb.linearVelocity.y,
                        moveDirection.z * speed
                    );
                }
            }
        }
    }

}