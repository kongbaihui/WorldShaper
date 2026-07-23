using System.Collections;
using System.Collections.Generic;
using Challenge2.TerrainPrototype;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[RequireComponent(
    typeof(Rigidbody2D),
    typeof(BoxCollider2D),
    typeof(PrototypeDamageable))]
public class SecondBossController : MonoBehaviour
{
    private static readonly int TailOutStateHash =
        Animator.StringToHash("Base Layer.TailoutAnimation");
    private static readonly int TailInStateHash =
        Animator.StringToHash("Base Layer.TailinAnimation");

    [Header("Main References")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private Rigidbody2D bossBody;
    [SerializeField] private PrototypeDamageable bossDamageable;
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private TerrainRegistry terrainRegistry;
    [SerializeField] private TerrainDamageService terrainDamageService;

    [Header("Body Contact Damage")]
    [SerializeField, Min(1)] private int bodyContactDamage = 10;
    [SerializeField, Min(0.05f)] private float bodyDamageInterval = 0.5f;

    [Header("Ground Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 13f;
    [SerializeField, Min(0f)] private float stopDistance = 4f;
    [SerializeField] private float leftBoundary = -66f;
    [SerializeField] private float rightBoundary = 66f;
    [SerializeField] private float bossGroundY = -15.4f;

    [Header("Upward Laser")]
    [SerializeField] private Transform laserOrigin;
    [SerializeField] private SpriteRenderer laserVisual;
    [SerializeField, Min(0.1f)] private float laserInterval = 4f;
    [SerializeField, Min(0f)] private float laserTelegraphTime = 0.55f;
    [SerializeField, Min(0.1f)] private float laserAimMaximumTime = 1.5f;
    [SerializeField, Min(0.05f)] private float laserGrowTime = 0.45f;
    [SerializeField, Min(0.1f)] private float laserDuration = 2f;
    [SerializeField, Min(1)] private int laserDamage = 18;
    [SerializeField, Min(1)] private int laserTerrainDamage = 2;
    [SerializeField, Min(0.1f)] private float laserWidth = 3f;
    [SerializeField, Min(1f)] private float laserMaximumDistance = 70f;

    [Header("Shared Tail")]
    [SerializeField] private GameObject leftTail;
    [SerializeField] private GameObject rightTail;
    [SerializeField] private Collider2D leftTailCollider;
    [SerializeField] private Collider2D rightTailCollider;
    [SerializeField] private PrototypeDamageable leftTailDamageable;
    [SerializeField] private PrototypeDamageable rightTailDamageable;
    [SerializeField, Min(0.1f)] private float tailStayTime = 2.5f;
    [SerializeField, Min(0f)] private float tailSwitchDelay = 0.5f;

    [Header("Top Spikes And Hooks")]
    [SerializeField] private GameObject[] topSpikes = new GameObject[2];
    [SerializeField] private PrototypeDamageable[] topSpikeDamageables =
        new PrototypeDamageable[2];
    [SerializeField] private Rigidbody2D[] topSpikeBodies =
        new Rigidbody2D[2];
    [SerializeField] private GameObject[] grappleHooks =
        new GameObject[2];
    [SerializeField, Min(1)] private int topSpikeImpactDamage = 20;

    [Header("Bats")]
    [SerializeField] private GameObject batTemplate;
    [SerializeField] private Transform[] batSpawnPoints;
    [SerializeField, Min(0.1f)] private float batSpawnInterval = 3.5f;
    [SerializeField, Min(0.1f)] private float batMoveSpeed = 16f;
    [SerializeField, Min(1)] private int batDamage = 10;
    [SerializeField, Min(1)] private int batMaximumCount = 6;
    [SerializeField, Min(0.1f)] private float batLifeTime = 12f;

    [Header("Moving Platforms")]
    [SerializeField] private Transform[] movingPlatforms;
    [SerializeField] private GameObject[] platformsToShatter;
    [SerializeField, Min(0f)] private float platformMoveRange = 7f;
    [SerializeField, Min(0.1f)] private float platformMoveSpeed = 1.1f;
    [SerializeField, Min(0f)] private float platformCarryTolerance = 0.25f;

    private readonly List<SecondBossBat> spawnedBats =
        new List<SecondBossBat>();
    private readonly List<TerrainEntity> terrainSnapshot =
        new List<TerrainEntity>();
    private readonly HashSet<int> damagedActorIds = new HashSet<int>();
    private readonly HashSet<int> damagedTerrainIds = new HashSet<int>();

    private Vector3[] platformStartPositions;
    private Rigidbody2D[] movingPlatformBodies;
    private Collider2D[] movingPlatformColliders;
    private Collider2D playerCollider;
    private heroscrip playerController;
    private float phaseTwoStartedAt;
    private float nextBodyDamageTime;
    private int sharedTailHealth;
    private int previousLeftTailHealth;
    private int previousRightTailHealth;
    private int currentPhase = 1;
    private bool syncingTailHealth;
    private bool tailDefeated;
    private bool hasShatteredTerrain;
    private bool bossDead;
    private bool laserPreparing;
    private bool laserActive;
    private bool[] topSpikeDropped = new bool[2];

    private void Awake()
    {
        if (bossBody == null)
        {
            bossBody = GetComponent<Rigidbody2D>();
        }

        if (bossDamageable == null)
        {
            bossDamageable = GetComponent<PrototypeDamageable>();
        }

        if (bossAnimator == null)
        {
            bossAnimator = GetComponent<Animator>();
        }

        if (player != null)
        {
            playerController = player.GetComponent<heroscrip>();
            if (playerBody == null)
            {
                playerBody = player.GetComponent<Rigidbody2D>();
            }

            playerCollider = player.GetComponent<Collider2D>();
        }

        if (leftTailDamageable != null)
        {
            sharedTailHealth = leftTailDamageable.CurrentHealth;
            previousLeftTailHealth = leftTailDamageable.CurrentHealth;
            leftTailDamageable.HealthChanged += HandleLeftTailHealth;
        }

        if (rightTailDamageable != null)
        {
            if (sharedTailHealth <= 0)
            {
                sharedTailHealth = rightTailDamageable.CurrentHealth;
            }

            previousRightTailHealth = rightTailDamageable.CurrentHealth;
            rightTailDamageable.HealthChanged += HandleRightTailHealth;
        }

        if (topSpikeDamageables != null &&
            topSpikeDamageables.Length >= 2)
        {
            if (topSpikeDamageables[0] != null)
            {
                topSpikeDamageables[0].HealthChanged +=
                    HandleLeftTopSpikeHealth;
            }

            if (topSpikeDamageables[1] != null)
            {
                topSpikeDamageables[1].HealthChanged +=
                    HandleRightTopSpikeHealth;
            }
        }
    }

    private void Start()
    {
        sharedTailHealth = Mathf.Max(
            leftTailDamageable != null
                ? leftTailDamageable.CurrentHealth
                : 0,
            rightTailDamageable != null
                ? rightTailDamageable.CurrentHealth
                : 0);
        previousLeftTailHealth =
            leftTailDamageable != null
                ? leftTailDamageable.CurrentHealth
                : 0;
        previousRightTailHealth =
            rightTailDamageable != null
                ? rightTailDamageable.CurrentHealth
                : 0;

        if (bossBody != null)
        {
            bossBody.bodyType = RigidbodyType2D.Kinematic;
            bossBody.gravityScale = 0f;
            bossBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (laserVisual != null)
        {
            laserVisual.enabled = false;
        }

        LockTopSpikesToCeiling();
        StopTailAnimationAndHide(leftTail, leftTailCollider);
        StopTailAnimationAndHide(rightTail, rightTailCollider);

        if (movingPlatforms != null)
        {
            platformStartPositions = new Vector3[movingPlatforms.Length];
            movingPlatformBodies =
                new Rigidbody2D[movingPlatforms.Length];
            movingPlatformColliders =
                new Collider2D[movingPlatforms.Length];

            for (int i = 0; i < movingPlatforms.Length; i++)
            {
                if (movingPlatforms[i] != null)
                {
                    platformStartPositions[i] =
                        movingPlatforms[i].position;

                    Rigidbody2D platformBody =
                        movingPlatforms[i].GetComponent<Rigidbody2D>();
                    movingPlatformBodies[i] = platformBody;
                    movingPlatformColliders[i] =
                        movingPlatforms[i].GetComponent<Collider2D>();

                    if (platformBody != null)
                    {
                        platformBody.bodyType =
                            RigidbodyType2D.Kinematic;
                        platformBody.gravityScale = 0f;
                        platformBody.constraints =
                            RigidbodyConstraints2D.FreezeRotation;
                        platformBody.interpolation =
                            RigidbodyInterpolation2D.Interpolate;
                    }
                }
            }

            RestorePlatformsToStartPositions();
        }

        StartCoroutine(LaserRoutine());
        StartCoroutine(TailRoutine());
        StartCoroutine(BatRoutine());
    }

    private void Update()
    {
        if (bossDead)
        {
            return;
        }

        if (bossDamageable == null || !bossDamageable.IsAlive)
        {
            HandleBossDeath();
            return;
        }

        UpdatePhase();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        nextBodyDamageTime = 0f;
        DamageHeroOnContact(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        DamageHeroOnContact(collision.collider);
    }

    private void DamageHeroOnContact(Collider2D other)
    {
        if (bossDead ||
            bossDamageable == null ||
            !bossDamageable.IsAlive ||
            other == null ||
            Time.time < nextBodyDamageTime)
        {
            return;
        }

        heroscrip hero = other.GetComponentInParent<heroscrip>();
        if (hero == null)
        {
            return;
        }

        PrototypeDamageable target =
            hero.GetComponent<PrototypeDamageable>();
        if (target == null || target.Owner != TerrainOwner.Player)
        {
            return;
        }

        if (target.TryApplyDamage(
            bodyContactDamage,
            TerrainOwner.Boss,
            transform))
        {
            nextBodyDamageTime = Time.time + bodyDamageInterval;
        }
    }

    private void FixedUpdate()
    {
        if (bossDead ||
            player == null ||
            bossBody == null ||
            bossDamageable == null ||
            !bossDamageable.IsAlive)
        {
            return;
        }

        if (currentPhase == 2)
        {
            MovePlatforms();
        }

        if (laserActive)
        {
            bossBody.velocity = Vector2.zero;
            bossBody.MovePosition(
                new Vector2(bossBody.position.x, bossGroundY));
            return;
        }

        float distance = player.position.x - bossBody.position.x;
        float newX = bossBody.position.x;
        float activeStopDistance = laserPreparing
            ? Mathf.Min(stopDistance, laserWidth * 0.5f)
            : stopDistance;

        if (Mathf.Abs(distance) > activeStopDistance)
        {
            float direction = Mathf.Sign(distance);
            float trackingSpeed = moveSpeed;
            if (playerController != null)
            {
                trackingSpeed = Mathf.Min(
                    trackingSpeed,
                    Mathf.Abs(playerController.xSpeed));
            }

            newX += direction *
                Mathf.Max(0.1f, trackingSpeed) *
                Time.fixedDeltaTime;
        }

        newX = Mathf.Clamp(newX, leftBoundary, rightBoundary);
        bossBody.MovePosition(new Vector2(newX, bossGroundY));
    }

    private IEnumerator LaserRoutine()
    {
        while (!bossDead)
        {
            yield return new WaitForSeconds(laserInterval);

            if (bossDead ||
                bossDamageable == null ||
                !bossDamageable.IsAlive)
            {
                yield break;
            }

            laserPreparing = true;

            float aimElapsed = 0f;
            float aimDistance =
                Mathf.Max(0.1f, laserWidth * 0.5f);
            while (player != null &&
                   Mathf.Abs(
                       player.position.x - bossBody.position.x) >
                   aimDistance &&
                   aimElapsed < laserAimMaximumTime)
            {
                aimElapsed += Time.deltaTime;
                yield return null;
            }

            if (bossDead ||
                bossDamageable == null ||
                !bossDamageable.IsAlive)
            {
                laserPreparing = false;
                yield break;
            }

            if (bossAnimator != null)
            {
                bossAnimator.SetTrigger("ShootLine");
            }

            yield return new WaitForSeconds(laserTelegraphTime);

            laserPreparing = false;
            laserActive = true;
            damagedActorIds.Clear();
            damagedTerrainIds.Clear();

            float elapsed = 0f;
            while (elapsed < laserGrowTime)
            {
                elapsed += Time.deltaTime;
                float maximumDistance = GetLaserBlockDistance();
                float distance = maximumDistance *
                    Mathf.Clamp01(elapsed / laserGrowTime);
                ShowLaser(distance);
                DamageLaserPath(distance);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < laserDuration)
            {
                elapsed += Time.deltaTime;
                float distance = GetLaserBlockDistance();
                ShowLaser(distance);
                DamageLaserPath(distance);
                yield return null;
            }

            laserActive = false;
            if (laserVisual != null)
            {
                laserVisual.enabled = false;
            }
        }
    }

    private float GetLaserBlockDistance()
    {
        if (laserOrigin == null)
        {
            return laserMaximumDistance;
        }

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            laserOrigin.position,
            Vector2.up,
            laserMaximumDistance);

        float nearestDistance = laserMaximumDistance;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null ||
                hitCollider.transform == transform ||
                hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            TerrainSegment segment =
                hitCollider.GetComponentInParent<TerrainSegment>();
            TerrainEntity terrain = segment != null
                ? segment.ParentTerrain
                : hitCollider.GetComponentInParent<TerrainEntity>();

            bool isWall = terrain is FallingStoneWallTerrain;
            bool isTail =
                hitCollider == leftTailCollider ||
                hitCollider == rightTailCollider;

            if ((isWall || isTail) &&
                hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
            }
        }

        return Mathf.Max(0.1f, nearestDistance);
    }

    private void ShowLaser(float distance)
    {
        if (laserOrigin == null || laserVisual == null)
        {
            return;
        }

        laserVisual.transform.position =
            laserOrigin.position + Vector3.up * distance * 0.5f;
        laserVisual.transform.rotation = Quaternion.identity;

        Vector2 spriteSize = laserVisual.sprite != null
            ? laserVisual.sprite.bounds.size
            : Vector2.one;

        laserVisual.transform.localScale = new Vector3(
            laserWidth / Mathf.Max(0.01f, spriteSize.x),
            distance / Mathf.Max(0.01f, spriteSize.y),
            1f);
        laserVisual.enabled = true;
    }

    private void DamageLaserPath(float distance)
    {
        if (laserOrigin == null)
        {
            return;
        }

        Vector2 center =
            (Vector2)laserOrigin.position + Vector2.up * distance * 0.5f;
        if (distance < laserWidth)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCapsuleAll(
            center,
            new Vector2(laserWidth, distance),
            CapsuleDirection2D.Vertical,
            0f);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null ||
                hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            PrototypeDamageable actor =
                hit.GetComponentInParent<PrototypeDamageable>();
            if (actor != null &&
                actor.Owner == TerrainOwner.Player &&
                actor != bossDamageable &&
                damagedActorIds.Add(actor.GetInstanceID()))
            {
                actor.TryApplyDamage(
                    laserDamage,
                    TerrainOwner.Boss,
                    transform);
            }

            TerrainSegment segment =
                hit.GetComponentInParent<TerrainSegment>();
            TerrainEntity terrain = segment != null
                ? segment.ParentTerrain
                : hit.GetComponentInParent<TerrainEntity>();

            if (terrain == null ||
                terrain.IsBeingDestroyed ||
                terrainDamageService == null ||
                (segment == null && terrain is ITerrainSegmentHost))
            {
                continue;
            }

            int terrainTargetId = segment != null
                ? segment.GetInstanceID()
                : terrain.GetInstanceID();

            if (!damagedTerrainIds.Add(terrainTargetId))
            {
                continue;
            }

            if (segment != null)
            {
                terrainDamageService.TryDamageTerrain(
                    segment,
                    laserTerrainDamage,
                    TerrainOwner.Boss,
                    transform);
            }
            else
            {
                terrainDamageService.TryDamageTerrain(
                    terrain,
                    laserTerrainDamage,
                    TerrainOwner.Boss,
                    transform);
            }
        }
    }

    private IEnumerator TailRoutine()
    {
        int side = 0;

        while (!bossDead)
        {
            if (tailDefeated)
            {
                yield break;
            }

            GameObject tail = side == 0 ? leftTail : rightTail;
            Collider2D tailCollider =
                side == 0 ? leftTailCollider : rightTailCollider;
            GameObject otherTail = side == 0 ? rightTail : leftTail;
            Collider2D otherTailCollider =
                side == 0 ? rightTailCollider : leftTailCollider;
            Animator tailAnimator =
                tail != null ? tail.GetComponent<Animator>() : null;
            SpriteRenderer tailRenderer =
                tail != null ? tail.GetComponent<SpriteRenderer>() : null;

            HideTail(otherTail, otherTailCollider);

            if (tailAnimator == null ||
                tailAnimator.runtimeAnimatorController == null ||
                tailRenderer == null ||
                !tailAnimator.HasState(0, TailOutStateHash) ||
                !tailAnimator.HasState(0, TailInStateHash))
            {
                HideTail(tail, tailCollider);
                yield return new WaitForSeconds(tailSwitchDelay);
                side = 1 - side;
                continue;
            }

            yield return PlayTailAnimation(
                tailAnimator,
                tailRenderer,
                tailCollider,
                TailOutStateHash);

            if (tailDefeated || bossDead)
            {
                yield break;
            }

            if (tailCollider != null)
            {
                tailCollider.enabled = true;
            }

            yield return new WaitForSeconds(tailStayTime);

            if (tailDefeated || bossDead)
            {
                yield break;
            }

            if (tailCollider != null)
            {
                tailCollider.enabled = false;
            }

            yield return PlayTailAnimation(
                tailAnimator,
                tailRenderer,
                tailCollider,
                TailInStateHash);

            HideTail(tail, tailCollider);
            if (tailDefeated || bossDead)
            {
                yield break;
            }

            yield return new WaitForSeconds(tailSwitchDelay);
            side = 1 - side;
        }
    }

    private IEnumerator PlayTailAnimation(
        Animator animator,
        SpriteRenderer renderer,
        Collider2D tailCollider,
        int stateHash)
    {
        if (tailDefeated || bossDead)
        {
            yield break;
        }

        if (tailCollider != null)
        {
            tailCollider.enabled = false;
        }

        animator.enabled = true;
        renderer.enabled = true;
        animator.Play(stateHash, 0, 0f);
        animator.Update(0f);

        while (!tailDefeated && !bossDead)
        {
            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == stateHash &&
                state.normalizedTime >= 1f)
            {
                yield break;
            }

            yield return null;
        }
    }

    private static void HideTail(
        GameObject tail,
        Collider2D tailCollider)
    {
        if (tailCollider != null)
        {
            tailCollider.enabled = false;
        }

        if (tail != null)
        {
            SpriteRenderer renderer = tail.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    private static void StopTailAnimationAndHide(
        GameObject tail,
        Collider2D tailCollider)
    {
        if (tail != null)
        {
            Animator animator = tail.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        HideTail(tail, tailCollider);
    }

    private void HandleLeftTailHealth(int current, int maximum)
    {
        HandleTailHealth(
            0,
            current,
            ref previousLeftTailHealth);
    }

    private void HandleRightTailHealth(int current, int maximum)
    {
        HandleTailHealth(
            1,
            current,
            ref previousRightTailHealth);
    }

    private void HandleTailHealth(
        int damagedSide,
        int current,
        ref int previous)
    {
        int damageTaken = Mathf.Max(0, previous - current);
        previous = current;

        if (syncingTailHealth || damageTaken <= 0 || tailDefeated)
        {
            return;
        }

        sharedTailHealth = Mathf.Max(0, sharedTailHealth - damageTaken);

        if (bossDamageable != null && bossDamageable.IsAlive)
        {
            Transform attacker = damagedSide == 0 && leftTail != null
                ? leftTail.transform
                : rightTail != null
                    ? rightTail.transform
                    : transform;

            bossDamageable.TryApplyDamage(
                damageTaken,
                TerrainOwner.Player,
                attacker);
        }

        SyncOtherTail(damagedSide);

        if (sharedTailHealth <= 0)
        {
            DefeatTail();
        }
    }

    private void SyncOtherTail(int damagedSide)
    {
        PrototypeDamageable other =
            damagedSide == 0
                ? rightTailDamageable
                : leftTailDamageable;

        if (other == null || other.CurrentHealth <= sharedTailHealth)
        {
            return;
        }

        syncingTailHealth = true;
        other.TryApplyDamage(
            other.CurrentHealth - sharedTailHealth,
            TerrainOwner.Player,
            transform);
        syncingTailHealth = false;

        previousLeftTailHealth =
            leftTailDamageable != null
                ? leftTailDamageable.CurrentHealth
                : 0;
        previousRightTailHealth =
            rightTailDamageable != null
                ? rightTailDamageable.CurrentHealth
                : 0;
    }

    private void DefeatTail()
    {
        tailDefeated = true;
        StopTailAnimationAndHide(leftTail, leftTailCollider);
        StopTailAnimationAndHide(rightTail, rightTailCollider);
        currentPhase = 3;
        ShatterTerrainOnce();
    }

    private IEnumerator BatRoutine()
    {
        while (!bossDead)
        {
            yield return new WaitForSeconds(batSpawnInterval);

            if (currentPhase >= 2)
            {
                SpawnBat();
            }
        }
    }

    private void SpawnBat()
    {
        if (batTemplate == null || player == null)
        {
            return;
        }

        for (int i = spawnedBats.Count - 1; i >= 0; i--)
        {
            if (spawnedBats[i] == null)
            {
                spawnedBats.RemoveAt(i);
            }
        }

        if (spawnedBats.Count >= batMaximumCount)
        {
            return;
        }

        Transform spawnPoint = transform;
        if (batSpawnPoints != null && batSpawnPoints.Length > 0)
        {
            int index = Random.Range(0, batSpawnPoints.Length);
            if (batSpawnPoints[index] != null)
            {
                spawnPoint = batSpawnPoints[index];
            }
        }

        GameObject batObject = Instantiate(
            batTemplate,
            spawnPoint.position,
            Quaternion.identity);
        batObject.name = "SecondBossBat";
        batObject.SetActive(true);

        SecondBossBat bat = batObject.GetComponent<SecondBossBat>();
        if (bat != null)
        {
            bat.Initialize(
                player,
                batMoveSpeed,
                batDamage,
                batLifeTime);
            spawnedBats.Add(bat);
        }
    }

    private void MovePlatforms()
    {
        if (movingPlatforms == null ||
            platformStartPositions == null)
        {
            return;
        }

        float phaseElapsed =
            Mathf.Max(0f, Time.time - phaseTwoStartedAt);
        bool playerCarried = false;

        for (int i = 0; i < movingPlatforms.Length; i++)
        {
            Transform platform = movingPlatforms[i];
            if (platform == null || !platform.gameObject.activeSelf)
            {
                continue;
            }

            float direction = i % 2 == 0 ? 1f : -1f;
            float offset =
                Mathf.Sin(phaseElapsed * platformMoveSpeed) *
                platformMoveRange *
                direction;

            Vector3 position = platformStartPositions[i];
            position.y += offset;

            Collider2D platformCollider =
                movingPlatformColliders != null &&
                i < movingPlatformColliders.Length
                    ? movingPlatformColliders[i]
                    : null;
            bool carryPlayer =
                !playerCarried &&
                IsPlayerStandingOn(platformCollider);

            Rigidbody2D platformBody =
                movingPlatformBodies != null &&
                i < movingPlatformBodies.Length
                    ? movingPlatformBodies[i]
                    : null;
            Vector2 previousPosition =
                platformBody != null
                    ? platformBody.position
                    : (Vector2)platform.position;
            Vector2 targetPosition = position;
            Vector2 movement = targetPosition - previousPosition;

            if (platformBody != null)
            {
                platformBody.MovePosition(targetPosition);
            }
            else
            {
                platform.position = position;
            }

            if (carryPlayer &&
                playerBody != null &&
                movement.sqrMagnitude > 0f)
            {
                playerBody.position += movement;
                playerCarried = true;
            }
        }
    }

    private bool IsPlayerStandingOn(Collider2D platformCollider)
    {
        if (playerCollider == null ||
            playerCollider.isTrigger ||
            platformCollider == null ||
            !platformCollider.enabled)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        Bounds platformBounds = platformCollider.bounds;
        bool overlapsHorizontally =
            playerBounds.max.x > platformBounds.min.x &&
            playerBounds.min.x < platformBounds.max.x;
        float feetToPlatformTop =
            playerBounds.min.y - platformBounds.max.y;

        return overlapsHorizontally &&
               feetToPlatformTop >= -platformCarryTolerance &&
               feetToPlatformTop <= platformCarryTolerance;
    }

    private void RestorePlatformsToStartPositions()
    {
        if (movingPlatforms == null ||
            platformStartPositions == null)
        {
            return;
        }

        for (int i = 0; i < movingPlatforms.Length; i++)
        {
            if (movingPlatforms[i] != null)
            {
                Rigidbody2D platformBody =
                    movingPlatformBodies != null &&
                    i < movingPlatformBodies.Length
                        ? movingPlatformBodies[i]
                        : null;
                if (platformBody != null)
                {
                    platformBody.position =
                        platformStartPositions[i];
                }
                else
                {
                    movingPlatforms[i].position =
                        platformStartPositions[i];
                }
            }
        }
    }

    private void UpdatePhase()
    {
        if (bossDamageable == null ||
            bossDamageable.MaximumHealth <= 0)
        {
            return;
        }

        float healthRate =
            (float)bossDamageable.CurrentHealth /
            bossDamageable.MaximumHealth;

        int targetPhase = 1;
        if (healthRate <= 0.3f || tailDefeated)
        {
            targetPhase = 3;
        }
        else if (healthRate <= 0.7f)
        {
            targetPhase = 2;
        }

        if (targetPhase > currentPhase)
        {
            if (currentPhase < 2 && targetPhase == 2)
            {
                phaseTwoStartedAt = Time.time;
            }

            currentPhase = targetPhase;
        }

        if (currentPhase >= 3)
        {
            ShatterTerrainOnce();
        }
    }

    private void ShatterTerrainOnce()
    {
        if (hasShatteredTerrain)
        {
            return;
        }

        hasShatteredTerrain = true;

        if (terrainRegistry != null)
        {
            terrainRegistry.CopyActiveTerrain(terrainSnapshot);
            for (int i = 0; i < terrainSnapshot.Count; i++)
            {
                TerrainEntity terrain = terrainSnapshot[i];
                if (terrain != null && !terrain.IsBeingDestroyed)
                {
                    terrain.DestroyTerrain(true);
                }
            }
        }

        if (platformsToShatter != null)
        {
            for (int i = 0; i < platformsToShatter.Length; i++)
            {
                if (platformsToShatter[i] != null)
                {
                    platformsToShatter[i].SetActive(false);
                }
            }
        }

        BreakTopSpike(0);
        BreakTopSpike(1);
    }

    private void HandleLeftTopSpikeHealth(int current, int maximum)
    {
        if (current < maximum)
        {
            DropTopSpike(0);
        }
    }

    private void HandleRightTopSpikeHealth(int current, int maximum)
    {
        if (current < maximum)
        {
            DropTopSpike(1);
        }
    }

    private void LockTopSpikesToCeiling()
    {
        for (int i = 0; i < topSpikeDropped.Length; i++)
        {
            topSpikeDropped[i] = false;

            if (topSpikeBodies != null &&
                i < topSpikeBodies.Length &&
                topSpikeBodies[i] != null)
            {
                Rigidbody2D body = topSpikeBodies[i];
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            if (grappleHooks != null &&
                i < grappleHooks.Length &&
                grappleHooks[i] != null)
            {
                grappleHooks[i].SetActive(false);
            }
        }
    }

    private void BreakTopSpike(int index)
    {
        if (topSpikeDamageables == null ||
            index < 0 ||
            index >= topSpikeDamageables.Length)
        {
            return;
        }

        PrototypeDamageable spike = topSpikeDamageables[index];
        if (spike != null && spike.IsAlive)
        {
            spike.TryApplyDamage(
                spike.CurrentHealth,
                TerrainOwner.Boss,
                transform);
        }
        else
        {
            DropTopSpike(index);
        }
    }

    private void DropTopSpike(int index)
    {
        if (index < 0 || index >= topSpikeDropped.Length ||
            topSpikeDropped[index])
        {
            return;
        }

        topSpikeDropped[index] = true;

        if (topSpikeBodies != null &&
            index < topSpikeBodies.Length &&
            topSpikeBodies[index] != null)
        {
            Rigidbody2D body = topSpikeBodies[index];
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 3f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;
            body.WakeUp();

            StartCoroutine(WatchDroppedTopSpike(index, body));
        }

        if (grappleHooks != null &&
            index < grappleHooks.Length &&
            grappleHooks[index] != null)
        {
            grappleHooks[index].SetActive(true);
        }
    }

    private IEnumerator WatchDroppedTopSpike(
        int index,
        Rigidbody2D body)
    {
        yield return new WaitForSeconds(0.1f);

        Collider2D spikeCollider = body != null
            ? body.GetComponent<Collider2D>()
            : null;
        if (spikeCollider == null)
        {
            yield break;
        }

        Collider2D[] contacts = new Collider2D[8];
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();

        while (body != null && spikeCollider != null)
        {
            int count = spikeCollider.GetContacts(filter, contacts);
            if (ConsumeDroppedTopSpikeContact(
                index,
                body,
                contacts,
                count))
            {
                yield break;
            }

            count = spikeCollider.OverlapCollider(filter, contacts);
            if (ConsumeDroppedTopSpikeContact(
                index,
                body,
                contacts,
                count))
            {
                yield break;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    private bool ConsumeDroppedTopSpikeContact(
        int index,
        Rigidbody2D body,
        Collider2D[] contacts,
        int count)
    {
        for (int i = 0; i < count; i++)
        {
            Collider2D other = contacts[i];
            if (ShouldIgnoreTopSpikeContact(index, other))
            {
                continue;
            }

            PrototypeDamageable target =
                other.GetComponentInParent<PrototypeDamageable>();
            if (target != null &&
                target.Owner == TerrainOwner.Player)
            {
                target.TryApplyDamage(
                    topSpikeImpactDamage,
                    TerrainOwner.Boss,
                    body.transform);
            }

            Destroy(body.gameObject);
            return true;
        }

        return false;
    }

    private bool ShouldIgnoreTopSpikeContact(
        int index,
        Collider2D other)
    {
        if (other == null)
        {
            return true;
        }

        if (topSpikes != null &&
            index < topSpikes.Length &&
            topSpikes[index] != null &&
            (other.transform == topSpikes[index].transform ||
             other.transform.IsChildOf(topSpikes[index].transform)))
        {
            return true;
        }

        return grappleHooks != null &&
               index < grappleHooks.Length &&
               grappleHooks[index] != null &&
               (other.transform == grappleHooks[index].transform ||
                other.transform.IsChildOf(grappleHooks[index].transform));
    }

    private void HandleBossDeath()
    {
        if (bossDead)
        {
            return;
        }

        bossDead = true;
        laserPreparing = false;
        laserActive = false;
        StopAllCoroutines();

        if (bossBody != null)
        {
            bossBody.velocity = Vector2.zero;
        }

        if (laserVisual != null)
        {
            laserVisual.enabled = false;
        }

        StopTailAnimationAndHide(leftTail, leftTailCollider);
        StopTailAnimationAndHide(rightTail, rightTailCollider);

        for (int i = 0; i < spawnedBats.Count; i++)
        {
            if (spawnedBats[i] != null)
            {
                Destroy(spawnedBats[i].gameObject);
            }
        }

        spawnedBats.Clear();
    }

    private void OnDisable()
    {
        if (leftTailDamageable != null)
        {
            leftTailDamageable.HealthChanged -= HandleLeftTailHealth;
        }

        if (rightTailDamageable != null)
        {
            rightTailDamageable.HealthChanged -= HandleRightTailHealth;
        }

        if (topSpikeDamageables != null &&
            topSpikeDamageables.Length >= 2)
        {
            if (topSpikeDamageables[0] != null)
            {
                topSpikeDamageables[0].HealthChanged -=
                    HandleLeftTopSpikeHealth;
            }

            if (topSpikeDamageables[1] != null)
            {
                topSpikeDamageables[1].HealthChanged -=
                    HandleRightTopSpikeHealth;
            }
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        player = FindSceneObject("Hero")?.transform;
        playerBody = player != null
            ? player.GetComponent<Rigidbody2D>()
            : null;

        bossBody = GetComponent<Rigidbody2D>();
        bossBody.bodyType = RigidbodyType2D.Kinematic;
        bossBody.gravityScale = 0f;
        bossBody.constraints = RigidbodyConstraints2D.FreezeRotation;

        bossAnimator = GetComponent<Animator>();
        bossDamageable = GetComponent<PrototypeDamageable>();
        ConfigureDamageable(
            bossDamageable,
            TerrainOwner.Boss,
            500);

        TerrainRegistry registry =
            FindObjectOfType<TerrainRegistry>();
        TerrainDamageService damageService =
            FindObjectOfType<TerrainDamageService>();
        terrainRegistry = registry;
        terrainDamageService = damageService;

        GameObject roomRoot =
            FindSceneObject("SecondBossRoomObjects");
        if (roomRoot == null)
        {
            roomRoot = new GameObject("SecondBossRoomObjects");
        }

        CreateLaserObjects(roomRoot.transform);
        CreateTailObjects(roomRoot.transform);
        CreateTopSpikeAndHookObjects(roomRoot.transform);
        ConfigureBatTemplate();
        BindPlatforms();
        ConfigureGround();
        BindBossBar();
        BindHeroBar();

        bossGroundY = transform.position.y;
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    private void CreateLaserObjects(Transform roomRoot)
    {
        GameObject originObject = FindSceneObject("SecondBossLaserOrigin");
        if (originObject == null)
        {
            originObject = new GameObject("SecondBossLaserOrigin");
        }

        originObject.transform.SetParent(transform);
        originObject.transform.localPosition = new Vector3(0f, 0f, 0f);
        originObject.transform.localRotation = Quaternion.identity;
        laserOrigin = originObject.transform;

        GameObject visualObject = FindSceneObject("SecondBossLaserVisual");
        if (visualObject == null)
        {
            visualObject = new GameObject("SecondBossLaserVisual");
        }

        visualObject.transform.SetParent(roomRoot);
        laserVisual = GetOrAdd<SpriteRenderer>(visualObject);
        laserVisual.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/" +
            "DefaultAssets/Textures/v2/Capsule.png");
        if (laserVisual.sprite == null)
        {
            laserVisual.sprite =
                Resources.Load<Sprite>("Art/PrototypeSquare");
        }

        laserVisual.color = Color.red;
        laserVisual.sortingOrder = 20;
        laserVisual.enabled = false;
    }

    private void CreateTailObjects(Transform roomRoot)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(
            "Art/Boss/e53d0e5d-f973-4b4c-9dc2-345cc35a6270");
        GameObject existingRightTail = FindSceneObject("Tail");
        Sprite tailSprite = sprites.Length > 2
            ? sprites[2]
            : sprites.Length > 0
                ? sprites[sprites.Length - 1]
                : null;

        leftTail = CreateTail(
            "SecondBossLeftTail",
            roomRoot,
            new Vector3(-35f, -28.8f, 0f),
            0f,
            false,
            tailSprite);

        if (existingRightTail != null)
        {
            rightTail = ConfigureExistingTail(
                existingRightTail,
                roomRoot,
                tailSprite);

            GameObject generatedDuplicate =
                FindSceneObject("SecondBossRightTail");
            if (generatedDuplicate != null &&
                generatedDuplicate != rightTail)
            {
                DestroyImmediate(generatedDuplicate);
            }
        }
        else
        {
            rightTail = CreateTail(
                "SecondBossRightTail",
                roomRoot,
                new Vector3(35f, -28.8f, 0f),
                0f,
                true,
                tailSprite);
        }

        leftTailCollider = leftTail.GetComponent<Collider2D>();
        rightTailCollider = rightTail.GetComponent<Collider2D>();
        leftTailDamageable =
            leftTail.GetComponent<PrototypeDamageable>();
        rightTailDamageable =
            rightTail.GetComponent<PrototypeDamageable>();
    }

    private GameObject CreateTail(
        string objectName,
        Transform roomRoot,
        Vector3 position,
        float rotationZ,
        bool flipX,
        Sprite sprite)
    {
        GameObject tail = FindSceneObject(objectName);
        if (tail == null)
        {
            tail = new GameObject(objectName);
        }

        tail.transform.SetParent(roomRoot);
        tail.transform.position = position;
        tail.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
        tail.transform.localScale = new Vector3(3f, 3f, 1f);
        tail.layer = LayerMask.NameToLayer("Ground");

        SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(tail);
        renderer.sprite = sprite;
        renderer.flipX = flipX;
        renderer.sortingOrder = 4;

        BoxCollider2D collider = GetOrAdd<BoxCollider2D>(tail);
        collider.isTrigger = false;

        PrototypeDamageable damageable =
            GetOrAdd<PrototypeDamageable>(tail);
        ConfigureDamageable(
            damageable,
            TerrainOwner.Boss,
            150);
        ConfigureTailAnimator(tail);

        return tail;
    }

    private GameObject ConfigureExistingTail(
        GameObject tail,
        Transform roomRoot,
        Sprite sprite)
    {
        tail.transform.SetParent(roomRoot);
        tail.transform.position = new Vector3(35f, -28.8f, 0f);
        tail.transform.rotation = Quaternion.identity;
        tail.transform.localScale = new Vector3(3f, 3f, 1f);
        tail.layer = LayerMask.NameToLayer("Ground");

        SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(tail);
        renderer.sprite = sprite;
        renderer.sortingOrder = 4;

        BoxCollider2D collider = GetOrAdd<BoxCollider2D>(tail);
        collider.isTrigger = false;

        PrototypeDamageable damageable =
            GetOrAdd<PrototypeDamageable>(tail);
        ConfigureDamageable(
            damageable,
            TerrainOwner.Boss,
            150);
        ConfigureTailAnimator(tail);

        return tail;
    }

    private void CreateTopSpikeAndHookObjects(Transform roomRoot)
    {
        Sprite spikeSprite = LoadDownwardSpikeSprite();

        topSpikes = new GameObject[2];
        topSpikeDamageables = new PrototypeDamageable[2];
        topSpikeBodies = new Rigidbody2D[2];
        grappleHooks = new GameObject[2];

        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -27f : 27f;
            string side = i == 0 ? "Left" : "Right";

            GameObject spike =
                FindSceneObject("SecondBossTopSpike" + side);
            if (spike == null)
            {
                spike = new GameObject("SecondBossTopSpike" + side);
            }

            spike.transform.SetParent(roomRoot);
            spike.transform.position = new Vector3(x, 39f, 0f);
            spike.transform.rotation = Quaternion.identity;
            spike.transform.localScale = new Vector3(2.1f, 2.1f, 1f);
            spike.layer = LayerMask.NameToLayer("Breakable");

            SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(spike);
            renderer.sprite = spikeSprite;
            renderer.sortingOrder = 5;

            BoxCollider2D spikeCollider =
                GetOrAdd<BoxCollider2D>(spike);
            spikeCollider.isTrigger = false;
            if (spikeSprite != null)
            {
                spikeCollider.offset = spikeSprite.bounds.center;
                spikeCollider.size = spikeSprite.bounds.size;
            }

            Rigidbody2D body = GetOrAdd<Rigidbody2D>(spike);
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeAll;

            PrototypeDamageable damageable =
                GetOrAdd<PrototypeDamageable>(spike);
            ConfigureDamageable(
                damageable,
                TerrainOwner.Neutral,
                40);

            GameObject hook =
                FindSceneObject("SecondBossHook" + side);
            if (hook == null)
            {
                hook = new GameObject("SecondBossHook" + side);
            }

            hook.transform.SetParent(roomRoot);
            hook.transform.position = new Vector3(x, 39f, 0f);
            hook.transform.rotation = Quaternion.identity;
            hook.transform.localScale = new Vector3(3f, 3f, 1f);

            SpriteRenderer hookRenderer = GetOrAdd<SpriteRenderer>(hook);
            hookRenderer.sprite =
                Resources.Load<Sprite>("Art/PrototypeSquare");
            hookRenderer.color = new Color(1f, 0.85f, 0.1f, 1f);
            hookRenderer.sortingOrder = 10;

            CircleCollider2D hookCollider =
                GetOrAdd<CircleCollider2D>(hook);
            hookCollider.isTrigger = true;

            GrappleHook grapple = GetOrAdd<GrappleHook>(hook);
            SerializedObject grappleSerialized =
                new SerializedObject(grapple);
            grappleSerialized.FindProperty("player").objectReferenceValue =
                player;
            grappleSerialized.FindProperty("playerBody").objectReferenceValue =
                playerBody;
            grappleSerialized.FindProperty("hero").objectReferenceValue =
                player != null
                    ? player.GetComponent<heroscrip>()
                    : null;
            grappleSerialized.ApplyModifiedPropertiesWithoutUndo();

            hook.SetActive(false);

            topSpikes[i] = spike;
            topSpikeDamageables[i] = damageable;
            topSpikeBodies[i] = body;
            grappleHooks[i] = hook;
        }
    }

    private static void ConfigureTailAnimator(GameObject tail)
    {
        Animator animator = GetOrAdd<Animator>(tail);
        animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Animation/Boss/Tail.controller");
        animator.applyRootMotion = false;
    }

    private static Sprite LoadDownwardSpikeSprite()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(
            "Assets/Resources/Art/PFC_props1.png");
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite sprite = assets[i] as Sprite;
            if (sprite != null && sprite.name == "CaveDownwardSpike")
            {
                return sprite;
            }
        }

        return null;
    }

    private void ConfigureBatTemplate()
    {
        batTemplate = FindSceneObject("Bat");
        if (batTemplate == null)
        {
            return;
        }

        Rigidbody2D body = GetOrAdd<Rigidbody2D>(batTemplate);
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        CircleCollider2D collider =
            GetOrAdd<CircleCollider2D>(batTemplate);
        collider.isTrigger = true;
        GetOrAdd<SecondBossBat>(batTemplate);
        ConfigureDamageable(
            GetOrAdd<PrototypeDamageable>(batTemplate),
            TerrainOwner.Boss,
            1);
        batTemplate.layer = LayerMask.NameToLayer("Ignore Raycast");

        batSpawnPoints = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            string objectName =
                i == 0 ? "BatSpawnLeft" : "BatSpawnRight";
            GameObject spawn = FindSceneObject(objectName);
            if (spawn == null)
            {
                spawn = new GameObject(objectName);
            }

            spawn.transform.SetParent(transform);
            spawn.transform.localPosition =
                new Vector3(i == 0 ? -3f : 3f, 2f, 0f);
            spawn.transform.localRotation = Quaternion.identity;
            batSpawnPoints[i] = spawn.transform;
        }

        batTemplate.SetActive(false);
    }

    private void BindPlatforms()
    {
        string[] platformNames =
        {
            "platforn",
            "platform1",
            "platform2",
            "platform3",
            "platform4"
        };

        movingPlatforms = new Transform[platformNames.Length];
        platformsToShatter = new GameObject[platformNames.Length];

        for (int i = 0; i < platformNames.Length; i++)
        {
            GameObject platform = FindSceneObject(platformNames[i]);
            if (platform != null)
            {
                Rigidbody2D platformBody =
                    GetOrAdd<Rigidbody2D>(platform);
                platformBody.bodyType = RigidbodyType2D.Kinematic;
                platformBody.gravityScale = 0f;
                platformBody.constraints =
                    RigidbodyConstraints2D.FreezeRotation;
                platformBody.interpolation =
                    RigidbodyInterpolation2D.Interpolate;

                movingPlatforms[i] = platform.transform;
                platformsToShatter[i] = platform;
            }
        }
    }

    private void ConfigureGround()
    {
        GameObject ground = FindSceneObject("Square_bottom");
        if (ground != null)
        {
            GetOrAdd<BossRoomGround>(ground);
        }
    }

    private void BindBossBar()
    {
        BossBarScrip bossBar = FindObjectOfType<BossBarScrip>();
        if (bossBar == null)
        {
            return;
        }

        SerializedObject serializedBar =
            new SerializedObject(bossBar);
        serializedBar.FindProperty("observedDamageable")
            .objectReferenceValue = bossDamageable;
        serializedBar.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bossBar);
    }

    private void BindHeroBar()
    {
        HeroBarScrip heroBar = FindObjectOfType<HeroBarScrip>();
        PrototypeDamageable heroDamageable =
            player != null
                ? player.GetComponent<PrototypeDamageable>()
                : null;
        if (heroBar == null || heroDamageable == null)
        {
            return;
        }

        SerializedObject serializedBar =
            new SerializedObject(heroBar);
        serializedBar.FindProperty("observedDamageable")
            .objectReferenceValue = heroDamageable;
        serializedBar.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(heroBar);
    }

    private static void ConfigureDamageable(
        PrototypeDamageable damageable,
        TerrainOwner owner,
        int maximumHealth)
    {
        if (damageable == null)
        {
            return;
        }

        SerializedObject serialized =
            new SerializedObject(damageable);
        serialized.FindProperty("_owner").enumValueIndex = (int)owner;
        serialized.FindProperty("_maximumHealth").intValue =
            maximumHealth;
        serialized.FindProperty("_spriteRenderer").objectReferenceValue =
            damageable.GetComponent<SpriteRenderer>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(damageable);
    }

    private static T GetOrAdd<T>(GameObject target)
        where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null
            ? component
            : target.AddComponent<T>();
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Transform[] transforms =
            Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null &&
                candidate.name == objectName &&
                candidate.gameObject.scene.IsValid())
            {
                return candidate.gameObject;
            }
        }

        return null;
    }
#endif
}
