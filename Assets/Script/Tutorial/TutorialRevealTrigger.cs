using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class TutorialRevealTrigger : MonoBehaviour
{
    [SerializeField] private GameObject revealTarget;
    [SerializeField] private bool hideTargetOnAwake = true;

    private bool revealed;

    private void Awake()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;

        if (hideTargetOnAwake && revealTarget != null)
        {
            revealTarget.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (revealed ||
            revealTarget == null ||
            other.GetComponentInParent<heroscrip>() == null)
        {
            return;
        }

        revealed = true;
        revealTarget.SetActive(true);
        gameObject.SetActive(false);
    }
}
