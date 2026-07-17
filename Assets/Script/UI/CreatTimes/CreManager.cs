using UnityEngine;

public class CreManager : MonoBehaviour
{
    public GameObject Cre1;
    public GameObject Cre2;
    public GameObject Cre3;

    public float CreateNum = 3f;

    [Min(0.1f)]
    public float RecoverSpeed = 1f;

    private float RecoverAt;
    private const float MaxCreateNum = 3f;

    private void Start()
    {
        if (Cre1 == null)
        {
            Cre1 = GameObject.Find("Create1");
        }

        if (Cre2 == null)
        {
            Cre2 = GameObject.Find("Create2");
        }

        if (Cre3 == null)
        {
            Cre3 = GameObject.Find("Create3");
        }

        CreateNum = Mathf.Clamp(CreateNum, 0f, MaxCreateNum);
        RecoverAt = Time.time;

        RecoverCreateNum();
    }

    private void Update()
    {
        RecoverCreateNum();
    }

    public bool CanCreate()
    {
        return CreateNum >= 1f;
    }

    public bool ConsumeCreateCharge()
    {
        if (!CanCreate())
        {
            TurnRed();
            return false;
        }

        CreateNum = Mathf.Max(0f, CreateNum - 1f);
        return true;
    }

    public void ShowNotEnoughFeedback()
    {
        TurnRed();
    }

    private void RecoverCreateNum()
    {
        if (CreateNum < MaxCreateNum)
        {
            if (Time.time - RecoverAt >= RecoverSpeed / 10f)
            {
                CreateNum += 0.1f;
                CreateNum = Mathf.Min(CreateNum, MaxCreateNum);

                RecoverAt = Time.time;
            }
        }
        else
        {
            CreateNum = MaxCreateNum;
            RecoverAt = Time.time;
        }

        if (CreateNum >= 0f && CreateNum < 1f)
        {
            Cre1.GetComponent<Create1Scrip>()
                .GiveCR1Rate(CreateNum);

            Cre2.GetComponent<Create2Scrip>()
                .GiveCR2Rate(0f);

            Cre3.GetComponent<Create3Scrip>()
                .GiveCR3Rate(0f);
        }
        else if (CreateNum >= 1f && CreateNum < 2f)
        {
            Cre1.GetComponent<SpriteRenderer>().color =
                new Color(0f, 0f, 1f, 1f);

            Cre1.GetComponent<Create1Scrip>()
                .GiveCR1Rate(1f);

            Cre2.GetComponent<Create2Scrip>()
                .GiveCR2Rate(CreateNum - 1f);

            Cre3.GetComponent<Create3Scrip>()
                .GiveCR3Rate(0f);
        }
        else
        {
            Cre1.GetComponent<SpriteRenderer>().color =
                new Color(0f, 0f, 1f, 1f);

            Cre1.GetComponent<Create1Scrip>()
                .GiveCR1Rate(1f);

            Cre2.GetComponent<Create2Scrip>()
                .GiveCR2Rate(1f);

            Cre3.GetComponent<Create3Scrip>()
                .GiveCR3Rate(CreateNum - 2f);
        }
    }

    private void TurnRed()
    {
        if (Cre1 == null)
        {
            return;
        }

        Cre1.GetComponent<SpriteRenderer>().color =
            new Color(1f, 0f, 0f, 1f);
    }
}