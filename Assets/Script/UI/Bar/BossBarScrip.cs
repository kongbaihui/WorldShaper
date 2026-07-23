using Challenge2.TerrainPrototype;
using UnityEngine;
using UnityEngine.SceneManagement;
public class BossBarScrip : MonoBehaviour
{
    public float InitialPositionX = 55;
    public float InitialPositionY = -35;
    [SerializeField] private PrototypeDamageable observedDamageable;

    private void OnEnable()
    {
        if (observedDamageable != null)
        {
            observedDamageable.HealthChanged += HandleHealthChanged;
            HandleHealthChanged(observedDamageable.CurrentHealth, observedDamageable.MaximumHealth);
        }
    }

    private void OnDisable()
    {
        if (observedDamageable != null)
        {
            observedDamageable.HealthChanged -= HandleHealthChanged;
        }
    }

    public void ChangeBarTo(float TargetNum)
    {
        float BarRate = Mathf.Clamp01(TargetNum / 100f);
        Vector3 TempPosition, TempSacle;
        TempPosition.x = InitialPositionX + (45f / 2f) * (1 - BarRate);
        TempPosition.y = transform.localPosition.y;
        TempPosition.z = transform.localPosition.z;
        transform.localPosition = TempPosition;

        TempSacle.x = 45f * BarRate;
        TempSacle.y = transform.localScale.y;
        TempSacle.z = transform.localScale.z;
        transform.localScale = TempSacle;
        string sceneName = SceneManager.GetActiveScene().name;
        if (BarRate == 0 && sceneName == "BossScene")
        {
            WinLoseChange.JumpToEnd(true);
        }
        else if (BarRate == 0 && sceneName == "SampleScene")
        {
            WinLoseChange.JumpToNext();
        }
    }

    private void HandleHealthChanged(int currentHealth, int maximumHealth)
    {
        ChangeBarTo(maximumHealth > 0 ? currentHealth * 100f / maximumHealth : 0f);
    }
}

