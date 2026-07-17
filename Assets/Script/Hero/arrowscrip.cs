using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class arrowscrip : MonoBehaviour
{
    public GameObject TheHero;
    private Vector3 MousePosition;
    public float RelativeAdd = 10f;
    // Start is called before the first frame update
    void Start()
    {
        TheHero = GameObject.Find("Hero");
    }

    // Update is called once per frame
    void Update()
    {
        //set position
        MousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        MousePosition.z = 0;
        Vector3 HeroPosition = TheHero.transform.position;
        //set direct vector from hero to mouse
        transform.up = (MousePosition - HeroPosition).normalized;
        //set position to the edge of circle: realpos = heropos + relepos
        Vector3 ReletiveMove = transform.up;
        transform.position = HeroPosition + ReletiveMove * RelativeAdd;
    }

    public Vector3 getMouseDirct()
    {
        return transform.up;
    }
}
