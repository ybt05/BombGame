using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class KillLogManager : MonoBehaviour
{
    public TextMeshProUGUI logText;

    private List<string> logs = new List<string>();

    public void AddLog(string killer, string victim)
    {
        string msg = $"{killer} → {victim}";

        logs.Add(msg);

        if (logs.Count > 5)
            logs.RemoveAt(0);

        UpdateLog();

        StartCoroutine(RemoveLogAfterTime(msg)); // ←変更
    }

    IEnumerator RemoveLogAfterTime(string msg)
    {
        yield return new WaitForSeconds(5f);

        if (logs.Contains(msg))
        {
            logs.Remove(msg);
            UpdateLog();
        }
    }

    void UpdateLog()
    {
        logText.text = string.Join("\n", logs);
    }

}