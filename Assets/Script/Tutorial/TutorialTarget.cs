using System.Collections;
using Challenge2.TerrainPrototype;
using UnityEngine;

public enum TutorialHitType
{
    Ranged,
    Melee,
    WallDamage,
    SpikeDamage
}

[RequireComponent(
    typeof(SpriteRenderer),
    typeof(Collider2D),
    typeof(PrototypeDamageable))]
public sealed class TutorialTarget : MonoBehaviour
{
    private const string DummySpritePath =
        "Assets/Resources/Art/Tutorial/Dummy/dummy.png";

    [SerializeField] private TutorialHitType requiredHit;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite hurtSprite1;
    [SerializeField] private Sprite hurtSprite2;
    [SerializeField, Min(0.02f)] private float hurtFrameDuration = 0.1f;
    [SerializeField] private bool hideAfterHit = true;

    private SpriteRenderer spriteRenderer;
    private Collider2D targetCollider;
    private bool completed;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Object[] assets =
            UnityEditor.AssetDatabase.LoadAllAssetsAtPath(DummySpritePath);

        for (int i = 0; i < assets.Length; i++)
        {
            Sprite sprite = assets[i] as Sprite;
            if (sprite == null)
            {
                continue;
            }

            if (sprite.name == "dummy_idle")
            {
                idleSprite = sprite;
            }
            else if (sprite.name == "dummy_hurt_1")
            {
                hurtSprite1 = sprite;
            }
            else if (sprite.name == "dummy_hurt_2")
            {
                hurtSprite2 = sprite;
            }
        }

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && idleSprite != null)
        {
            renderer.sprite = idleSprite;
        }
    }
#endif

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        targetCollider = GetComponent<Collider2D>();

        if (idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
    }

    public void TryReceiveHit(TutorialHitType hitType)
    {
        if (completed ||
            hitType != requiredHit ||
            TutorialManager.Instance == null)
        {
            return;
        }

        TutorialStep expectedStep = GetExpectedStep(hitType);
        if (!TutorialManager.Instance.IsCurrentStep(expectedStep))
        {
            return;
        }

        completed = true;
        TutorialManager.Instance.TryCompleteStep(expectedStep);
        StartCoroutine(PlayHurtOnce());
    }

    private IEnumerator PlayHurtOnce()
    {
        if (hurtSprite1 != null)
        {
            spriteRenderer.sprite = hurtSprite1;
            yield return new WaitForSeconds(hurtFrameDuration);
        }

        if (hurtSprite2 != null)
        {
            spriteRenderer.sprite = hurtSprite2;
            yield return new WaitForSeconds(hurtFrameDuration);
        }

        if (hideAfterHit)
        {
            spriteRenderer.enabled = false;
            targetCollider.enabled = false;
        }
        else if (idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
    }

    private static TutorialStep GetExpectedStep(TutorialHitType hitType)
    {
        switch (hitType)
        {
            case TutorialHitType.Ranged:
                return TutorialStep.Ranged;
            case TutorialHitType.Melee:
                return TutorialStep.Melee;
            case TutorialHitType.WallDamage:
                return TutorialStep.WallDamage;
            default:
                return TutorialStep.SpikeDamage;
        }
    }
}
