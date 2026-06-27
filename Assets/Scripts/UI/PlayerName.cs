using TMPro;
using UnityEngine;

public class PlayerName : MonoBehaviour
{
    public TextMeshPro nameText;

    void Start()
    {
        if (nameText == null)
        {
            nameText = GetComponent<TextMeshPro>();
        }

        PlayerHealth ph = GetComponentInParent<PlayerHealth>();

        if (ph != null && nameText != null)
        {
            nameText.text = ph.playerName;
        }
    }


    string lastName = "";

    void LateUpdate()
    {
        if (Camera.main == null) return;

        PlayerHealth ph = GetComponentInParent<PlayerHealth>();
        if (ph != null && nameText != null)
        {
            if (lastName != ph.playerName)
            {
                nameText.text = ph.playerName;
                lastName = ph.playerName;
            }
        }

        Vector3 dir = transform.position - Camera.main.transform.position;
        dir.y = 0f;

        transform.rotation = Quaternion.LookRotation(dir);
    }

}
