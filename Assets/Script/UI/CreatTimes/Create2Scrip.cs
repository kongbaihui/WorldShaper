using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Create2Scrip : MonoBehaviour
{
    public float InitialPositionX = 0;
    public float InitialPositionY = -40;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 1f, 1f);
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void GiveCR2Rate(float BarRate)
    {
        Vector3 TempPosition, TempSacle;
        TempPosition.x = InitialPositionX - (5f / 2f) * (1 - BarRate);
        TempPosition.y = transform.position.y;
        TempPosition.z = transform.position.z;
        transform.localPosition = TempPosition;

        TempSacle.x = 5f * BarRate;
        TempSacle.y = transform.localScale.y;
        TempSacle.z = transform.localScale.z;
        transform.localScale = TempSacle;
    }
}
