using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class BGMManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        if (FindObjectsOfType<BGMManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        //Scene currentScene = SceneManager.GetActiveScene();
        //string sceneName = currentScene.name;
        //if (sceneName == "StartScene")
        //{
        //    Destroy(gameObject);
        //}
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;
        if (sceneName == "StartScene")
        {
            Destroy(gameObject);
        }
    }
}
