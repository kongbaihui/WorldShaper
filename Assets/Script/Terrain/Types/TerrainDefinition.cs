using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    public readonly struct TerrainDefinition
    {
        public TerrainDefinition(
            TerrainType type,
            Vector2 size,
            int maximumHealth,
            float gravityScale,
            Color color)
        {
            Type = type;
            Size = size;
            MaximumHealth = maximumHealth;
            GravityScale = gravityScale;
            Color = color;
        }

        public TerrainType Type { get; }
        public Vector2 Size { get; }
        public int MaximumHealth { get; }
        public float GravityScale { get; }
        public Color Color { get; }
    }

    public static class TerrainDefinitionCatalog
    {
        public static TerrainDefinition Get(TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.FloatingPlatform:
                    return new TerrainDefinition(
                        terrainType,
                        new Vector2(2.5f, 0.4f),
                        3,
                        0f,
                        new Color(0.18f, 0.72f, 0.92f, 1f));

                case TerrainType.FallingStoneWall:
                    return new TerrainDefinition(
                        terrainType,
                        new Vector2(1f, 2f),
                        5,
                        2f,
                        new Color(0.46f, 0.40f, 0.35f, 1f));

                case TerrainType.FallingStoneSpike:
                    return new TerrainDefinition(
                        terrainType,
                        new Vector2(0.8f, 1.5f),
                        1,
                        2.5f,
                        new Color(0.86f, 0.22f, 0.28f, 1f));

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(terrainType), terrainType, null);
            }
        }
    }
}
