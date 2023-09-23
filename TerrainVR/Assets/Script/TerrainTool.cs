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

    public void RaiseTerrainFilled(Vector3 worldPosition, Vector3 initialPosition, float height, float initHeight, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);
        var initBrushPosition = GetBrushPosition(initialPosition, brushWidth, brushHeight);

        int worldPositionX = brushPosition.x + (int)((float)brushWidth / 2f);
        int worldPositionY = brushPosition.y + (int)((float)brushWidth / 2f);
        int initPositionX = initBrushPosition.x + (int)((float)brushWidth / 2f);
        int initPositionY = initBrushPosition.y + (int)((float)brushWidth / 2f);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        int start, end;
        float actualHeight;
        if (worldPositionX != initPositionX)
        {
            start = (int)Mathf.Min(worldPositionX, initPositionX);
            end = (int)Mathf.Max(worldPositionX, initPositionX);

            int y;

            for (var x = start; x < end; x++)
            {
                actualHeight = Mathf.Lerp(initHeight, height, (float)(x - initPositionX) / (float)(worldPositionX - initPositionX));
                y = initPositionY + (int)((float)(x - initPositionX) / (float)(worldPositionX - initPositionX) * (worldPositionY - initPositionY));

                for (var yy = 0; yy < 20; yy++)
                {
                    for (var xx = 0; xx < 20; xx++)
                    {
                        if (virtualHeights[yy - 10 + y, xx - 10 + x] < actualHeight / terrainData.size.y)
                            virtualHeights[yy - 10 + y, xx - 10 + x] = actualHeight / terrainData.size.y;
                        if (alphas[yy - 10 + y, xx - 10 + x] < 1f)
                            alphas[yy - 10 + y, xx - 10 + x] = 1f;
                    }
                }
            }
        }
        else
        {
            start = (int)Mathf.Min(worldPositionY, initPositionY);
            end = (int)Mathf.Max(worldPositionY, initPositionY);

            int x;

            for (var y = start; y < end; y++)
            {
                actualHeight = Mathf.Lerp(initHeight, height, (float)(y - initPositionY) / (float)(worldPositionY - initPositionY));
                x = (int)initPositionX + (int)((float)(y - initPositionY) / (float)(worldPositionY - initPositionY) * (worldPositionX - initPositionX));

                for (var yy = 0; yy < 20; yy++)
                {
                    for (var xx = 0; xx < 20; xx++)
                    {
                        if (virtualHeights[yy - 10 + y, xx - 10 + x] < actualHeight / terrainData.size.y)
                            virtualHeights[yy - 10 + y, xx - 10 + x] = actualHeight / terrainData.size.y;
                        if (alphas[yy - 10 + y, xx - 10 + x] < 1f)
                            alphas[yy - 10 + y, xx - 10 + x] = 1f;
                    }
                }
            }
        }
    }

    public void LowerTerrain(Vector3 worldPosition, float height, int brushWidth, int brushHeight)
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
                if (virtualHeights[y + brushPosition.y, x + brushPosition.x] > (terrainOffset - Mathf.Pow((1f - distance), 3f) * height) / terrainData.size.y)
                    virtualHeights[y + brushPosition.y, x + brushPosition.x] = (terrainOffset - Mathf.Pow((1f - distance), 3f) * height) / terrainData.size.y;

                float alpha = 1f - distance;
                if (alphas[y + brushPosition.y, x + brushPosition.x] < alpha)
                    alphas[y + brushPosition.y, x + brushPosition.x] = alpha;
            }
        }
    }

    public void LowerTerrainFilled(Vector3 worldPosition, Vector3 initialPosition, float height, float initHeight, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);
        var initBrushPosition = GetBrushPosition(initialPosition, brushWidth, brushHeight);

        int worldPositionX = brushPosition.x + (int)((float)brushWidth / 2f);
        int worldPositionY = brushPosition.y + (int)((float)brushWidth / 2f);
        int initPositionX = initBrushPosition.x + (int)((float)brushWidth / 2f);
        int initPositionY = initBrushPosition.y + (int)((float)brushWidth / 2f);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        int start, end;
        float actualHeight;
        if (worldPositionX != initPositionX)
        {
            start = (int)Mathf.Min(worldPositionX, initPositionX);
            end = (int)Mathf.Max(worldPositionX, initPositionX);

            int y;

            for (var x = start; x < end; x++)
            {
                actualHeight = Mathf.Lerp(initHeight, height, (float)(x - initPositionX) / (float)(worldPositionX - initPositionX));
                y = initPositionY + (int)((float)(x - initPositionX) / (float)(worldPositionX - initPositionX) * (worldPositionY - initPositionY));

                for (var yy = 0; yy < 20; yy++)
                {
                    for (var xx = 0; xx < 20; xx++)
                    {
                        if (virtualHeights[yy - 10 + y, xx - 10 + x] > (terrainOffset - actualHeight) / terrainData.size.y)
                            virtualHeights[yy - 10 + y, xx - 10 + x] = (terrainOffset - actualHeight) / terrainData.size.y;
                        if (alphas[yy - 10 + y, xx - 10 + x] < 1f)
                            alphas[yy - 10 + y, xx - 10 + x] = 1f;
                    }
                }
            }
        }
        else
        {
            start = (int)Mathf.Min(worldPositionY, initPositionY);
            end = (int)Mathf.Max(worldPositionY, initPositionY);

            int x;

            for (var y = start; y < end; y++)
            {
                actualHeight = Mathf.Lerp(initHeight, height, (float)(y - initPositionY) / (float)(worldPositionY - initPositionY));
                x = (int)initPositionX + (int)((float)(y - initPositionY) / (float)(worldPositionY - initPositionY) * (worldPositionX - initPositionX));

                for (var yy = 0; yy < 20; yy++)
                {
                    for (var xx = 0; xx < 20; xx++)
                    {
                        if (virtualHeights[yy - 10 + y, xx - 10 + x] < actualHeight / terrainData.size.y)
                            virtualHeights[yy - 10 + y, xx - 10 + x] = actualHeight / terrainData.size.y;
                        if (alphas[yy - 10 + y, xx - 10 + x] < 1f)
                            alphas[yy - 10 + y, xx - 10 + x] = 1f;
                    }
                }
            }
        }
    }

    public void ApplyTerrain(bool filled, int terrainType, float xMin, float xMax, float yMin, float yMax, float radius, float erosionStrength)
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

        if (filled)
            modifier.ModifyTerrain(terrainType, erosionStrength / 2f, 8);
        else
            modifier.ModifyTerrain(terrainType, erosionStrength, 15);
        for (var y = 0; y < GetHeightmapResolution(); y++)
        {
            for (var x = 0; x < GetHeightmapResolution(); x++)
            {
                virtualHeights[y, x] = 0f;
                alphas[y, x] = 0f;
            }
        }
    }

    public void ApplyFilledTerrain(bool canyon, Vector3 worldPosition, Vector3 initialPosition, 
                                   int terrainType, float xMin, float xMax, float yMin, float yMax, float radius, float erosionStrength)
    {
        if (worldPosition.x != initialPosition.x)
        {
            float start = Mathf.Min(worldPosition.x, initialPosition.x);
            float end = Mathf.Max(worldPosition.x, initialPosition.x);

            float y, z;

            while (start < end)
            {
                y = initialPosition.y + (float)(start - initialPosition.x) / (float)(worldPosition.x - initialPosition.x) * (worldPosition.y - initialPosition.y);
                z = initialPosition.z + (float)(start - initialPosition.x) / (float)(worldPosition.x - initialPosition.x) * (worldPosition.z - initialPosition.z);
                if (canyon)
                    LowerTerrain(new Vector3(start, _targetTerrain.transform.position.y, z),
                                 (y - (_targetTerrain.transform.position.y + terrainOffset)) / 4f,
                                 100, 100);
                else
                    RaiseTerrain(new Vector3(start, _targetTerrain.transform.position.y, z),
                                 (y - (_targetTerrain.transform.position.y + terrainOffset)) / 4f,
                                 100, 100);
                start += 1f;
            }
        }
        else
        {
            float start = Mathf.Min(worldPosition.z, initialPosition.z);
            float end = Mathf.Max(worldPosition.z, initialPosition.z);

            float x, y;

            while (start < end)
            {
                x = initialPosition.x + (float)(start - initialPosition.z) / (float)(worldPosition.z - initialPosition.z) * (worldPosition.x - initialPosition.x);
                y = initialPosition.y + (float)(start - initialPosition.z) / (float)(worldPosition.z - initialPosition.z) * (worldPosition.y - initialPosition.y);
                if (canyon)
                    LowerTerrain(new Vector3(x, _targetTerrain.transform.position.y, start),
                                 (y - (_targetTerrain.transform.position.y + terrainOffset)) / 4f,
                                 50, 50);
                else
                    RaiseTerrain(new Vector3(x, _targetTerrain.transform.position.y, start),
                                 (y - (_targetTerrain.transform.position.y + terrainOffset)) / 4f,
                                 50, 50);
                start += 1f;
            }
        }

        ApplyTerrain(true, terrainType, xMin, xMax, yMin, yMax, radius, erosionStrength);
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