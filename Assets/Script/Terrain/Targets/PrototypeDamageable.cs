using System;
using System.Collections;
using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class PrototypeDamageable : MonoBehaviour, IPrototypeDamageable
    {
        [Header("Identity")]
        [SerializeField] private TerrainOwner _owner;
        [SerializeField, Min(1)] private int _maximumHealth;

        [Header("Presentation")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Color _damageFlashColor = Color.white;

        // UI 的 OnEnable 可能早于此组件的 Awake，使用非零值避免血条在初始化阶段误判死亡。
        private int _currentHealth = 1;
        private Color _baseColor;
        private Coroutine _flashRoutine;

        public event Action<int, int> HealthChanged;

        public TerrainOwner Owner => _owner;
        public Transform DamageTransform => transform;
        public bool IsAlive => _currentHealth > 0;
        public int CurrentHealth => _currentHealth;
        public int MaximumHealth => _maximumHealth;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            _baseColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
            ResetHealth();
        }

        public bool TryApplyDamage(int amount, TerrainOwner attacker, Transform attackerTransform)
        {
            if (amount <= 0 || !IsAlive)
            {
                return false;
            }

            if (attacker != TerrainOwner.Neutral && attacker == _owner)
            {
                return false;
            }

            if (IsSameActor(attackerTransform))
            {
                return false;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            HealthChanged?.Invoke(_currentHealth, _maximumHealth);
            PlayDamageFlash();
            return true;
        }

        public void ResetHealth()
        {
            _currentHealth = Mathf.Max(1, _maximumHealth);
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
            }

            HealthChanged?.Invoke(_currentHealth, _maximumHealth);
        }

        private bool IsSameActor(Transform attackerTransform)
        {
            if (attackerTransform == null)
            {
                return false;
            }

            return attackerTransform == transform ||
                   attackerTransform.IsChildOf(transform) ||
                   transform.IsChildOf(attackerTransform);
        }

        private void PlayDamageFlash()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(DamageFlashRoutine());
        }

        private IEnumerator DamageFlashRoutine()
        {
            _spriteRenderer.color = _damageFlashColor;
            yield return new WaitForSeconds(0.12f);
            _spriteRenderer.color = _baseColor;
            _flashRoutine = null;
        }
    }
}
