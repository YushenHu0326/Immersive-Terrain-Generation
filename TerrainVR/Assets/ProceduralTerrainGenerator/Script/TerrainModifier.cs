using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Barracuda;

public class TerrainModifier : MonoBehaviour
{
    private Terrain terrain;
    private TerrainData terrainData;
    private float maxHeight;
    private float minHeight;
    private float maxColor;
    private int terrainType;

    public float[,] heights;
    public float[,] alphas;
    public int xOffset, yOffset, range;
    public float terrainOffset = 50f;
    private float erosionStrength;
    private int blurStrength;

    private Model model;
    public NNModel mountainModel;
    public NNModel canyonModel;
    public NNModel glacierModel;

    private IWorker worker;

    public void LoadModel(int i)
    {
        if (i == 0) model = ModelLoader.Load(mountainModel);
        else if (i == 1) model = ModelLoader.Load(canyonModel);
        else if (i == 2) model = ModelLoader.Load(glacierModel);

        terrainType = i;

        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputeRef, model);
    }

    public void GetDefaultTerrain()
    {
        terrain = Object.FindObjectOfType<Terrain>();
        terrainData = terrain.terrainData;
        heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        xOffset = 0;
        yOffset = 0;
        range = terrainData.heightmapResolution;
        alphas = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                alphas[y, x] = 1f;
            }
        }
    }

    void GrabTerrainData()
    {
        terrain = Object.FindObjectOfType<Terrain>();
        terrainData = terrain.terrainData;
        maxHeight = 0.01f;
        minHeight = 1.01f;

        for (int y = 0; y < range; y++)
        {
            for (int x = 0; x < range; x++)
            {
                if (heights[y + yOffset, x + xOffset] > maxHeight) 
                    maxHeight = heights[y + yOffset, x + xOffset];
                if (heights[y + yOffset, x + xOffset] < minHeight) 
                    minHeight = heights[y + yOffset, x + xOffset];
            }
        }
    }

    Texture2D RetrieveTerrainHeightmap()
    {
        Texture2D tex = new Texture2D(range, range, TextureFormat.ARGB32, false);

        for (int y = 0; y < range; y++)
        {
            for (int x = 0; x < range; x++)
            {
                tex.SetPixel(x, y, new Color((heights[y + yOffset, x + xOffset] - minHeight) / (maxHeight - minHeight),
                                             (heights[y + yOffset, x + xOffset] - minHeight) / (maxHeight - minHeight),
                                             (heights[y + yOffset, x + xOffset] - minHeight) / (maxHeight - minHeight), 1));
            }
        }

        return tex;
    }

    float[] ProcessHeightmap(Texture2D tex)
    {
        tex.Apply();
        RenderTexture rt = RenderTexture.GetTemporary(256, 256, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        RenderTexture.active = rt;

        Graphics.Blit(tex, rt);
        tex.Resize(256, 256, tex.format, true);
        tex.filterMode = FilterMode.Bilinear;
        tex.ReadPixels(new Rect(0.0f, 0.0f, 256, 256), 0, 0);
        tex.Apply();

        ParallelGaussianBlur blur = gameObject.AddComponent<ParallelGaussianBlur>();
        blur.Radial = blurStrength;

        blur.GaussianBlur(ref tex);

        for (int h = 0; h < 256; h++)
        {
            for (int w = 0; w < 256; w++)
            {
                float r = tex.GetPixel(w, h).r;
                float g = tex.GetPixel(w, h).g;
                float b = tex.GetPixel(w, h).b;

                float n = Mathf.PerlinNoise((float)w / 25f, (float)h / 25f);
                
                r *= (n * 0.4f * erosionStrength / 100f + 1f);
                g *= (n * 0.4f * erosionStrength / 100f + 1f);
                b *= (n * 0.4f * erosionStrength / 100f + 1f);
                
                r = r * 5f;
                g = g * 5f;
                b = b * 5f;

                r = Mathf.Floor(r);
                g = Mathf.Floor(g);
                b = Mathf.Floor(b);

                r = r / 5f;
                g = g / 5f;
                b = b / 5f;

                tex.SetPixel(w, h, new Color(r, g, b));
            }
        }

        byte[] bytes = tex.EncodeToPNG();

        System.IO.File.WriteAllBytes(Application.dataPath + "/h.png", bytes);

        float[] values = new float[196608];

        for (int h = 0; h < 256; h++)
        {
            for (int w = 0; w < 256; w++)
            {
                values[w * 3 + h * 256 * 3] = tex.GetPixel(w, h).r * 2f - 1f;
                values[w * 3 + h * 256 * 3 + 1] = tex.GetPixel(w, h).g * 2f - 1f;
                values[w * 3 + h * 256 * 3 + 2] = tex.GetPixel(w, h).b * 2f - 1f;
            }
        }

        DestroyImmediate(blur);

        return values;
    }

    Texture2D RestoreHeightmap(Texture2D tex)
    {
        /*
        ParallelGaussianBlur blur = gameObject.AddComponent<ParallelGaussianBlur>();
        blur.Radial = 1;

        blur.GaussianBlur(ref tex);
        blur.GaussianBlur(ref tex);
        DestroyImmediate(blur);
        */

        tex.Apply();
        RenderTexture rt = RenderTexture.GetTemporary(range, range, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        RenderTexture.active = rt;

        Graphics.Blit(tex, rt);
        tex.Resize(range, range, tex.format, true);
        tex.filterMode = FilterMode.Bilinear;
        tex.ReadPixels(new Rect(0.0f, 0.0f, range, range), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();

        System.IO.File.WriteAllBytes(Application.dataPath + "/h0.png", bytes);

        return tex;
    }

    void WriteTerrainData(Texture2D tex)
    {
        Texture2D atex = new Texture2D(terrainData.heightmapResolution, terrainData.heightmapResolution, TextureFormat.ARGB32, false);
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                atex.SetPixel(x, y, new Color(alphas[y, x], alphas[y, x], alphas[y, x], 1f));
            }
        }

        byte[] bytes = atex.EncodeToPNG();

        System.IO.File.WriteAllBytes(Application.dataPath + "/a.png", bytes);

        float[,] originHeights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        float[,] newHeights = new float[range, range];

        maxColor = 0f;

        for (int y = 0; y < range; y++)
        {
            for (int x = 0; x < range; x++)
            {
                if (tex.GetPixel(x, y).r > maxColor) maxColor = tex.GetPixel(x, y).r;
            }
        }

        for (int y = 0; y < range; y++)
        {
            for (int x = 0; x < range; x++)
            {
                if (terrainType != 1)
                    newHeights[y, x] = Mathf.Max(
                                       originHeights[y + yOffset, x + xOffset],
                                       Mathf.Lerp(tex.GetPixel(x, y).r / maxColor * (maxHeight - minHeight) + minHeight + terrainOffset / terrainData.size.y,
                                                  originHeights[y + yOffset, x + xOffset],
                                                  1f - alphas[y + yOffset, x + xOffset]
                                       )
                                       );
                else
                    newHeights[y, x] = Mathf.Min(
                                       originHeights[y + yOffset, x + xOffset],
                                       Mathf.Lerp(tex.GetPixel(x, y).r / maxColor *  terrainOffset / terrainData.size.y,
                                                  originHeights[y + yOffset, x + xOffset],
                                                  1f - alphas[y + yOffset, x + xOffset]
                                       )
                                       );
            }
        }

        terrainData.SetHeights(xOffset, yOffset, newHeights);
    }

    public void ModifyTerrain(int i, float erosionStrength, int blurStrength)
    {
        this.erosionStrength = erosionStrength;
        this.blurStrength = blurStrength;

        LoadModel(i);
        GrabTerrainData();
        Texture2D tex = RetrieveTerrainHeightmap();
        float[] values = ProcessHeightmap(tex);

        Tensor input = new Tensor(1, 256, 256, 3, values);

        Tensor output = worker.Execute(input).PeekOutput();
        float[] newValues = output.AsFloats();
        input.Dispose();
        worker?.Dispose();

        Texture2D newTex = new Texture2D(256, 256, TextureFormat.ARGB32, false);

        for (int h = 0; h < 256; h++)
        {
            for (int w = 0; w < 256; w++)
            {
                newTex.SetPixel(w, h, new Color(newValues[w * 3 + h * 256 * 3] * 0.5f + 0.5f,
                                                newValues[w * 3 + h * 256 * 3 + 1] * 0.5f + 0.5f,
                                                newValues[w * 3 + h * 256 * 3 + 2] * 0.5f + 0.5f));
            }
        }

        Texture2D modifiedTex = RestoreHeightmap(newTex);

        WriteTerrainData(modifiedTex);
    }

    public void ModifyMountain()
    {
        ModifyTerrain(0, 100f, 15);
    }

    public void ModifyCanyon()
    {
        ModifyTerrain(1, 100f, 15);
    }

    public void ModifyGlacier()
    {
        ModifyTerrain(2, 100f, 15);
    }

    public void OnDestroy()
    {
        worker?.Dispose();
    }
}
