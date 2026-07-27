using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class StopGame : MonoBehaviour
{
    public Button RETRY;
    public Button RETURN;
    public bool isPause = false;
    public GameObject PausePanel;
    void Start()
    {
        PausePanel.SetActive(false);
        RETRY.onClick.AddListener(RetryGame);
        RETURN.onClick.AddListener(ReturnGame);
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ChangeTimeScale();
        }
    }

    private void ChangeTimeScale()
    {
        isPause = !isPause;
        if (isPause)
        {
            Time.timeScale = 0;
            PausePanel.SetActive(true);
        }
        else
        {
            Time.timeScale = 1;
            PausePanel.SetActive(false);
        }
    }

    public void RetryGame()
    {
        int curSceneIdx = SceneManager.GetActiveScene().buildIndex;
        if (curSceneIdx == 3)
        {
            SceneManager.LoadScene(3);
        }
        else
        {
            SceneManager.LoadScene(4);
        }
        Time.timeScale = 1;
        PausePanel.SetActive(false);
    }

    public void ReturnGame()
    {
        SceneManager.LoadScene(0);
        Time.timeScale = 1;
        PausePanel.SetActive(false);
    }
}
