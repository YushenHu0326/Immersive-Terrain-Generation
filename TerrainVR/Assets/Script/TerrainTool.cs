using System.Linq;

using UnityEngine;

public sealed class TerrainTool : MonoBehaviour
{
    public enum TerrainModificationAction
    {
        Raise,
        Lower,
        Flatten,
        Sample,
        SampleAverage,
    }

    public int brushWidth = 200;
    public int brushHeight = 200;

    public TerrainModificationAction modificationAction;

    public Terrain _targetTerrain;
    private float[,] virtualHeights;
    private float[,] alphas;
    public float terrainOffset = 50f;

    private float _sampledHeight;

    public void OnActionChange(int value)
    {
        if (value == 0) modificationAction = TerrainModificationAction.Raise;
        else if (value == 1) modificationAction = TerrainModificationAction.Lower;
        else if (value == 2) modificationAction = TerrainModificationAction.Flatten;
        else if (value == 3) modificationAction = TerrainModificationAction.Sample;
        else if (value == 4) modificationAction = TerrainModificationAction.SampleAverage;
    }

    public void OnSizeChange(float size)
    {
        brushWidth = (int) (500f * size);
        brushHeight = (int) (500f * size);
    }

    private void Start()
    {
        _targetTerrain = GameObject.FindObjectOfType<Terrain>();
        virtualHeights = new float[GetHeightmapResolution(), GetHeightmapResolution()];
        alphas = new float[GetHeightmapResolution(), GetHeightmapResolution()];
    }

    public void OnCanyon()
    {
        float terrainSizeY = GetTerrainSize().y;
        for (int y = 0; y < GetHeightmapResolution(); y++)
        {
            for (int x = 0; x < GetHeightmapResolution(); x++)
            {
                virtualHeights[y, x] = terrainOffset / terrainSizeY;
            }
        }
    }

    private TerrainData GetTerrainData() => _targetTerrain.terrainData;

    private int GetHeightmapResolution() => GetTerrainData().heightmapResolution;

    private Vector3 GetTerrainSize() => GetTerrainData().size;

    public Vector3 WorldToTerrainPosition(Vector3 worldPosition)
    {
        var terrainPosition = worldPosition - _targetTerrain.GetPosition();

        var terrainSize = GetTerrainSize();

        var heightmapResolution = GetHeightmapResolution();

        terrainPosition = new Vector3(terrainPosition.x / terrainSize.x, terrainPosition.y / terrainSize.y, terrainPosition.z / terrainSize.z);

        return new Vector3(terrainPosition.x * heightmapResolution, 0, terrainPosition.z * heightmapResolution);
    }

    public Vector2Int GetBrushPosition(Vector3 worldPosition, int brushWidth, int brushHeight)
    {
        var terrainPosition = WorldToTerrainPosition(worldPosition);

        var heightmapResolution = GetHeightmapResolution();

        return new Vector2Int((int)Mathf.Clamp(terrainPosition.x - brushWidth / 2.0f, 0.0f, heightmapResolution), (int)Mathf.Clamp(terrainPosition.z - brushHeight / 2.0f, 0.0f, heightmapResolution));
    }

    public Vector2Int GetSafeBrushSize(int brushX, int brushY, int brushWidth, int brushHeight)
    {
        var heightmapResolution = GetHeightmapResolution();

        while (heightmapResolution - (brushX + brushWidth) < 0) brushWidth--;

        while (heightmapResolution - (brushY + brushHeight) < 0) brushHeight--;

        return new Vector2Int(brushWidth, brushHeight);
    }

