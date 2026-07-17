using System;
using System.Collections.Generic;
using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    public sealed class TerrainRegistry : MonoBehaviour
    {
        private readonly List<TerrainEntity> _activeTerrain = new List<TerrainEntity>(16);

        public event Action Changed;

        public int TotalCount
        {
            get
            {
                RemoveMissingEntries();
                return _activeTerrain.Count;
            }
        }

        public void Register(TerrainEntity terrain)
        {
            if (terrain == null || _activeTerrain.Contains(terrain))
            {
                return;
            }

            _activeTerrain.Add(terrain);
            Changed?.Invoke();
        }

        public void Unregister(TerrainEntity terrain)
        {
            if (terrain == null || !_activeTerrain.Remove(terrain))
            {
                return;
            }

            Changed?.Invoke();
        }

        public int Count(TerrainType terrainType)
        {
            RemoveMissingEntries();
            int count = 0;
            for (int i = 0; i < _activeTerrain.Count; i++)
            {
                if (_activeTerrain[i].TerrainType == terrainType)
                {
                    count++;
                }
            }

            return count;
        }

        public int Count(TerrainOwner owner, TerrainType terrainType)
        {
            RemoveMissingEntries();
            int count = 0;
            for (int i = 0; i < _activeTerrain.Count; i++)
            {
                TerrainEntity terrain = _activeTerrain[i];
                if (terrain.Owner == owner && terrain.TerrainType == terrainType)
                {
                    count++;
                }
            }

            return count;
        }

        public int Count(TerrainOwner owner)
        {
            RemoveMissingEntries();
            int count = 0;
            for (int i = 0; i < _activeTerrain.Count; i++)
            {
                if (_activeTerrain[i].Owner == owner)
                {
                    count++;
                }
            }

            return count;
        }

        public void CopyActiveTerrain(List<TerrainEntity> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            RemoveMissingEntries();
            destination.Clear();
            destination.AddRange(_activeTerrain);
        }

        private void RemoveMissingEntries()
        {
            bool changed = false;
            for (int i = _activeTerrain.Count - 1; i >= 0; i--)
            {
                if (_activeTerrain[i] != null)
                {
                    continue;
                }

                _activeTerrain.RemoveAt(i);
                changed = true;
            }

            if (changed)
            {
                Changed?.Invoke();
            }
        }
    }
}
