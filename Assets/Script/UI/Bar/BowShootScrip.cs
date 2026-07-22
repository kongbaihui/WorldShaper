using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BowShootScrip : MonoBehaviour
{
    public float InitialPositionX = -68;
    public float InitialPositionY = -40;
    public float Charge = 0;
    public float ChargeSpeed = 30f;
    public GameObject TheHero;
    // Start is called before the first frame update
    void Start()
    {
        TheHero = GameObject.Find("Hero");
    }

    // Update is called once per frame
    void Update()
    {
        if (TheHero.GetComponent<heroscrip>().RemainNum == 0)
        {
            if (!TheHero.GetComponent<heroscrip>().IsMelee)
            {
                if (Charge < 3)
                {
                    StartCharge();
                }
                else
                {
                    TheHero.GetComponent<heroscrip>().RemainNum = 3;
                    Charge = 0;
                }
            }
            ChangeScale(Charge);
        }
        else if (TheHero.GetComponent<heroscrip>().RemainNum == 1)
        {
            ChangeScale(1);
        }
        else if (TheHero.GetComponent<heroscrip>().RemainNum == 2)
        {
            ChangeScale(2);
        }
        else
        {
            ChangeScale(3);
        }
    }


    void ChangeScale(float NowNum)
    {
        float BarRate = Mathf.Clamp01(NowNum / 3f);
        Vector3 TempPosition, TempSacle;
        TempPosition.x = InitialPositionX + (4f / 2f) * (1 - BarRate);
        TempPosition.y = transform.localPosition.y;
        TempPosition.z = transform.localPosition.z;
        transform.localPosition = TempPosition;

        TempSacle.x = 4f * BarRate;
        TempSacle.y = transform.localScale.y;
        TempSacle.z = transform.localScale.z;
        transform.localScale = TempSacle;

        if (TheHero.GetComponent<heroscrip>().RemainNum == 0)
        {
            GetComponent<SpriteRenderer>().color = new Color(0f, 0.33f, 0.5f, 1f);
        }
        else
        {
            GetComponent<SpriteRenderer>().color = new Color(0f, 0.5f, 0f, 1f);
        }
    }

    void StartCharge()
    {
        float addValue = ChargeSpeed * Time.deltaTime;
        Charge = Mathf.Min(Charge + addValue, 3);
    }
}
