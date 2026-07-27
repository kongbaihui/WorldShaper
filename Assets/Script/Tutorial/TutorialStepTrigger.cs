using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class TutorialStepTrigger : MonoBehaviour
{
    [SerializeField] private TutorialStep step;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<heroscrip>() == null)
        {
            return;
        }

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.TryCompleteStep(step);
        }
    }
}
