using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private string GameScene = "GameScene";

    [SerializeField] private GameObject guidePanel;
    [SerializeField] private GameObject creditPanel;
    // Start is called before the first frame update
    void Start()
    {
        //隐藏游戏说明面板
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape) && guidePanel != null && guidePanel.activeSelf)
        {
            CloseGuide();
        }
    }

    //开始游戏按钮
    public void StartGame()
    {
        SceneManager.LoadScene(GameScene);
    }

    // 游戏说明按钮调用
    public void OpenGuide()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(true);
        }
    }

    // 说明界面的返回按钮调用
    public void CloseGuide()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }
    }

    //致谢界面按钮调用
    public void OpenCredit()
    {
        if (creditPanel != null)
        {
            creditPanel.SetActive(true);
        }
    }

    // 致谢界面的返回按钮调用
    public void CloseCredit()
    {
        if (creditPanel != null)
        {
            creditPanel.SetActive(false);
        }
    }

    // 退出游戏按钮调用
    public void QuitGame()
    {
        Debug.Log("退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
