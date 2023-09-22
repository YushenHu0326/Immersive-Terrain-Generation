using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ApplicationEditor : EditorWindow
{
    [MenuItem("Window/Terrain Modifier")]
    public static void ShowWindow()
    {
        GetWindow<ApplicationEditor>("Terrain Modifier");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Mountain"))
        {
            ModifyTerrain(0);
        }

        if (GUILayout.Button("Canyon"))
        {
            ModifyTerrain(1);
        }

        if (GUILayout.Button("Glacier"))
        {
            ModifyTerrain(2);
        }
    }

    private void ModifyTerrain(int i)
    {
        TerrainModifier[] terrains = FindObjectsOfType<TerrainModifier>();
        if (terrains.Length > 0)
        {
            terrains[0].GetDefaultTerrain();
            terrains[0].ModifyTerrain(i, 100f, 15);
        }
    }
}
