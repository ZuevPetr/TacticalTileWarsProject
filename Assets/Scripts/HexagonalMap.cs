using UnityEngine;
using System.Collections.Generic;

public enum TileType
{
    Water,
    Plain,
    Mountain
}

public class HexTile
{
    public TileType Type { get; set; }
    public Vector2Int GridPosition { get; set; }
    public Vector3 WorldPosition { get; set; }
}

public class HexagonalMap : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapRadius = 10;
    public float hexSize = 1f;

    [Header("Perlin Noise Settings")]
    public float noiseScale = 0.1f;
    [Range(0f, 1f)] public float waterThreshold = 0.3f;
    [Range(0f, 1f)] public float mountainThreshold = 0.7f;
    public Vector2 noiseOffset;

    [Header("Tile Prefabs")]
    public GameObject waterTilePrefab;
    public GameObject plainTilePrefab;
    public GameObject mountainTilePrefab;

    private Dictionary<Vector2Int, HexTile> hexGrid = new Dictionary<Vector2Int, HexTile>();
    private Dictionary<Vector2Int, GameObject> tileObjects = new Dictionary<Vector2Int, GameObject>();

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        GenerateHexGrid();
        ApplyPerlinNoise();
        VisualizeTiles();
    }

    private void GenerateHexGrid()
    {
        hexGrid.Clear();

        for (int q = -mapRadius; q <= mapRadius; q++)
        {
            for (int r = -mapRadius; r <= mapRadius; r++)
            {
                int s = -q - r;
                if (Mathf.Abs(q) <= mapRadius && Mathf.Abs(r) <= mapRadius && Mathf.Abs(s) <= mapRadius)
                {
                    Vector2Int gridPos = new Vector2Int(q, r);
                    Vector3 worldPos = GridToWorldPosition(gridPos);

                    HexTile tile = new HexTile
                    {
                        GridPosition = gridPos,
                        WorldPosition = worldPos,
                        Type = TileType.Plain
                    };

                    hexGrid.Add(gridPos, tile);
                }
            }
        }
    }

    private Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        float q = gridPos.x;
        float r = gridPos.y;

        // Правильное преобразование для flat-top гексов
        float x = hexSize * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
        float y = hexSize * (3f / 2f * r);

        return new Vector3(x, y, 0);
    }

    private void ApplyPerlinNoise()
    {
        foreach (HexTile tile in hexGrid.Values)
        {
            // Используем мировые координаты для согласованного шума
            float noiseX = (tile.WorldPosition.x + noiseOffset.x) * noiseScale;
            float noiseY = (tile.WorldPosition.y + noiseOffset.y) * noiseScale;

            float noiseValue = Mathf.PerlinNoise(noiseX, noiseY);

            if (noiseValue < waterThreshold)
            {
                tile.Type = TileType.Water;
            }
            else if (noiseValue > mountainThreshold)
            {
                tile.Type = TileType.Mountain;
            }
            else
            {
                tile.Type = TileType.Plain;
            }
        }
    }

    private void VisualizeTiles()
    {
        // Удаляем старые тайлы
        foreach (var obj in tileObjects.Values)
        {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }
        tileObjects.Clear();

        // Создаем новые тайлы
        foreach (HexTile tile in hexGrid.Values)
        {
            GameObject prefab = GetPrefabByType(tile.Type);
            if (prefab != null)
            {
                GameObject tileObj = Instantiate(prefab, transform);
                tileObj.transform.position = tile.WorldPosition;

                tileObj.transform.Rotate(0, 0, 90); // Для flat-top гексов поворот не нужен, они уже должны быть правильно ориентированы в префабе, но поскольку в префабе гекс point-top, то:

                tileObjects.Add(tile.GridPosition, tileObj);
            }
        }
    }

    private GameObject GetPrefabByType(TileType type)
    {
        switch (type)
        {
            case TileType.Water: return waterTilePrefab;
            case TileType.Plain: return plainTilePrefab;
            case TileType.Mountain: return mountainTilePrefab;
            default: return plainTilePrefab;
        }
    }

    // Визуализация в редакторе для отладки
    void OnDrawGizmos()
    {
        if (hexGrid == null || hexGrid.Count == 0) return;

        Gizmos.color = Color.white;
        foreach (var tile in hexGrid.Values)
        {
            DrawHexGizmo(tile.WorldPosition, hexSize);
        }
    }

    private void DrawHexGizmo(Vector3 center, float size)
    {
        Vector3[] vertices = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i - 30f; // Смещение для flat-top
            angle *= Mathf.Deg2Rad;
            vertices[i] = center + new Vector3(size * Mathf.Cos(angle), size * Mathf.Sin(angle), 0);
        }

        for (int i = 0; i < 6; i++)
        {
            Gizmos.DrawLine(vertices[i], vertices[(i + 1) % 6]);
        }
    }

    void Start()
    {
        GenerateMap();
    }
}