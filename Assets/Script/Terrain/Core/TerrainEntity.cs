using System.Collections;
using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    public abstract class TerrainEntity : MonoBehaviour, IPrototypeDamageable
    {
        [Header("Terrain Identity")]
        [SerializeField] private TerrainType _terrainType;
        [SerializeField] private TerrainOwner _owner = TerrainOwner.Neutral;
        [SerializeField, Min(1)] private int _maximumHealth = 1;

        [Header("Presentation")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Scene Wiring")]
        [SerializeField] private TerrainRegistry _registry;
        [SerializeField] private Transform _creator;

        private int _currentHealth;
        private bool _isRegistered;
        private bool _isBeingDestroyed;
        private Vector3 _baseScale;
        private Color _baseColor;
        private Coroutine _feedbackRoutine;

        public TerrainType TerrainType => _terrainType;
        public TerrainOwner Owner => _owner;
        public Transform Creator => _creator;
        public Transform DamageTransform => transform;
        public bool IsAlive => !_isBeingDestroyed && _currentHealth > 0;
        public bool IsBeingDestroyed => _isBeingDestroyed;
        public int CurrentHealth => _currentHealth;
        public int MaximumHealth => _maximumHealth;
        public Sprite VisualSprite => _spriteRenderer != null ? _spriteRenderer.sprite : null;
        public Collider2D PrimaryCollider { get; private set; }

        protected virtual void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            PrimaryCollider = GetComponent<Collider2D>();
            _currentHealth = Mathf.Max(1, _maximumHealth);
            _baseScale = transform.localScale;
            _baseColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
        }

        protected virtual void Start()
        {
            RegisterIfReady();
        }

        public void ConfigurePrefab(TerrainType terrainType, int maximumHealth, SpriteRenderer spriteRenderer)
        {
            _terrainType = terrainType;
            _maximumHealth = Mathf.Max(1, maximumHealth);
            _spriteRenderer = spriteRenderer;
        }

        public void ConfigureSceneTerrain(TerrainOwner owner, Transform creator, TerrainRegistry registry)
        {
            _owner = owner;
            _creator = creator;
            _registry = registry;
        }

        public void Initialize(TerrainOwner owner, Transform creator, TerrainRegistry registry)
        {
            _owner = owner;
            _creator = creator;
            _registry = registry;
            _currentHealth = Mathf.Max(1, _maximumHealth);
            RegisterIfReady();
        }

        public bool TryApplyDamage(int amount, TerrainOwner attacker, Transform attackerTransform)
        {
            if (_isBeingDestroyed || amount <= 0)
            {
                return false;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            if (_currentHealth == 0)
            {
                DestroyTerrain(true);
            }
            else
            {
                PlayDamageFeedback();
            }

            return true;
        }

        public void DestroyTerrain(bool showEffect)
        {
            if (_isBeingDestroyed)
            {
                return;
            }

            _isBeingDestroyed = true;
            Unregister();

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = false;
            }

            if (showEffect && _spriteRenderer != null)
            {
                PrototypeImpactEffect.Spawn(
                    transform.position,
                    _spriteRenderer.sprite,
                    _baseColor,
                    GetVisualSize(),
                    1f);
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = false;
            }

            Destroy(gameObject);
        }

        protected bool CanDamageTarget(PrototypeDamageable target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            if (_owner != TerrainOwner.Neutral && target.Owner == _owner)
            {
                return false;
            }

            return !IsCreatorTransform(target.transform);
        }

        protected bool IsCreatorTransform(Transform candidate)
        {
            if (_creator == null || candidate == null)
            {
                return false;
            }

            return candidate == _creator ||
                   candidate.IsChildOf(_creator) ||
                   _creator.IsChildOf(candidate);
        }

        private void RegisterIfReady()
        {
            if (_isRegistered || _registry == null || _isBeingDestroyed)
            {
                return;
            }

            _registry.Register(this);
            _isRegistered = true;
        }

        private void Unregister()
        {
            if (!_isRegistered || _registry == null)
            {
                return;
            }

            _registry.Unregister(this);
            _isRegistered = false;
        }

        private Vector2 GetVisualSize()
        {
            if (_spriteRenderer != null)
            {
                return _spriteRenderer.bounds.size;
            }

            return new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        }

        private void PlayDamageFeedback()
        {
            if (_feedbackRoutine != null)
            {
                StopCoroutine(_feedbackRoutine);
            }

            _feedbackRoutine = StartCoroutine(DamageFeedbackRoutine());
        }

        private IEnumerator DamageFeedbackRoutine()
        {
            float elapsed = 0f;
            const float duration = 0.14f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(normalized * Mathf.PI);
                transform.localScale = _baseScale * (1f + pulse * 0.08f);
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.color = Color.Lerp(_baseColor, Color.white, pulse * 0.9f);
                }

                yield return null;
            }

            transform.localScale = _baseScale;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
            }

            _feedbackRoutine = null;
        }

        protected virtual void OnDestroy()
        {
            Unregister();
        }
    }
}
