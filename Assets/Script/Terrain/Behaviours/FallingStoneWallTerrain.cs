using System.Collections.Generic;
using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class FallingStoneWallTerrain : TerrainEntity, ITerrainSegmentHost
    {
        private const int SegmentCount = 10;
        private const float VisualOverlapWorldUnits = 0.015f;

        [Header("Impact Damage")]
        [SerializeField, Min(0)] private int _impactDamage = 10;
        [SerializeField, Min(0f)] private float _impactSpeedThreshold = 4.5f;
        [SerializeField, Min(0.05f)] private float _perTargetCooldown = 0.75f;

        private readonly Dictionary<int, float> _nextDamageTimeByTarget = new Dictionary<int, float>();
        private readonly GameObject[] _segmentObjects =
            new GameObject[SegmentCount];
        private readonly int[] _segmentHealth =
            new int[SegmentCount];
        // 断开后生成的刚体都记在这里，石墙销毁时一起清掉。
        private readonly List<GameObject> _bodyGroups =
            new List<GameObject>(SegmentCount);

        private int _activeSegmentCount;
        private Rigidbody2D _rootBody;
        private Rigidbody2D _followBody;
        private bool _hasBrokenApart;

        public TerrainEntity TerrainEntity => this;

        protected override void Awake()
        {
            base.Awake();
            _rootBody = GetComponent<Rigidbody2D>();
            CreateSegments();
        }

        private void LateUpdate()
        {
            if (!_hasBrokenApart || _followBody == null)
            {
                return;
            }

            // 2026-07-19：根对象只负责登记，位置跟着其中一块走。
            transform.SetPositionAndRotation(
                _followBody.transform.position,
                _followBody.transform.rotation);
        }

        public int GetSegmentCurrentHealth(int segmentIndex)
        {
            return IsValidSegment(segmentIndex)
                ? _segmentHealth[segmentIndex]
                : 0;
        }

        public bool TryApplySegmentDamage(
            int segmentIndex,
            int amount,
            TerrainOwner attacker,
            Transform attackerTransform)
        {
            if (IsBeingDestroyed ||
                amount <= 0 ||
                !IsValidSegment(segmentIndex) ||
                _segmentObjects[segmentIndex] == null)
            {
                return false;
            }

            // 2026-07-19：石墙也改成命中一次只拆掉当前这一段。
            _segmentHealth[segmentIndex] = 0;
            DestroySegment(segmentIndex);

            return true;
        }

        private void CreateSegments()
        {
            SpriteRenderer sourceRenderer =
                GetComponent<SpriteRenderer>();
            BoxCollider2D sourceCollider =
                GetComponent<BoxCollider2D>();

            if (sourceRenderer == null ||
                sourceRenderer.sprite == null ||
                sourceCollider == null)
            {
                Debug.LogError(
                    "Falling Stone Wall requires a root SpriteRenderer " +
                    "and BoxCollider2D for ten-segment construction.",
                    this);
                return;
            }

            Vector3 worldScale = transform.lossyScale;
            float worldWidth =
                sourceCollider.size.x * Mathf.Abs(worldScale.x);
            float worldHeight =
                sourceCollider.size.y * Mathf.Abs(worldScale.y);

            // 2026-07-19：按石墙自己的长轴切，旋转后子段会继续跟着根刚体走。
            bool splitAlongLocalX = worldWidth >= worldHeight;
            float longAxisSize = splitAlongLocalX
                ? sourceCollider.size.x
                : sourceCollider.size.y;
            float segmentLength = longAxisSize / SegmentCount;
            float longAxisOffset = splitAlongLocalX
                ? sourceCollider.offset.x
                : sourceCollider.offset.y;
            float start = longAxisOffset - longAxisSize * 0.5f;
            float worldAxisScale = splitAlongLocalX
                ? Mathf.Abs(worldScale.x)
                : Mathf.Abs(worldScale.y);
            float visualOverlapLocal = worldAxisScale > 0.0001f
                ? VisualOverlapWorldUnits / worldAxisScale
                : 0f;
            float spriteAxisSize = splitAlongLocalX
                ? sourceRenderer.sprite.bounds.size.x
                : sourceRenderer.sprite.bounds.size.y;

            if (segmentLength <= 0f || spriteAxisSize <= 0.0001f)
            {
                Debug.LogError(
                    "Falling Stone Wall has an invalid Sprite or Collider size.",
                    this);
                return;
            }

            for (int i = 0; i < SegmentCount; i++)
            {
                float center =
                    start + (i + 0.5f) * segmentLength;
                Vector2 localCenter = sourceCollider.offset;
                if (splitAlongLocalX)
                {
                    localCenter.x = center;
                }
                else
                {
                    localCenter.y = center;
                }

                GameObject segmentObject =
                    new GameObject($"Segment {i + 1:00}");
                segmentObject.layer = gameObject.layer;
                segmentObject.tag = gameObject.tag;
                segmentObject.transform.SetParent(transform, false);
                segmentObject.transform.localPosition = localCenter;

                Vector2 segmentSize = sourceCollider.size;
                if (splitAlongLocalX)
                {
                    segmentSize.x = segmentLength;
                }
                else
                {
                    segmentSize.y = segmentLength;
                }

                BoxCollider2D segmentCollider =
                    segmentObject.AddComponent<BoxCollider2D>();
                segmentCollider.size = segmentSize;
                segmentCollider.offset = Vector2.zero;
                segmentCollider.sharedMaterial =
                    sourceCollider.sharedMaterial;
                segmentCollider.isTrigger = sourceCollider.isTrigger;
                segmentCollider.usedByEffector =
                    sourceCollider.usedByEffector;
                segmentCollider.usedByComposite =
                    sourceCollider.usedByComposite;
                segmentCollider.density = sourceCollider.density;
                segmentCollider.edgeRadius =
                    sourceCollider.edgeRadius;

                TerrainSegment segment =
                    segmentObject.AddComponent<TerrainSegment>();
                segment.Initialize(this, i, segmentCollider);

                GameObject visualObject =
                    new GameObject("Visual");
                visualObject.layer = gameObject.layer;
                visualObject.tag = gameObject.tag;
                visualObject.transform.SetParent(
                    segmentObject.transform,
                    false);

                Vector3 visualScale = Vector3.one;
                float visualAxisScale =
                    (segmentLength + visualOverlapLocal) /
                    spriteAxisSize;
                if (splitAlongLocalX)
                {
                    visualScale.x = visualAxisScale;
                }
                else
                {
                    visualScale.y = visualAxisScale;
                }

                visualObject.transform.localScale = visualScale;

                SpriteRenderer segmentRenderer =
                    visualObject.AddComponent<SpriteRenderer>();
                CopyRendererSettings(
                    sourceRenderer,
                    segmentRenderer);

                _segmentObjects[i] = segmentObject;
                _segmentHealth[i] = MaximumHealth;
            }

            _activeSegmentCount = SegmentCount;

            // 根碰撞只保留完整外框，真实碰撞由十段子碰撞负责。
            sourceCollider.isTrigger = true;
            sourceRenderer.enabled = false;
        }

        private void DestroySegment(int segmentIndex)
        {
            GameObject segmentObject =
                _segmentObjects[segmentIndex];
            if (segmentObject != null)
            {
                _segmentObjects[segmentIndex] = null;
                _activeSegmentCount--;
                segmentObject.SetActive(false);
                Destroy(segmentObject);
            }

            if (_activeSegmentCount == 0)
            {
                CleanupBodyGroups();
                DestroyTerrain(true);
                return;
            }

            RebuildBodyGroups();
        }

        private void RebuildBodyGroups()
        {
            // 2026-07-19：先记住每段原来跟随的刚体状态，再重新分组。
            BodyState[] states = new BodyState[SegmentCount];
            bool[] hasState = new bool[SegmentCount];

            for (int i = 0; i < SegmentCount; i++)
            {
                GameObject segmentObject = _segmentObjects[i];
                if (segmentObject == null)
                {
                    continue;
                }

                Collider2D segmentCollider =
                    segmentObject.GetComponent<Collider2D>();
                Rigidbody2D sourceBody = segmentCollider != null
                    ? segmentCollider.attachedRigidbody
                    : null;

                if (sourceBody != null)
                {
                    states[i] = BodyState.Capture(sourceBody);
                    hasState[i] = true;
                }

                segmentObject.transform.SetParent(null, true);
            }

            CleanupBodyGroups(false);
            if (_rootBody != null)
            {
                _rootBody.simulated = false;
            }

            _hasBrokenApart = true;
            _followBody = null;

            // 相邻而且没被打掉的段算作同一块。
            int startIndex = 0;
            while (startIndex < SegmentCount)
            {
                while (startIndex < SegmentCount &&
                       _segmentObjects[startIndex] == null)
                {
                    startIndex++;
                }

                if (startIndex >= SegmentCount)
                {
                    break;
                }

                int endIndex = startIndex;
                while (endIndex + 1 < SegmentCount &&
                       _segmentObjects[endIndex + 1] != null)
                {
                    endIndex++;
                }

                BodyState state = hasState[startIndex]
                    ? states[startIndex]
                    : BodyState.Capture(_rootBody);
                Rigidbody2D groupBody = CreateBodyGroup(
                    startIndex,
                    endIndex,
                    state);

                if (_followBody == null)
                {
                    _followBody = groupBody;
                }

                startIndex = endIndex + 1;
            }
        }

        private Rigidbody2D CreateBodyGroup(
            int startIndex,
            int endIndex,
            BodyState state)
        {
            // 新刚体不挂在石墙根节点下，否则还是会跟着根节点一起动。
            GameObject groupObject = new GameObject(
                $"Falling Wall Body {startIndex + 1:00}-{endIndex + 1:00}");
            groupObject.layer = gameObject.layer;
            groupObject.tag = gameObject.tag;
            groupObject.transform.SetPositionAndRotation(
                state.Position,
                state.Rotation);

            Rigidbody2D groupBody =
                groupObject.AddComponent<Rigidbody2D>();
            state.ApplySettings(groupBody);

            FallingStoneWallBodyRelay relay =
                groupObject.AddComponent<FallingStoneWallBodyRelay>();
            relay.Initialize(this);

            for (int i = startIndex; i <= endIndex; i++)
            {
                _segmentObjects[i].transform.SetParent(
                    groupObject.transform,
                    true);
            }

            // 每块继承断开前这一点的运动，掉落时不会突然停一下。
            Vector2 newCenter = groupBody.worldCenterOfMass;
            groupBody.velocity = state.GetPointVelocity(newCenter);
            groupBody.angularVelocity = state.AngularVelocity;
            groupBody.WakeUp();

            _bodyGroups.Add(groupObject);
            return groupBody;
        }

        private void CleanupBodyGroups(bool disableColliders = false)
        {
            // 先停掉物理，避免 Destroy 真正执行前又发生一次碰撞。
            for (int i = 0; i < _bodyGroups.Count; i++)
            {
                GameObject groupObject = _bodyGroups[i];
                if (groupObject == null)
                {
                    continue;
                }

                Rigidbody2D body = groupObject.GetComponent<Rigidbody2D>();
                if (body != null)
                {
                    body.simulated = false;
                }

                if (disableColliders)
                {
                    Collider2D[] colliders =
                        groupObject.GetComponentsInChildren<Collider2D>(true);
                    for (int j = 0; j < colliders.Length; j++)
                    {
                        colliders[j].enabled = false;
                    }
                }

                Destroy(groupObject);
            }

            _bodyGroups.Clear();
        }

        private bool IsValidSegment(int segmentIndex)
        {
            return segmentIndex >= 0 &&
                   segmentIndex < SegmentCount;
        }

        private static void CopyRendererSettings(
            SpriteRenderer source,
            SpriteRenderer destination)
        {
            destination.sprite = source.sprite;
            destination.color = source.color;
            destination.sharedMaterial = source.sharedMaterial;
            destination.flipX = source.flipX;
            destination.flipY = source.flipY;
            destination.sortingLayerID = source.sortingLayerID;
            destination.sortingOrder = source.sortingOrder;
            destination.maskInteraction = source.maskInteraction;
            destination.spriteSortPoint = source.spriteSortPoint;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleBodyCollision(collision);
        }

        internal void HandleBodyCollision(Collision2D collision)
        {
            if (IsBeingDestroyed || collision.relativeVelocity.magnitude < _impactSpeedThreshold)
            {
                return;
            }

            PrototypeDamageable target = collision.collider.GetComponentInParent<PrototypeDamageable>();
            if (!CanDamageTarget(target))
            {
                return;
            }

            int targetId = target.GetInstanceID();
            if (_nextDamageTimeByTarget.TryGetValue(targetId, out float nextDamageTime) && Time.time < nextDamageTime)
            {
                return;
            }

            if (target.TryApplyDamage(_impactDamage, Owner, Creator))
            {
                _nextDamageTimeByTarget[targetId] = Time.time + _perTargetCooldown;
            }
        }

        protected override void OnDestroy()
        {
            // 分出去的块已经不在根节点下面，需要单独清理。
            CleanupBodyGroups(true);
            base.OnDestroy();
        }

        // 保存断开瞬间的刚体数据，新块继续沿原来的方向运动。
        private struct BodyState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public RigidbodyType2D BodyType;
            public float Mass;
            public bool UseAutoMass;
            public float Drag;
            public float AngularDrag;
            public float GravityScale;
            public RigidbodyInterpolation2D Interpolation;
            public RigidbodySleepMode2D SleepMode;
            public CollisionDetectionMode2D CollisionDetectionMode;
            public RigidbodyConstraints2D Constraints;
            public bool Simulated;
            public Vector2 Velocity;
            public float AngularVelocity;
            public Vector2 WorldCenterOfMass;

            public static BodyState Capture(Rigidbody2D body)
            {
                if (body == null)
                {
                    return default;
                }

                return new BodyState
                {
                    Position = body.transform.position,
                    Rotation = body.transform.rotation,
                    BodyType = body.bodyType,
                    Mass = body.mass,
                    UseAutoMass = body.useAutoMass,
                    Drag = body.drag,
                    AngularDrag = body.angularDrag,
                    GravityScale = body.gravityScale,
                    Interpolation = body.interpolation,
                    SleepMode = body.sleepMode,
                    CollisionDetectionMode = body.collisionDetectionMode,
                    Constraints = body.constraints,
                    Simulated = body.simulated,
                    Velocity = body.velocity,
                    AngularVelocity = body.angularVelocity,
                    WorldCenterOfMass = body.worldCenterOfMass
                };
            }

            public void ApplySettings(Rigidbody2D body)
            {
                body.bodyType = BodyType;
                body.useAutoMass = UseAutoMass;
                if (!UseAutoMass)
                {
                    body.mass = Mass;
                }

                body.drag = Drag;
                body.angularDrag = AngularDrag;
                body.gravityScale = GravityScale;
                body.interpolation = Interpolation;
                body.sleepMode = SleepMode;
                body.collisionDetectionMode = CollisionDetectionMode;
                body.constraints = Constraints;
                body.simulated = Simulated;
            }

            public Vector2 GetPointVelocity(Vector2 worldPoint)
            {
                float radiansPerSecond =
                    AngularVelocity * Mathf.Deg2Rad;
                Vector2 offset = worldPoint - WorldCenterOfMass;
                return Velocity + new Vector2(
                    -radiansPerSecond * offset.y,
                    radiansPerSecond * offset.x);
            }
        }
    }

    internal sealed class FallingStoneWallBodyRelay : MonoBehaviour
    {
        private FallingStoneWallTerrain _wall;

        public void Initialize(FallingStoneWallTerrain wall)
        {
            _wall = wall;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 独立刚体上的碰撞仍交给原石墙处理伤害。
            if (_wall != null)
            {
                _wall.HandleBodyCollision(collision);
            }
        }
    }
}
