using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine.SceneManagement;
public class TimeCount : MonoBehaviour
{
    public TMP_Text TheTmpText = null;
    public float CountTime = 0;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        CountTime += Time.deltaTime;
        TheTmpText.text = TimeFormatter(CountTime);
        int curSceneIdx = SceneManager.GetActiveScene().buildIndex;
        if (curSceneIdx == 3)
        {
            StaticTime.Boss1Time = CountTime;
        }
        else if (curSceneIdx == 4)
        {
            StaticTime.Boss2Time = CountTime;
        }
    }
    private string TimeFormatter(float CountTime)
    {
        int min = (int)CountTime / 60;
        int sec = (int)(CountTime - min * 60);
        int msec = (int)((CountTime - min * 60 - sec) * 100);
        return min.ToString("D2") + ":" + sec.ToString("D2") + "." + msec.ToString("D2");
    }

    public int GetMilliseconds()
    {
        return Mathf.Max(0, (int)(CountTime * 1000f));
    }

    public static string FormatMilliseconds(int milliseconds)
    {
        int safeMilliseconds = Mathf.Max(0, milliseconds);
        int min = safeMilliseconds / 60000;
        int sec = safeMilliseconds / 1000 % 60;
        int msec = safeMilliseconds % 1000;
        return min.ToString("D2") + ":" + sec.ToString("D2") + "." + msec.ToString("D3");
    }

    public void RestartTime()
    {
        CountTime = 0;
    }
}
