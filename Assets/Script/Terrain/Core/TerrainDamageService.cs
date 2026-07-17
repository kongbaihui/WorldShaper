using System;
using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    public sealed class TerrainDamageService : MonoBehaviour
    {
        [SerializeField] private TerrainRegistry _registry;
        [SerializeField] private Transform _playerCreator;
        [SerializeField, Min(0.1f)] private float _interactionRange = 6f;
        [SerializeField, Min(1)] private int _terrainDamagePerClick = 1;
        [SerializeField, Min(0.05f)] private float _cursorQueryRadius = 0.65f;

        public event Action<string> MessageChanged;

        public string LastMessage { get; private set; } = string.Empty;
        public float InteractionRange => _interactionRange;
        public int TerrainDamagePerClick => _terrainDamagePerClick;

        public void Configure(
            TerrainRegistry registry,
            Transform playerCreator,
            float interactionRange = 6f,
            int terrainDamagePerClick = 1,
            float cursorQueryRadius = 0.65f)
        {
            _registry = registry;
            _playerCreator = playerCreator;
            _interactionRange = Mathf.Max(0.1f, interactionRange);
            _terrainDamagePerClick = Mathf.Max(1, terrainDamagePerClick);
            _cursorQueryRadius = Mathf.Max(0.05f, cursorQueryRadius);
        }

        public bool TryDamageTerrain(
            TerrainOwner attacker,
            Transform attackerTransform,
            Vector2 worldPosition)
        {
            if (_registry == null)
            {
                return ReportFailure("Terrain registry unavailable");
            }

            attackerTransform = attackerTransform != null ? attackerTransform : _playerCreator;
            if (attackerTransform == null)
            {
                return ReportFailure("Creator missing");
            }

            Collider2D[] overlaps = Physics2D.OverlapCircleAll(worldPosition, _cursorQueryRadius);
            TerrainEntity nearestTerrain = null;
            float nearestSquaredDistance = float.MaxValue;
            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider2D overlap = overlaps[i];
                TerrainEntity terrain = overlap != null ? overlap.GetComponentInParent<TerrainEntity>() : null;
                if (terrain == null || terrain.IsBeingDestroyed || terrain.PrimaryCollider == null)
                {
                    continue;
                }

                float squaredDistance = ((Vector2)terrain.PrimaryCollider.ClosestPoint(worldPosition) - worldPosition).sqrMagnitude;
                if (squaredDistance < nearestSquaredDistance)
                {
                    nearestSquaredDistance = squaredDistance;
                    nearestTerrain = terrain;
                }
            }

            if (nearestTerrain == null)
            {
                return ReportFailure("No destructible terrain at cursor");
            }

            Vector2 closestPointToAttacker = nearestTerrain.PrimaryCollider.ClosestPoint(attackerTransform.position);
            if (Vector2.Distance(attackerTransform.position, closestPointToAttacker) > _interactionRange)
            {
                return ReportFailure("Target out of range");
            }

            return TryDamageTerrain(
                nearestTerrain,
                _terrainDamagePerClick,
                attacker,
                attackerTransform);
        }

        public bool TryDamageTerrain(
            TerrainEntity terrain,
            int damage,
            TerrainOwner attacker,
            Transform attackerTransform)
        {
            if (terrain == null || terrain.IsBeingDestroyed)
            {
                return ReportFailure("Terrain cannot be damaged");
            }

            if (!terrain.TryApplyDamage(
                    Mathf.Max(1, damage),
                    attacker,
                    attackerTransform != null ? attackerTransform : _playerCreator))
            {
                return ReportFailure("Terrain cannot be damaged");
            }

            LastMessage = terrain.CurrentHealth > 0
                ? $"Damaged {terrain.TerrainType}: {terrain.CurrentHealth}/{terrain.MaximumHealth} HP"
                : $"Destroyed {terrain.TerrainType}";
            MessageChanged?.Invoke(LastMessage);
            return true;
        }

        public void ClearMessage()
        {
            LastMessage = string.Empty;
            MessageChanged?.Invoke(LastMessage);
        }

        private bool ReportFailure(string message)
        {
            LastMessage = message;
            MessageChanged?.Invoke(LastMessage);
            return false;
        }
    }
}
