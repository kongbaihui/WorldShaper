using System;
using System.Collections;
using Challenge2.TerrainPrototype;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class TutorialArmCharge : MonoBehaviour
{
    [Header("Activation")]
    [SerializeField] private bool autoStart;
    [SerializeField] private bool runWithoutTutorialManager;

    [SerializeField] private SpriteRenderer warningPath;
    [SerializeField, Min(0.05f)] private float warningLineWidth = 0.35f;
    [SerializeField] private Color warningLineColor =
        new Color(1f, 0.15f, 0.08f, 0.75f);
    [SerializeField] private Vector2 chargeDirection = Vector2.right;
    [SerializeField, Min(1f)] private float chargeDistance = 28f;
    [SerializeField, Min(0.1f)] private float chargeSpeed = 35f;
    [SerializeField, Min(0f)] private float warningDuration = 1.5f;
    [SerializeField, Min(0f)] private float retryDelay = 0.8f;

    private Collider2D armCollider;
    private Rigidbody2D chargeBody;
    private LineRenderer generatedWarningLine;
    private Material generatedWarningMaterial;
    private Vector3 startPosition;
    private Coroutine trainingRoutine;
    private bool trainingCompleted;

    public bool AutoStart => autoStart;
    public bool IsTraining => trainingRoutine != null;
    public bool TrainingCompleted => trainingCompleted;
    public event Action TrainingBlocked;

    private void Awake()
    {
        armCollider = GetComponent<Collider2D>();
        chargeBody = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        CreateWarningLine();

        if (warningPath != null)
        {
            warningPath.enabled = false;
        }
    }

    private void OnEnable()
    {
        trainingCompleted = false;
        if (autoStart)
        {
            BeginTraining();
        }
    }

    private void Update()
    {
        if (autoStart &&
            !trainingCompleted &&
            trainingRoutine == null &&
            ShouldContinueTraining())
        {
            BeginTraining();
        }
    }

    public void BeginTraining()
    {
        trainingCompleted = false;
        if (trainingRoutine != null)
        {
            StopCoroutine(trainingRoutine);
        }

        trainingRoutine = StartCoroutine(TrainingLoop());
    }

    private IEnumerator TrainingLoop()
    {
        while (ShouldContinueTraining())
        {
            SetChargePosition(startPosition);
            SetWarningVisible(true);
            yield return new WaitForSeconds(warningDuration);
            SetWarningVisible(false);

            bool wallBlocked = false;
            bool playerHit = false;
            float travelled = 0f;
            Vector2 direction = chargeDirection.sqrMagnitude > 0f
                ? chargeDirection.normalized
                : Vector2.right;

            while (travelled < chargeDistance)
            {
                float moveDistance = Mathf.Min(
                    chargeSpeed * Time.deltaTime,
                    chargeDistance - travelled);

                if (CheckChargePath(direction, moveDistance, out wallBlocked, out playerHit))
                {
                    break;
                }

                MoveCharge(direction * moveDistance);
                travelled += moveDistance;
                yield return null;
            }

            if (wallBlocked)
            {
                trainingCompleted = true;
                TrainingBlocked?.Invoke();
                if (TutorialManager.Instance != null &&
                    TutorialManager.Instance.IsCurrentStep(
                        TutorialStep.WallBlock))
                {
                    TutorialManager.Instance.TryCompleteStep(
                        TutorialStep.WallBlock);
                }

                break;
            }

            SetChargePosition(startPosition);
            if (playerHit || travelled >= chargeDistance)
            {
                yield return new WaitForSeconds(retryDelay);
            }
        }

        SetWarningVisible(false);
        SetChargePosition(startPosition);
        trainingRoutine = null;
    }

    private bool ShouldContinueTraining()
    {
        return runWithoutTutorialManager ||
               (TutorialManager.Instance != null &&
                TutorialManager.Instance.IsCurrentStep(
                    TutorialStep.WallBlock));
    }

    private bool CheckChargePath(
        Vector2 direction,
        float distance,
        out bool wallBlocked,
        out bool playerHit)
    {
        wallBlocked = false;
        playerHit = false;
        Physics2D.SyncTransforms();

        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            armCollider.bounds.center,
            armCollider.bounds.size,
            transform.eulerAngles.z,
            direction,
            distance);

        System.Array.Sort(
            hits,
            (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i].collider;
            if (hit == null || hit == armCollider)
            {
                continue;
            }

            TerrainSegment segment = hit.GetComponentInParent<TerrainSegment>();
            TerrainEntity terrain = segment != null
                ? segment.ParentTerrain
                : hit.GetComponentInParent<TerrainEntity>();

            if (terrain != null &&
                !terrain.IsBeingDestroyed &&
                terrain.Owner == TerrainOwner.Player &&
                terrain.TerrainType == TerrainType.FallingStoneWall)
            {
                terrain.DestroyTerrain(true);
                wallBlocked = true;
                return true;
            }

            if (hit.GetComponentInParent<heroscrip>() != null)
            {
                playerHit = true;
                return true;
            }
        }

        return false;
    }

    private void SetWarningVisible(bool visible)
    {
        if (warningPath != null)
        {
            warningPath.enabled = visible;
        }

        if (generatedWarningLine != null)
        {
            generatedWarningLine.enabled = visible;
        }
    }

    private void CreateWarningLine()
    {
        GameObject lineObject = new GameObject("ChargeWarningLine");
        lineObject.transform.SetParent(transform, false);

        generatedWarningLine = lineObject.AddComponent<LineRenderer>();
        generatedWarningLine.useWorldSpace = true;
        generatedWarningLine.positionCount = 2;
        generatedWarningLine.startWidth = warningLineWidth;
        generatedWarningLine.endWidth = warningLineWidth;
        generatedWarningLine.startColor = warningLineColor;
        generatedWarningLine.endColor = warningLineColor;
        generatedWarningLine.numCapVertices = 4;

        Shader warningShader = Shader.Find("Sprites/Default");
        if (warningShader != null)
        {
            generatedWarningMaterial = new Material(warningShader);
            generatedWarningLine.sharedMaterial = generatedWarningMaterial;
        }

        Vector2 direction = chargeDirection.sqrMagnitude > 0f
            ? chargeDirection.normalized
            : Vector2.right;
        Vector3 lineStart = armCollider.bounds.center;
        Vector3 lineEnd = lineStart + (Vector3)(direction * chargeDistance);
        generatedWarningLine.SetPosition(0, lineStart);
        generatedWarningLine.SetPosition(1, lineEnd);
        generatedWarningLine.enabled = false;
    }

    private void SetChargePosition(Vector3 position)
    {
        if (chargeBody != null)
        {
            chargeBody.position = position;
            return;
        }

        transform.position = position;
    }

    private void MoveCharge(Vector2 offset)
    {
        if (chargeBody != null)
        {
            chargeBody.position += offset;
            return;
        }

        transform.position += (Vector3)offset;
    }

    private void OnDisable()
    {
        if (trainingRoutine != null)
        {
            StopCoroutine(trainingRoutine);
            trainingRoutine = null;
        }

        SetWarningVisible(false);
        SetChargePosition(startPosition);
    }

    private void OnDestroy()
    {
        if (generatedWarningMaterial != null)
        {
            Destroy(generatedWarningMaterial);
        }
    }
}
