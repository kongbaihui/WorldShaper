using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class circlescrip : MonoBehaviour
{
    public GameObject TheHero;
    // Start is called before the first frame update
    void Start()
    {
        TheHero = GameObject.Find("Hero");
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = TheHero.transform.localPosition;
    }
}
