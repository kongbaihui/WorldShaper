using Challenge2.TerrainPrototype;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TutorialDummyPurpose
{
    HitReactionOnly,
    StartSpikeSequence,
    ExitOnLaser
}

[DisallowMultipleComponent]
[RequireComponent(
    typeof(BoxCollider2D),
    typeof(Animator),
    typeof(PrototypeDamageable))]
public sealed class TutorialDummyHitReaction : MonoBehaviour
{
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private string hitTriggerName = "Hitted";

    [Header("Tutorial Sequence")]
    [SerializeField] private TutorialDummyPurpose purpose;
    [SerializeField] private TutorialArmCharge spikeCharge;
    [SerializeField] private GameObject unlockAfterBlock;
    [SerializeField] private string exitSceneName = "StartScene";

    private PrototypeDamageable damageable;
    private int hitTriggerHash;
    private bool hasHitTrigger;
    private int previousHealth;
    private bool sequenceStarted;

    private void Awake()
    {
        damageable = GetComponent<PrototypeDamageable>();

        if (targetAnimator == null)
        {
            targetAnimator = GetComponent<Animator>();
        }

        CacheHitTrigger();
    }

    private void OnEnable()
    {
        if (damageable == null)
        {
            damageable = GetComponent<PrototypeDamageable>();
        }

        if (damageable != null)
        {
            previousHealth = damageable.CurrentHealth;
            damageable.HealthChanged += HandleHealthChanged;
        }

        if (spikeCharge != null)
        {
            spikeCharge.TrainingBlocked += HandleTrainingBlocked;
        }
    }

    private void OnDisable()
    {
        if (damageable != null)
        {
            damageable.HealthChanged -= HandleHealthChanged;
        }

        if (spikeCharge != null)
        {
            spikeCharge.TrainingBlocked -= HandleTrainingBlocked;
        }
    }

    private void HandleHealthChanged(int currentHealth, int maximumHealth)
    {
        bool tookDamage = currentHealth < previousHealth;
        previousHealth = currentHealth;

        if (tookDamage &&
            purpose == TutorialDummyPurpose.StartSpikeSequence &&
            !sequenceStarted &&
            spikeCharge != null)
        {
            sequenceStarted = true;
            if (!spikeCharge.gameObject.activeSelf)
            {
                spikeCharge.gameObject.SetActive(true);
            }

            spikeCharge.BeginTraining();
        }

        if (!tookDamage ||
            targetAnimator == null ||
            !hasHitTrigger)
        {
            return;
        }

        targetAnimator.ResetTrigger(hitTriggerHash);
        targetAnimator.SetTrigger(hitTriggerHash);
    }

    public bool TryReceiveLaserHit()
    {
        if (purpose != TutorialDummyPurpose.ExitOnLaser ||
            string.IsNullOrWhiteSpace(exitSceneName))
        {
            return false;
        }

        SceneManager.LoadScene(exitSceneName);
        return true;
    }

    private void HandleTrainingBlocked()
    {
        if (purpose == TutorialDummyPurpose.StartSpikeSequence &&
            unlockAfterBlock != null)
        {
            unlockAfterBlock.SetActive(true);
        }
    }

    private void CacheHitTrigger()
    {
        hasHitTrigger = false;
        if (targetAnimator == null ||
            targetAnimator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(hitTriggerName))
        {
            return;
        }

        hitTriggerHash = Animator.StringToHash(hitTriggerName);
        AnimatorControllerParameter[] parameters = targetAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == hitTriggerHash &&
                parameters[i].type == AnimatorControllerParameterType.Trigger)
            {
                hasHitTrigger = true;
                return;
            }
        }

        Debug.LogWarning(
            $"Animator on '{name}' has no Trigger parameter named " +
            $"'{hitTriggerName}'.",
            this);
    }
}
