using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineChargeBar : MonoBehaviour
{
    public float InitialPositionX = 35;
    public float InitialPositionY = -40;
    public GameObject TheHero;
    public float ChargeSpeed = 1f;
    public float Charge = 1;
    public float MaxCharge = 10f;
    // Start is called before the first frame update
    void Start()
    {
        TheHero = GameObject.Find("Hero");
    }

    // Update is called once per frame
    void Update()
    {

        if (Charge < MaxCharge)
        {
            float addValue = ChargeSpeed * Time.deltaTime;
            Charge = Mathf.Min(Charge + addValue, MaxCharge);
            ChangeBarTo(Charge / MaxCharge);

            GetComponent<SpriteRenderer>().color = new Color(0f, 1f, 1f, 1f);
        }
        else if (Charge == MaxCharge)
        {
            GetComponent<SpriteRenderer>().color = new Color(1f, 0.5f, 1f, 1f);
        }

    }

    public void ChangeBarTo(float TargetNum)
    {
        float BarRate = TargetNum;
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
