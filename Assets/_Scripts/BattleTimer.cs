using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleTimer : MonoBehaviour
{
    TextMeshProUGUI timerText;
    public float battleTime; 
    int minutes;
    int seconds;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        timerText = this.GetComponentInChildren<TextMeshProUGUI>();
        StartTimer();
    }
    
    public void StartTimer()
    {
        battleTime = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        battleTime += Time.deltaTime;
        minutes = Mathf.FloorToInt(battleTime / 60f);
        seconds = Mathf.FloorToInt(battleTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
