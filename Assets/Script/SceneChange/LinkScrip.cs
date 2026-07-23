using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class LinkScrip : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.rKey.isPressed)
        {
            SceneManager.LoadScene(0);
        }

        if (Keyboard.current.tKey.isPressed)
        {
            SceneManager.LoadScene(4);
        }
    }
}
