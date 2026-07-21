using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class weapon2 : MonoBehaviour
{
    public GameObject TheHero;
    private SpriteRenderer MySR;
    // Start is called before the first frame update
    void Start()
    {
        TheHero = GameObject.Find("Hero");
        MySR = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!TheHero.GetComponent<heroscrip>().IsMelee)
        {
            MySR.color = new Color(1f, 1f, 1f, 1f);
        }
        else
        {
            MySR.color = new Color(0f, 0f, 0f, 1f);
        }
    }
}
