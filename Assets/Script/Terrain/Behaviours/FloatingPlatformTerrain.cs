using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    public sealed class FloatingPlatformTerrain : TerrainEntity, ITerrainSegmentHost
    {
        // 2026-07-19：平台拆成十段，图片之间稍微压一点，避免出现白缝。
        private const int SegmentCount = 10;
        private const float VisualOverlapWorldUnits = 0.015f;

        private readonly GameObject[] _segmentObjects =
            new GameObject[SegmentCount];
        private readonly SpriteRenderer[] _segmentRenderers =
            new SpriteRenderer[SegmentCount];
        private readonly int[] _segmentHealth =
            new int[SegmentCount];

        private int _activeSegmentCount;

        public TerrainEntity TerrainEntity => this;

        protected override void Awake()
        {
            base.Awake();
            CreateSegments();
        }

        public int GetSegmentCurrentHealth(int segmentIndex)
        {
            return IsValidSegment(segmentIndex)
                ? _segmentHealth[segmentIndex]
                : 0;
        }

        public void CopyActiveSegmentRenderers(
            System.Collections.Generic.List<SpriteRenderer> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            if (IsBeingDestroyed)
            {
                return;
            }

            for (int i = 0; i < SegmentCount; i++)
            {
                if (_segmentObjects[i] == null ||
                    _segmentHealth[i] <= 0 ||
                    _segmentRenderers[i] == null)
                {
                    continue;
                }

                results.Add(_segmentRenderers[i]);
            }
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

            // 2026-07-19：现在命中一次就直接拆掉这一段。
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
                    "Floating Platform requires a root SpriteRenderer " +
                    "and BoxCollider2D for ten-segment construction.",
                    this);
                return;
            }

            float segmentWidth =
                sourceCollider.size.x / SegmentCount;
            float startX =
                sourceCollider.offset.x -
                sourceCollider.size.x * 0.5f;
            float parentWorldScaleX =
                Mathf.Abs(transform.lossyScale.x);
            float visualOverlapLocal =
                parentWorldScaleX > 0.0001f
                    ? VisualOverlapWorldUnits / parentWorldScaleX
                    : 0f;
            float spriteWidth =
                sourceRenderer.sprite.bounds.size.x;

            if (segmentWidth <= 0f || spriteWidth <= 0.0001f)
            {
                Debug.LogError(
                    "Floating Platform has an invalid Sprite or Collider width.",
                    this);
                return;
            }

            for (int i = 0; i < SegmentCount; i++)
            {
                // 每段都从同一个起点算位置，不用上一段的位置累加。
                float centerX =
                    startX + (i + 0.5f) * segmentWidth;

                GameObject segmentObject =
                    new GameObject($"Segment {i + 1:00}");
                segmentObject.layer = gameObject.layer;
                segmentObject.tag = gameObject.tag;
                segmentObject.transform.SetParent(transform, false);
                segmentObject.transform.localPosition =
                    new Vector3(
                        centerX,
                        sourceCollider.offset.y,
                        0f);

                BoxCollider2D segmentCollider =
                    segmentObject.AddComponent<BoxCollider2D>();
                segmentCollider.size =
                    new Vector2(
                        segmentWidth,
                        sourceCollider.size.y);
                segmentCollider.offset = Vector2.zero;
                segmentCollider.sharedMaterial =
                    sourceCollider.sharedMaterial;
                segmentCollider.isTrigger = sourceCollider.isTrigger;
                segmentCollider.usedByEffector =
                    sourceCollider.usedByEffector;
                segmentCollider.usedByComposite =
                    sourceCollider.usedByComposite;
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
                visualObject.transform.localScale =
                    new Vector3(
                        (segmentWidth + visualOverlapLocal) /
                        spriteWidth,
                        1f,
                        1f);

                SpriteRenderer segmentRenderer =
                    visualObject.AddComponent<SpriteRenderer>();
                CopyRendererSettings(
                    sourceRenderer,
                    segmentRenderer);

                _segmentObjects[i] = segmentObject;
                _segmentRenderers[i] = segmentRenderer;
                _segmentHealth[i] = MaximumHealth;

                PlatformEffector2D effector = segmentObject.AddComponent<PlatformEffector2D>();
            }

            _activeSegmentCount = SegmentCount;
            // 2026-07-19：根碰撞只留给范围检查，真正站立用下面十段碰撞。
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
                _segmentRenderers[segmentIndex] = null;
                _activeSegmentCount--;
                segmentObject.SetActive(false);
                Destroy(segmentObject);
            }

            if (_activeSegmentCount == 0)
            {
                DestroyTerrain(true);
            }
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
    }
}
