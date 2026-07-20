using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrengthBarScrip : MonoBehaviour
{
    public float InitialPositionX = -35;
    public float InitialPositionY = -40;
    public GameObject TheHero;
    void Start()
    {
        TheHero = GameObject.Find("Hero");
    }

    // Update is called once per frame
    void Update()
    {
        float TargetNum = (TheHero.GetComponent<heroscrip>().BulletSpeed - TheHero.GetComponent<heroscrip>().BulletInitialSpeed) / (TheHero.GetComponent<heroscrip>().MaxBulletSpeed - TheHero.GetComponent<heroscrip>().BulletInitialSpeed);
        ChangeBarTo(TargetNum * 100f);

        if (TargetNum >= 1)
        {
            //highlight
            GetComponent<SpriteRenderer>().color = new Color(1f, 0.5f, 1f, 1f);
        }
        else
        {
            GetComponent<SpriteRenderer>().color = new Color(0f, 1f, 1f, 1f);
        }
    }

    public void ChangeBarTo(float TargetNum)
    {
        float BarRate = Mathf.Clamp01(TargetNum / 100f);
        Vector3 TempPosition, TempSacle;
        TempPosition.x = InitialPositionX - (20f / 2f) * (1 - BarRate);
        TempPosition.y = transform.localPosition.y;
        TempPosition.z = transform.localPosition.z;
        transform.localPosition = TempPosition;

        TempSacle.x = 20f * BarRate;
        TempSacle.y = transform.localScale.y;
        TempSacle.z = transform.localScale.z;
        transform.localScale = TempSacle;
    }
}
