using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineScrip : MonoBehaviour
{
    public float NowScaleY = 5f;
    public float MaxScaleY = 100f;
    public GameObject TheHero;
    //public GameObject TheArrow;
    public Vector3 InitialPosition;
    // Start is called before the first frame update
    void Start()
    {
        TheHero = GameObject.Find("Hero");
        //TheArrow = GameObject.Find("Arrow");
    }

    // Update is called once per frame
    void Update()
    {
        if (NowScaleY < MaxScaleY)
        {
            IncreaseScale();
        }
        else
        {
            Destroy(gameObject);
            NowScaleY = 5f;
            TheHero.GetComponent<heroscrip>().IsShootLine = false;
        }
    }

    private void IncreaseScale()
    {
        Vector3 NewScale;
        InitialPosition = TheHero.GetComponent<heroscrip>().transform.position + transform.up;
        NewScale.x = transform.localScale.x;
        NewScale.y = NowScaleY;
        NewScale.z = transform.localScale.z;
        transform.localScale = NewScale;


        transform.localPosition = InitialPosition + transform.up * transform.localScale.y;

        NowScaleY += 0.1f;
    }
}
