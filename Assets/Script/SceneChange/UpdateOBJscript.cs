using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
public class UpdateOBJscript : MonoBehaviour
{
    public TMP_Text TheTime;
    // Start is called before the first frame update
    void Start()
    {
        TheTime.text = StaticTime.Boss2Time.ToString("0.000");
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.rKey.isPressed)
        {
            SceneManager.LoadScene(0);
        }
    }
}