    public void RaiseTerrain(Vector3 worldPosition, float height, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                float distance = Mathf.Sqrt(Mathf.Pow((float)x - (float)brushSize.x / 2f, 2f) +
                                            Mathf.Pow((float)y - (float)brushSize.y / 2f, 2f));
                distance /= (float)brushSize.x / 2f;
                distance = Mathf.Clamp(distance, 0f, 1f);
                if (virtualHeights[y + brushPosition.y, x + brushPosition.x] < (1f - distance) * height / terrainData.size.y)
                    virtualHeights[y + brushPosition.y, x + brushPosition.x] = (1f - distance) * height / terrainData.size.y;

                float alpha = 1f - Mathf.Pow(distance, 2);
                if (alphas[y + brushPosition.y, x + brushPosition.x] < alpha)
                    alphas[y + brushPosition.y, x + brushPosition.x] = alpha;
            }
        }
    }

    public void LowerTerrain(Vector3 worldPosition, float height, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        Debug.Log(height);

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                float distance = Mathf.Sqrt(Mathf.Pow((float)x - (float)brushSize.x / 2f, 2f) +
                                            Mathf.Pow((float)y - (float)brushSize.y / 2f, 2f));
                distance /= (float)brushSize.x / 2f;
                distance = Mathf.Clamp(distance, 0f, 1f);
                if (virtualHeights[y + brushPosition.y, x + brushPosition.x] > (terrainOffset - Mathf.Pow((1f - distance), 3f) * height) / terrainData.size.y)
                    virtualHeights[y + brushPosition.y, x + brushPosition.x] = (terrainOffset - Mathf.Pow((1f - distance), 3f) * height) / terrainData.size.y;

                float alpha = 1f - distance;
                if (alphas[y + brushPosition.y, x + brushPosition.x] < alpha)
                    alphas[y + brushPosition.y, x + brushPosition.x] = alpha;
            }
        }
    }

    public void ApplyTerrain(int terrainType, float xMin, float xMax, float yMin, float yMax, float radius, float erosionStrength)
    {
        Vector3 min = WorldToTerrainPosition(new Vector3(xMin, 0f, yMin));
        Vector3 max = WorldToTerrainPosition(new Vector3(xMax, 0f, yMax));
        float range = Mathf.Max(max.x - min.x, max.z - min.z) + 2f * radius;

        TerrainModifier modifier = Object.FindObjectOfType<TerrainModifier>();
        modifier.heights = virtualHeights;
        modifier.alphas = alphas;
        modifier.xOffset = (int)(min.x - radius);
        modifier.yOffset = (int)(min.z - radius);
        modifier.terrainOffset = terrainOffset;
        modifier.range = (int)range;

        modifier.ModifyTerrain(terrainType, erosionStrength);
        for (var y = 0; y < GetHeightmapResolution(); y++)
        {
            for (var x = 0; x < GetHeightmapResolution(); x++)
            {
                virtualHeights[y, x] = 0f;
                alphas[y, x] = 0f;
            }
        }
    }

    public void ClearTerrain()
    {
        var terrainData = GetTerrainData();

        int r = terrainData.heightmapResolution;

        var heights = terrainData.GetHeights(0, 0, r, r);

        for (var y = 0; y < r; y++)
        {
            for (var x = 0; x < r; x++)
            {
                heights[y, x] = terrainOffset / terrainData.size.y;
            }
        }

        terrainData.SetHeights(0, 0, heights);

        eTerrain eTerrain = _targetTerrain.gameObject.GetComponent<eTerrain>();
        if (eTerrain != null) eTerrain.Splat();
    }

    public void FlattenTerrain(Vector3 worldPosition, float height, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        var heights = terrainData.GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                float distance = Mathf.Sqrt(Mathf.Pow((float)x - (float)brushSize.x / 2f, 2f) +
                                            Mathf.Pow((float)y - (float)brushSize.y / 2f, 2f));
                if (distance > 0f) heights[y, x] = height;
            }
        }

        terrainData.SetHeights(brushPosition.x, brushPosition.y, heights);
    }

    public float SampleHeight(Vector3 worldPosition)
    {
        var terrainPosition = WorldToTerrainPosition(worldPosition);

        return GetTerrainData().GetInterpolatedHeight((int)terrainPosition.x, (int)terrainPosition.z);
    }

    public float SampleAverageHeight(Vector3 worldPosition, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var heights2D = GetTerrainData().GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);

        var heights = new float[heights2D.Length];

        var i = 0;

        for (int y = 0; y <= heights2D.GetUpperBound(0); y++)
        {
            for (int x = 0; x <= heights2D.GetUpperBound(1); x++)
            {
                heights[i++] = heights2D[y, x];
            }
        }

        return heights.Average();
    }
}