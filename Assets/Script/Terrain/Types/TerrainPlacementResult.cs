using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    public readonly struct TerrainPlacementResult
    {
        private TerrainPlacementResult(bool isValid, string reason, Vector2 snappedPosition)
        {
            IsValid = isValid;
            Reason = reason;
            SnappedPosition = snappedPosition;
        }

        public bool IsValid { get; }
        public string Reason { get; }
        public Vector2 SnappedPosition { get; }

        public static TerrainPlacementResult Valid(Vector2 snappedPosition)
        {
            return new TerrainPlacementResult(true, string.Empty, snappedPosition);
        }

        public static TerrainPlacementResult Invalid(string reason, Vector2 snappedPosition)
        {
            return new TerrainPlacementResult(false, reason, snappedPosition);
        }
    }
}
