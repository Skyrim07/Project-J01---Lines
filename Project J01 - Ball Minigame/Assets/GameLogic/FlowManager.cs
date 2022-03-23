using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SKCell;
public class FlowManager : MonoSingleton<FlowManager>
{
    [HideInInspector]
    public int score = 0;
    public int time =0;
    [HideInInspector]
    public bool gameOver = false;
    [HideInInspector]
    public int bestScore = 0, bestTime = int.MaxValue;
    private void Start()
    {
        LoadPlayerPrefs();
        CommonReference.instance.gameOver.SetActive(false);
        CommonUtils.InvokeActionUnlimited(0, () =>
        {
            if (gameOver)
                return;

            time += 1;
            CommonReference.instance.timeText.text = (Mathf.FloorToInt(time)/60).ToString("d2")+":" + (Mathf.FloorToInt(time) % 60).ToString("d2");
        }, 1, "Timer");
    }
    public void ChangeScore(int delta)
    {
        SetScore(score + delta);
    }
    public void SetScore(int targetScore)
    {
        int oscore = score;
        score = targetScore;
        CommonUtils.StartProcedure(SKCurve.QuadraticIn, 0.5f, (f) =>
         {
             CommonReference.instance.scoreText.text = ((int)((oscore + (targetScore - oscore) * f))).ToString("d6");
         }, null, "Score");
    }

    private void SavePlayerPrefs()
    {
        PlayerPrefs.SetInt("BestScore", score>bestScore?score:bestScore);
        PlayerPrefs.SetInt("BestTime", time < bestTime ? time : bestTime);
    }
    private void LoadPlayerPrefs()
    {
        bestScore=PlayerPrefs.HasKey("BestScore")? PlayerPrefs.GetInt("BestScore"):0;
        bestTime= PlayerPrefs.HasKey("BestTime") ? PlayerPrefs.GetInt("BestTime"):int.MaxValue;
    }
    public void GameOver()
    {
        gameOver=true;
        CommonReference.instance.gameOver.SetActive(true);
        SavePlayerPrefs();
    }
    private void Update()
    {
        if (gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R)){
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            }
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Application.Quit();
        }
    }
}
