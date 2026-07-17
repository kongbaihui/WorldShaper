using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PermanentTerrainMarker : MonoBehaviour
    {
        [SerializeField] private bool _isArenaBoundary;

        public bool IsArenaBoundary => _isArenaBoundary;

        public void Configure(bool isArenaBoundary)
        {
            _isArenaBoundary = isArenaBoundary;
        }
    }
}
