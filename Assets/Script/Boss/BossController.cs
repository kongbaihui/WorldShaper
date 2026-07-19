using System.Collections;
using Challenge2.TerrainPrototype;
using UnityEngine;

namespace FinalGame.Boss
{
    public enum BossState
    {
        Idle,
        SelectAttack,
        Telegraph,
        ExecuteAttack,
        Recover,
        PhaseTransition,
        Hurt,
        Dead
    }

    public enum BossAttackType
    {
        None,
        FloatingPlatformShield,
        FallingStoneWall,
        FallingStoneSpike,
        CloseRangeLightWave,
        TerrainCollapse
    }

    public sealed class BossAttackPlan
    {
        public BossAttackType AttackType;
        public Vector2 SpawnPosition;
        public Vector2 AttackReferencePoint;
        public Vector2 LandingPosition;
        public float TelegraphDuration;
        public float AttackRadius;
        public int Phase;
        public TerrainEntity CollapseTarget;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PrototypeDamageable))]
    public sealed class BossController : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private Transform player;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private PrototypeDamageable bossDamageable;
        [SerializeField] private BossBarScrip bossBar;
        [SerializeField] private BossTerrainAttackController terrainAttackController;
        [SerializeField] private SpriteRenderer bossRenderer;

        [Header("Detection and Timing")]
        [SerializeField, Min(0.1f)] private float activationRange = 90f;
        [SerializeField, Min(0.1f)] private float closeDistanceThreshold = 15f;
        [SerializeField, Min(0.1f)] private float farDistanceThreshold = 32f;
        [SerializeField, Min(0f)] private float recoveryDuration = 1.15f;
        [SerializeField, Min(0f)] private float unavailableRecoveryDuration = 0.7f;
        [SerializeField, Min(0f)] private float hurtDuration = 0.25f;
        [SerializeField, Min(0f)] private float hurtRecoveryDuration = 0.55f;

        [Header("Phase Rules")]
        [SerializeField, Range(0f, 1f)] private float phaseTwoThreshold = 0.65f;
        [SerializeField, Range(0f, 1f)] private float phaseThreeThreshold = 0.30f;
        [SerializeField, Min(0f)] private float phaseTransitionDuration = 1f;
        [SerializeField, Min(0f)] private float phaseTransitionRecovery = 0.45f;
        [SerializeField] private Color phaseTwoColor = new Color(1f, 0.48f, 0.20f, 1f);
        [SerializeField] private Color phaseThreeColor = new Color(0.95f, 0.18f, 0.55f, 1f);

        [Header("Attack Selection")]
        [SerializeField, Min(0f)] private float repeatedAttackPenalty = 35f;
        [SerializeField, Min(0f)] private float selectionScoreJitter = 4f;
        [SerializeField, Min(0f)] private float fastHorizontalSpeed = 5f;

        private static readonly BossAttackType[] AttackCandidates =
        {
            BossAttackType.FloatingPlatformShield,
            BossAttackType.FallingStoneWall,
            BossAttackType.FallingStoneSpike,
            BossAttackType.CloseRangeLightWave,
            BossAttackType.TerrainCollapse
        };

        private Coroutine activeRoutine;
        private float recoverUntil;
        private Vector3 baseScale;
        private Color baseColor;
        private int lastObservedHealth;
        private bool referencesReady;
        private BossAttackType previousAttack = BossAttackType.None;

        public BossState CurrentState { get; private set; } = BossState.Idle;
        public BossAttackType CurrentAttack { get; private set; } = BossAttackType.None;
        public int CurrentPhase { get; private set; } = 1;
        public float DistanceToPlayer { get; private set; }
        public bool IsPlayerInsideActivationRange { get; private set; }

        private void Awake()
        {
            if (bossDamageable == null)
            {
                bossDamageable = GetComponent<PrototypeDamageable>();
            }

            if (bossRenderer == null)
            {
                bossRenderer = GetComponent<SpriteRenderer>();
            }

            if (playerBody == null && player != null)
            {
                playerBody = player.GetComponent<Rigidbody2D>();
            }

            baseScale = transform.localScale;
            baseColor = bossRenderer != null ? bossRenderer.color : Color.white;
            lastObservedHealth = bossDamageable != null ? bossDamageable.CurrentHealth : 0;
            referencesReady = ValidateReferences();
        }

        private void OnEnable()
        {
            if (bossDamageable != null)
            {
                bossDamageable.HealthChanged += HandleHealthChanged;
                lastObservedHealth = bossDamageable.CurrentHealth;
            }
        }

        private void Start()
        {
            UpdateBossBar();
            EnterState(BossState.Idle);
        }

        private void OnDisable()
        {
            if (bossDamageable != null)
            {
                bossDamageable.HealthChanged -= HandleHealthChanged;
            }

            CancelCurrentAttack();
        }

        private void OnValidate()
        {
            phaseTwoThreshold = Mathf.Clamp01(phaseTwoThreshold);
            phaseThreeThreshold = Mathf.Clamp(phaseThreeThreshold, 0f, phaseTwoThreshold);
            closeDistanceThreshold = Mathf.Max(0.1f, closeDistanceThreshold);
            farDistanceThreshold = Mathf.Max(closeDistanceThreshold, farDistanceThreshold);
        }

        private void Update()
        {
            if (!referencesReady || CurrentState == BossState.Dead ||
                bossDamageable == null || !bossDamageable.IsAlive)
            {
                return;
            }

            DistanceToPlayer = Vector2.Distance(transform.position, player.position);
            IsPlayerInsideActivationRange = DistanceToPlayer <= activationRange;
            TrackPlayerVisually();

            if (!IsPlayerInsideActivationRange &&
                CurrentState != BossState.PhaseTransition &&
                CurrentState != BossState.Hurt)
            {
                if (CurrentState != BossState.Idle)
                {
                    CancelCurrentAttack();
                    EnterState(BossState.Idle);
                }

                return;
            }

            switch (CurrentState)
            {
                case BossState.Idle:
                    if (IsPlayerInsideActivationRange)
                    {
                        EnterState(BossState.SelectAttack);
                    }
                    break;

                case BossState.SelectAttack:
                    SelectAndStartAttack();
                    break;

                case BossState.Recover:
                    if (Time.time >= recoverUntil)
                    {
                        EnterState(IsPlayerInsideActivationRange
                            ? BossState.SelectAttack
                            : BossState.Idle);
                    }
                    break;
            }
        }

        private bool ValidateReferences()
        {
            bool valid = player != null && bossDamageable != null && terrainAttackController != null;
            if (!valid)
            {
                Debug.LogWarning(
                    "BossController is missing Player, PrototypeDamageable, or BossTerrainAttackController references.",
                    this);
            }

            if (player != null && playerBody == null)
            {
                Debug.LogWarning("BossController: Player Rigidbody2D is not assigned; prediction will use zero velocity.", this);
            }

            return valid;
        }

        private void SelectAndStartAttack()
        {
            if (activeRoutine != null)
            {
                return;
            }

            BossAttackType selected = BossAttackType.None;
            BossAttackPlan selectedPlan = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < AttackCandidates.Length; i++)
            {
                BossAttackType candidate = AttackCandidates[i];
                if (!IsUnlocked(candidate) ||
                    !terrainAttackController.TryPrepareAttack(candidate, out BossAttackPlan candidatePlan))
                {
                    continue;
                }

                float score = ScoreAttack(candidate);
                if (candidate == previousAttack)
                {
                    score -= repeatedAttackPenalty;
                }

                score += Random.Range(-selectionScoreJitter, selectionScoreJitter);
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                selected = candidate;
                selectedPlan = candidatePlan;
            }

            if (selected == BossAttackType.None || selectedPlan == null)
            {
                BeginRecovery(unavailableRecoveryDuration);
                return;
            }

            previousAttack = selected;
            selectedPlan.Phase = CurrentPhase;
            activeRoutine = StartCoroutine(RunAttackRoutine(selectedPlan));
        }

        private IEnumerator RunAttackRoutine(BossAttackPlan plan)
        {
            CurrentAttack = plan.AttackType;
            EnterState(BossState.Telegraph);
            terrainAttackController.BeginTelegraph(plan);

            float telegraphEndsAt = Time.time + plan.TelegraphDuration;
            while (Time.time < telegraphEndsAt && CurrentState == BossState.Telegraph &&
                   bossDamageable.IsAlive && IsPlayerInsideActivationRange)
            {
                // 2026-07-19：预警开始后位置锁定，不再重新选点或重新计时。
                yield return null;
            }

            if (CurrentState != BossState.Telegraph || !bossDamageable.IsAlive ||
                !IsPlayerInsideActivationRange)
            {
                terrainAttackController.CancelActiveTelegraph();
                activeRoutine = null;
                if (bossDamageable.IsAlive && CurrentState == BossState.Telegraph)
                {
                    EnterState(IsPlayerInsideActivationRange ? BossState.Recover : BossState.Idle);
                    recoverUntil = Time.time + unavailableRecoveryDuration;
                }
                yield break;
            }

            EnterState(BossState.ExecuteAttack);
            bool executed = terrainAttackController.Execute(plan);
            terrainAttackController.CancelActiveTelegraph();
            activeRoutine = null;

            if (bossDamageable.IsAlive && CurrentState == BossState.ExecuteAttack)
            {
                BeginRecovery(executed ? recoveryDuration : unavailableRecoveryDuration);
            }
        }

        private bool IsUnlocked(BossAttackType attack)
        {
            if (attack == BossAttackType.FallingStoneWall ||
                attack == BossAttackType.FallingStoneSpike ||
                attack == BossAttackType.CloseRangeLightWave)
            {
                return CurrentPhase >= 2;
            }

            return attack != BossAttackType.TerrainCollapse || CurrentPhase >= 3;
        }

        private float ScoreAttack(BossAttackType attack)
        {
            Vector2 velocity = playerBody != null ? playerBody.velocity : Vector2.zero;
            switch (attack)
            {
                case BossAttackType.FloatingPlatformShield:
                    return CurrentPhase == 1 ? 100f : DistanceToPlayer < closeDistanceThreshold ? 62f : 36f;
                case BossAttackType.FallingStoneWall:
                    return 52f + (DistanceToPlayer > farDistanceThreshold ? 34f : 0f);
                case BossAttackType.FallingStoneSpike:
                    return 48f + (Mathf.Abs(velocity.x) >= fastHorizontalSpeed ? 32f : 0f);
                case BossAttackType.CloseRangeLightWave:
                    return CurrentPhase >= 3 ? 122f : 108f;
                case BossAttackType.TerrainCollapse:
                    return 92f;
                default:
                    return float.NegativeInfinity;
            }
        }

        private void HandleHealthChanged(int currentHealth, int maximumHealth)
        {
            if (bossBar != null)
            {
                bossBar.ChangeBarTo(maximumHealth > 0 ? currentHealth * 100f / maximumHealth : 0f);
            }

            if (currentHealth <= 0)
            {
                lastObservedHealth = currentHealth;
                EnterDead();
                return;
            }

            bool phaseTransitionStarted = TryBeginPhaseTransition(currentHealth, maximumHealth);
            if (!phaseTransitionStarted && currentHealth < lastObservedHealth)
            {
                CancelCurrentAttack();
                activeRoutine = StartCoroutine(HurtRoutine());
            }

            lastObservedHealth = currentHealth;
        }

        private bool TryBeginPhaseTransition(int currentHealth, int maximumHealth)
        {
            if (maximumHealth <= 0)
            {
                return false;
            }

            float normalizedHealth = currentHealth / (float)maximumHealth;
            int desiredPhase = normalizedHealth <= phaseThreeThreshold
                ? 3
                : normalizedHealth <= phaseTwoThreshold ? 2 : 1;

            if (desiredPhase <= CurrentPhase)
            {
                return false;
            }

            CurrentPhase = desiredPhase;
            CancelCurrentAttack();
            activeRoutine = StartCoroutine(PhaseTransitionRoutine());
            return true;
        }

        private IEnumerator HurtRoutine()
        {
            EnterState(BossState.Hurt);
            if (bossRenderer != null)
            {
                bossRenderer.color = Color.white;
            }

            yield return new WaitForSeconds(hurtDuration);
            RestorePhasePresentation();
            activeRoutine = null;
            if (bossDamageable.IsAlive)
            {
                BeginRecovery(hurtRecoveryDuration);
            }
        }

        private IEnumerator PhaseTransitionRoutine()
        {
            EnterState(BossState.PhaseTransition);
            float elapsed = 0f;
            Color phaseColor = GetPhaseColor();

            while (elapsed < phaseTransitionDuration && bossDamageable.IsAlive)
            {
                elapsed += Time.deltaTime;
                float pulse = 1f + Mathf.Sin(elapsed * Mathf.PI * 5f) * 0.14f;
                transform.localScale = baseScale * pulse;
                if (bossRenderer != null)
                {
                    bossRenderer.color = Color.Lerp(Color.white, phaseColor, Mathf.PingPong(elapsed * 3f, 1f));
                }

                yield return null;
            }

            RestorePhasePresentation();
            activeRoutine = null;
            if (bossDamageable.IsAlive)
            {
                BeginRecovery(phaseTransitionRecovery);
            }
        }

        private void EnterDead()
        {
            if (CurrentState == BossState.Dead)
            {
                return;
            }

            CancelCurrentAttack();
            CurrentAttack = BossAttackType.None;
            EnterState(BossState.Dead);
            transform.localScale = baseScale;
            if (bossRenderer != null)
            {
                bossRenderer.color = new Color(0.16f, 0.16f, 0.20f, 1f);
            }
        }

        private void BeginRecovery(float duration)
        {
            CurrentAttack = BossAttackType.None;
            recoverUntil = Time.time + Mathf.Max(0f, duration);
            EnterState(BossState.Recover);
        }

        private void CancelCurrentAttack()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            if (terrainAttackController != null)
            {
                terrainAttackController.CancelActiveTelegraph();
            }

            CurrentAttack = BossAttackType.None;
        }

        private void TrackPlayerVisually()
        {
            if (bossRenderer != null && player != null)
            {
                bossRenderer.flipX = player.position.x < transform.position.x;
            }
        }

        private void RestorePhasePresentation()
        {
            transform.localScale = baseScale;
            if (bossRenderer != null)
            {
                bossRenderer.color = GetPhaseColor();
            }
        }

        private Color GetPhaseColor()
        {
            return CurrentPhase >= 3 ? phaseThreeColor : CurrentPhase == 2 ? phaseTwoColor : baseColor;
        }

        private void UpdateBossBar()
        {
            if (bossDamageable != null && bossBar != null)
            {
                bossBar.ChangeBarTo(bossDamageable.MaximumHealth > 0
                    ? bossDamageable.CurrentHealth * 100f / bossDamageable.MaximumHealth
                    : 0f);
            }
        }

        private void EnterState(BossState state)
        {
            CurrentState = state;
        }
    }
}
