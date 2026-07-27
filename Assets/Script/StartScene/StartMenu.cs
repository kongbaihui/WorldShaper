using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private string GameScene1 = "SampleScene";
    [SerializeField] private string GameScene2 = "BossScene";

    [SerializeField] private GameObject guidePanel;
    [SerializeField] private GameObject creditPanel;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject namePanel;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Text nameErrorText;
    [SerializeField] private GameObject loginButton;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private GameObject logoutButton;

    private string pendingGameScene;

    // Start is called before the first frame update
    void Start()
    {
        //隐藏游戏说明面板
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }

        if(creditPanel != null)
        {
            creditPanel.SetActive(false);
        }

        if(startPanel != null)
        {
            startPanel.SetActive(false);
        }

        if (namePanel != null)
        {
            namePanel.SetActive(false);
        }

        if (nameErrorText != null)
        {
            nameErrorText.text = string.Empty;
        }

        RefreshLoginDisplay();
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
    public void StartGame1()
    {
        StartGame(GameScene1);
    }

    public void StartGame2()
    {
        StartGame(GameScene2);
    }

    public void ConfirmPlayerName()
    {
        if (nameInput == null)
        {
            SetNameError("Name input is not configured.");
            Debug.LogWarning("StartMenu nameInput is not assigned.");
            return;
        }

        if (!TryNormalizePlayerName(nameInput.text, out string playerName))
        {
            SetNameError("Name must be 1-20 characters without line breaks or tabs.");
            return;
        }

        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();
        SetNameError(string.Empty);
        RefreshLoginDisplay();

        if (namePanel != null)
        {
            namePanel.SetActive(false);
        }

        if (!string.IsNullOrEmpty(pendingGameScene))
        {
            string sceneToLoad = pendingGameScene;
            pendingGameScene = string.Empty;
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    public void OpenLogin()
    {
        pendingGameScene = string.Empty;
        SetNameError(string.Empty);

        if (namePanel != null)
        {
            namePanel.SetActive(true);
        }

        if (nameInput != null)
        {
            nameInput.text = PlayerPrefs.GetString("PlayerName", string.Empty).Trim();
            nameInput.ActivateInputField();
        }
    }

    public void LogoutPlayer()
    {
        pendingGameScene = string.Empty;
        PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.Save();

        if (nameInput != null)
        {
            nameInput.text = string.Empty;
        }

        if (namePanel != null)
        {
            namePanel.SetActive(false);
        }

        SetNameError(string.Empty);
        RefreshLoginDisplay();
    }

    public void StartTutorial()
    {
        SceneManager.LoadScene("TutorialScene");
    }

    public void OpenStart()
    {
        if(startPanel != null)
        {
            startPanel.SetActive(true);
        }
    }

    public void CloseStart()
    {
        if(startPanel != null)
        {
            startPanel.SetActive(false);
        }
    }

    // 游戏说明按钮调用
    public void OpenGuide()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(true);

            KeyRebindButton[] keyButtons =
                guidePanel.GetComponentsInChildren<KeyRebindButton>(true);

            foreach (KeyRebindButton keyButton in keyButtons)
            {
                keyButton.RefreshText();
            }
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

    private void StartGame(string sceneName)
    {
        string savedName = PlayerPrefs.GetString("PlayerName", string.Empty);
        if (TryNormalizePlayerName(savedName, out string playerName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        pendingGameScene = sceneName;
        SetNameError(string.Empty);

        if (nameInput != null)
        {
            nameInput.text = playerName;
        }

        if (namePanel != null)
        {
            namePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("StartMenu namePanel is not assigned.");
        }
    }

    private void SetNameError(string message)
    {
        if (nameErrorText != null)
        {
            nameErrorText.text = message;
        }
    }

    private void RefreshLoginDisplay()
    {
        bool isLoggedIn = TryNormalizePlayerName(
            PlayerPrefs.GetString("PlayerName", string.Empty),
            out string playerName);

        if (loginButton != null)
        {
            loginButton.SetActive(!isLoggedIn);
        }

        if (playerNameText != null)
        {
            playerNameText.text = isLoggedIn
                ? playerName.Replace("<", "＜").Replace(">", "＞")
                : string.Empty;
            playerNameText.gameObject.SetActive(isLoggedIn);
        }

        if (logoutButton != null)
        {
            logoutButton.SetActive(isLoggedIn);
        }
    }

    private static bool TryNormalizePlayerName(string rawName, out string playerName)
    {
        rawName = rawName ?? string.Empty;
        playerName = rawName.Trim();
        return playerName.Length >= 1 &&
               playerName.Length <= 20 &&
               rawName.IndexOfAny(new[] { '\r', '\n', '\t' }) < 0;
    }
}
