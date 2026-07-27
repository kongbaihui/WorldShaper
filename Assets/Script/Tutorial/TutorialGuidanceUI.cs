using System.Collections;
using Challenge2.TerrainPrototype;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class TutorialGuidanceUI : MonoBehaviour
{
    private enum GuidanceStage
    {
        Move,
        BuildUp,
        SwitchAttack,
        AttackDummy,
        BlockSpike,
        UseGrapple,
        ReleaseGrapple,
        UseLaser
    }

    [Header("Scene References")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Transform hero;
    [SerializeField] private Transform buildSectionEnd;
    [SerializeField] private PrototypeDamageable openingDummy;
    [SerializeField] private TutorialArmCharge spikeCharge;
    [SerializeField] private GrappleHook grappleHook;

    [Header("Presentation")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
    [SerializeField, Min(0.1f)] private float movementDistance = 4f;

    private CanvasGroup canvasGroup;
    private GuidanceStage currentStage;
    private int previousOpeningDummyHealth;
    private float heroStartX;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (hero != null)
        {
            heroStartX = hero.position.x;
        }

        if (openingDummy != null)
        {
            previousOpeningDummyHealth = openingDummy.CurrentHealth;
            openingDummy.HealthChanged += HandleOpeningDummyHealthChanged;
        }

        if (spikeCharge != null)
        {
            spikeCharge.TrainingBlocked += HandleSpikeBlocked;
        }

        SetStage(GuidanceStage.Move, false);
    }

    private void OnDisable()
    {
        if (openingDummy != null)
        {
            openingDummy.HealthChanged -= HandleOpeningDummyHealthChanged;
        }

        if (spikeCharge != null)
        {
            spikeCharge.TrainingBlocked -= HandleSpikeBlocked;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    private void Update()
    {
        if (currentStage == GuidanceStage.Move &&
            hero != null &&
            Mathf.Abs(hero.position.x - heroStartX) >= movementDistance)
        {
            SetStage(GuidanceStage.BuildUp, true);
        }

        if (currentStage == GuidanceStage.BuildUp &&
            hero != null &&
            buildSectionEnd != null &&
            hero.position.x >= buildSectionEnd.position.x)
        {
            SetStage(GuidanceStage.SwitchAttack, true);
        }

        if (currentStage == GuidanceStage.SwitchAttack &&
            GameKeySettings.WasPressed(GameKeyAction.SwitchWeapon))
        {
            SetStage(GuidanceStage.AttackDummy, true);
        }

        if (currentStage == GuidanceStage.UseGrapple &&
            grappleHook != null &&
            grappleHook.IsPulling)
        {
            SetStage(GuidanceStage.ReleaseGrapple, true);
        }

        if (currentStage == GuidanceStage.ReleaseGrapple &&
            grappleHook != null &&
            !grappleHook.IsPulling)
        {
            SetStage(GuidanceStage.UseLaser, true);
        }
    }

    private void HandleOpeningDummyHealthChanged(
        int currentHealth,
        int maximumHealth)
    {
        bool tookDamage =
            currentHealth < previousOpeningDummyHealth;
        previousOpeningDummyHealth = currentHealth;

        if (tookDamage &&
            (currentStage == GuidanceStage.SwitchAttack ||
             currentStage == GuidanceStage.AttackDummy))
        {
            SetStage(GuidanceStage.BlockSpike, true);
        }
    }

    private void HandleSpikeBlocked()
    {
        SetStage(GuidanceStage.UseGrapple, true);
    }

    private void SetStage(GuidanceStage stage, bool animate)
    {
        currentStage = stage;
        if (instructionText == null)
        {
            return;
        }

        instructionText.text = BuildInstruction(stage);

        if (!animate || fadeDuration <= 0f)
        {
            canvasGroup.alpha = 1f;
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeIn());
    }

    private string BuildInstruction(GuidanceStage stage)
    {
        switch (stage)
        {
            case GuidanceStage.Move:
                return "Use [" +
                       GameKeySettings.GetDisplayName(
                           GameKeyAction.MoveLeft) +
                       "] / [" +
                       GameKeySettings.GetDisplayName(
                           GameKeyAction.MoveRight) +
                       "] to move.";

            case GuidanceStage.BuildUp:
                return "Switch to build mode [" +
                       GameKeySettings.GetDisplayName(
                           GameKeyAction.BuildMode) +
                       "]. Use platform [" +
                       GameKeySettings.GetDisplayName(
                           GameKeyAction.SelectPlatform) +
                       "] or stone wall [" +
                       GameKeySettings.GetDisplayName(
                           GameKeyAction.SelectWall) +
                       "] and build upward past the wall. Press [" +
                       GameKeySettings.GetDisplayName(
                           GameKeyAction.MoveDown) +
                       "] + [" +
                       GameKeySettings.GetDisplayName(
                           GameKeyAction.Jump) +
                       "] to drop through a platform you created.";

            case GuidanceStage.SwitchAttack:
                return "At the two stone walls, press [" +
                       GameKeySettings.GetDisplayName(
                           GameKeyAction.SwitchWeapon) +
                       "] to switch to attack mode.";

            case GuidanceStage.AttackDummy:
                return "Use Left Mouse to attack the training dummy.";

            case GuidanceStage.BlockSpike:
                return "Block the incoming attack: build mode [" +
                       GameKeySettings.GetDisplayName(
                           GameKeyAction.BuildMode) +
                       "]  |  Select stone wall [" +
                       GameKeySettings.GetDisplayName(
                           GameKeyAction.SelectWall) +
                       "]  |  Place it in the red path.";

            case GuidanceStage.UseGrapple:
                return "Left-click the grapple hook to cross the upper section.";

            case GuidanceStage.ReleaseGrapple:
                return "Press [" +
                       GameKeySettings.GetDisplayName(
                           GameKeyAction.Jump) +
                       "] to slide down from the grapple line.";

            case GuidanceStage.UseLaser:
                return "Use laser [" +
                       GameKeySettings.GetDisplayName(
                           GameKeyAction.Laser) +
                       "] to attack the final training dummy.";

            default:
                return string.Empty;
        }
    }

    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha =
                Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        fadeRoutine = null;
    }
}
