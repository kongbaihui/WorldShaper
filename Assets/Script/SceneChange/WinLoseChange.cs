using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinLoseChange
{

    //true to win, false to lose
    public static void JumpToEnd(bool IsWin)
    {
        if (IsWin)
        {
            SceneManager.LoadScene(1);
        }
        else
        {
            SceneManager.LoadScene(2);
        }
    }
}
