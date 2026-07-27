using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TutorialStep
{
    Move,
    Jump,
    Ranged,
    Melee,
    Platform,
    WallDamage,
    WallBlock,
    SpikeDamage,
    Exit
}

public sealed class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private GameObject explanationPanel;
    [SerializeField] private TMP_Text explanationText;
    [SerializeField] private GameObject completePanel;
    [SerializeField] private TMP_Text completeText;

    [Header("Level")]
    [Tooltip("Gate at index N opens when TutorialStep N is completed.")]
    [SerializeField] private GameObject[] gates;
    [SerializeField] private TutorialArmCharge armCharge;
    [SerializeField] private heroscrip hero;

    [Header("Scene Names")]
    [SerializeField] private string firstGameScene = "SampleScene";
    [SerializeField] private string menuScene = "StartScene";

    private TutorialStep currentStep;
    private string lastInstruction;
    private float nextInstructionRefresh;

    public TutorialStep CurrentStep => currentStep;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (hero == null)
        {
            hero = FindObjectOfType<heroscrip>();
        }

        if (gates != null)
        {
            for (int i = 0; i < gates.Length; i++)
            {
                if (gates[i] != null)
                {
                    gates[i].SetActive(true);
                }
            }
        }

        if (explanationPanel != null)
        {
            explanationPanel.SetActive(false);
        }

        if (completePanel != null)
        {
            completePanel.SetActive(false);
        }

        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
        }

        currentStep = TutorialStep.Move;
        RefreshInstruction(true);
    }

    private void Update()
    {
        if (currentStep == TutorialStep.Ranged &&
            hero != null &&
            hero.RemainNum <= 0)
        {
            // 教程中射空后补一支箭，避免玩家永久卡关。
            hero.RemainNum = 1;
        }

        if (Time.unscaledTime >= nextInstructionRefresh)
        {
            nextInstructionRefresh = Time.unscaledTime + 0.25f;
            RefreshInstruction(false);
        }
    }

    public bool IsCurrentStep(TutorialStep step)
    {
        return currentStep == step;
    }

    public void TryCompleteStep(TutorialStep step)
    {
        if (step != currentStep)
        {
            return;
        }

        int completedIndex = (int)currentStep;
        if (gates != null &&
            completedIndex >= 0 &&
            completedIndex < gates.Length &&
            gates[completedIndex] != null)
        {
            gates[completedIndex].SetActive(false);
        }

        if (currentStep == TutorialStep.Exit)
        {
            ShowCompletePanel();
            return;
        }

        currentStep++;
        EnterCurrentStep();
    }

    public void ContinueWallBlockExplanation()
    {
        if (currentStep != TutorialStep.WallBlock)
        {
            return;
        }

        if (explanationPanel != null)
        {
            explanationPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        if (armCharge != null)
        {
            armCharge.BeginTraining();
        }
    }

    public void StartGame()
    {
        LoadScene(firstGameScene);
    }

    public void BackToMenu()
    {
        LoadScene(menuScene);
    }

    private void EnterCurrentStep()
    {
        RefreshInstruction(true);

        if (currentStep == TutorialStep.WallBlock)
        {
            if (explanationText != null)
            {
                explanationText.text =
                    "A falling stone wall can stop the boss arm charge. " +
                    "Create the wall directly in the red warning path.";
            }

            if (explanationPanel != null)
            {
                explanationPanel.SetActive(true);
            }

            Time.timeScale = 0f;
        }
    }

    private void RefreshInstruction(bool force)
    {
        if (instructionText == null)
        {
            return;
        }

        string text = BuildInstruction();
        if (force || text != lastInstruction)
        {
            instructionText.text = text;
            lastInstruction = text;
        }
    }

    private string BuildInstruction()
    {
        switch (currentStep)
        {
            case TutorialStep.Move:
                return "Move right with " +
                       GameKeySettings.GetDisplayName(GameKeyAction.MoveRight) +
                       " and reach the marker.";

            case TutorialStep.Jump:
                return "Press " +
                       GameKeySettings.GetDisplayName(GameKeyAction.Jump) +
                       " and land on the raised platform.";

            case TutorialStep.Ranged:
                return "Aim with the mouse, then hold and release Left Mouse " +
                       "to hit the target with an arrow.";

            case TutorialStep.Melee:
                return "Press " +
                       GameKeySettings.GetDisplayName(GameKeyAction.SwitchWeapon) +
                       " to select melee, then hit the target with Left Mouse.";

            case TutorialStep.Platform:
                return "Press " +
                       GameKeySettings.GetDisplayName(GameKeyAction.BuildMode) +
                       " for build mode, select platform with " +
                       GameKeySettings.GetDisplayName(GameKeyAction.SelectPlatform) +
                       ", then create one with Left Mouse and cross the gap.";

            case TutorialStep.WallDamage:
                return "In build mode select stone wall with " +
                       GameKeySettings.GetDisplayName(GameKeyAction.SelectWall) +
                       ", then drop it from above onto the target.";

            case TutorialStep.WallBlock:
                return "Create a stone wall in the red path to block the arm charge.";

            case TutorialStep.SpikeDamage:
                return "In build mode select stone spike with " +
                       GameKeySettings.GetDisplayName(GameKeyAction.SelectSpike) +
                       ", then drop it onto the target.";

            case TutorialStep.Exit:
                return "Reach the exit.";

            default:
                return string.Empty;
        }
    }

    private void ShowCompletePanel()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }

        if (explanationPanel != null)
        {
            explanationPanel.SetActive(false);
        }

        if (completeText != null)
        {
            completeText.text = "Tutorial Complete";
        }

        if (completePanel != null)
        {
            completePanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    private static void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
        }
    }
}
