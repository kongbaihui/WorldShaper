using System.Collections;
using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    public sealed class PrototypeImpactEffect : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Color _startColor;
        private Vector3 _startScale;
        private float _duration;

        public static void Spawn(
            Vector2 worldPosition,
            Sprite sprite,
            Color color,
            Vector2 sourceSize,
            float intensity = 1f)
        {
            GameObject effectObject = new GameObject("Challenge2 Impact Effect");
            effectObject.transform.position = worldPosition;
            effectObject.transform.localScale = new Vector3(
                Mathf.Max(0.25f, sourceSize.x * 0.35f * intensity),
                Mathf.Max(0.25f, sourceSize.y * 0.35f * intensity),
                1f);

            SpriteRenderer spriteRenderer = effectObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 50;

            PrototypeImpactEffect effect = effectObject.AddComponent<PrototypeImpactEffect>();
            effect.Initialize(spriteRenderer, 0.25f);
        }

        private void Initialize(SpriteRenderer spriteRenderer, float duration)
        {
            _renderer = spriteRenderer;
            _startColor = spriteRenderer.color;
            _startScale = transform.localScale;
            _duration = duration;
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / _duration);
                transform.localScale = Vector3.Lerp(_startScale, _startScale * 2.25f, normalized);
                Color color = _startColor;
                color.a = 1f - normalized;
                _renderer.color = color;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
