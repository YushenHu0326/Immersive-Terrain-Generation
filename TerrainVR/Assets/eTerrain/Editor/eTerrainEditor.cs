/// <summary>
/// Original work by Mika Islander, Rougelike Games. 2018.
/// http://www.rougelikegames.com
/// this script is a part of the "eTerrain" asset package.
/// </summary>

using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(eTerrain))]
public class eTerrainEditor: Editor {

	public override void OnInspectorGUI(){
		
		DrawDefaultInspector();
		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		eTerrain easyTerrain = (eTerrain)target;
		EditorGUILayout.BeginHorizontal();
		GUI.tooltip = "fjsjfjsf";
		if (GUILayout.Button("Help")){
			if(EditorUtility.DisplayDialog("Help","Welcome to eTerrain.\n\nAdjust the values in the inspector according to your preferences and click the buttons below to apply the corresponding changes.\n\n-\"Smooth\" autosmooths the whole terrain(good for getting rid of artifacts and bad .raw imports).\n\n" +
				"-\"Splat\" automatically assigns a splatmap from your chosen textures to your terrain(saves you enormous amounts of time from having to manually paint everything).\n\n" +
				"-\"Level\" modifies the height values on your terrain based on your texture weights(good for making ditches, holes, bumps, roads, and whatever else).\n\n" +
				"-\"Stitch\" visually stitches selected terrains together getting rid of all and any visible gaps between them(a must-have when working with multiple terrains. This algorithm is pixel perfect).\n\n" +
				"-\"Reset Splats\" resets all custom splat weights on your terrain returning them to default(be careful, this will indeed reset your terrain splatmap).\n\n" +
				"-\"Min\" flattens the whole terrain to a height value of 0 which is the lowest possible height on your terrain(not ideal most of the time since you cannot shape your terrain lower than this).\n\n" +
				"-\"Average\" flattens the whole terrain to a height value of 0.5 which is the exact half of your terrains maximum height(this is the most ideal base level value because it allows you to shape the terrain as much below, as above).\n\n" +
				"-\"Max\" flattens the whole terrain to a height value of 1 which is the maximum height of your terrain(this is not ideal most of the time since you cannot shape your terrain higher than this).\n\n" +
				"It is recommended you prototype this tool on a test-terrain to get a better idea of how the different options function. Also, be sure to hover over all the different variables to view their corresponding tooltips in order to get a better understanding of what the variables stand for and how they should be used.\n\n" +
				"Enjoy working with eTerrain! :) -RougelikeGames", "Ok")){
			}
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginVertical();
		EditorGUILayout.EndVertical();
		Rect scale = GUILayoutUtility.GetLastRect();
		GUILayout.Space(96);
		EditorGUI.TextArea(new Rect(scale.position.x, scale.position.y, scale.width, 96),( "\n" +
			"WARNING: When working with terrain data\nthere is no undo, so back-up is recommended.\n" +
			"This is a common practice when working with\ntools that manipulate the terrain as a whole.\n" +
			"Press \"Help\" to open quick guide."));

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Smooth")){
			if(EditorUtility.DisplayDialog("Smoothing", "You are about to apply smoothing on your whole terrain.", "Ok","Cancel")){;
			easyTerrain.Smooth();
			}
		}
		if (GUILayout.Button("Splat")){
			if (easyTerrain.gameObject.GetComponent<Terrain>().terrainData.alphamapResolution == easyTerrain.gameObject.GetComponent<Terrain>().terrainData.heightmapResolution -1){
				if(EditorUtility.DisplayDialog("Splatmapping", "You are about to automatically calculate splatmaps to your terrain. This will overwrite all of your pre-existing texture weights on this terrain.", "Ok","Cancel")){;
			easyTerrain.Splat();
				}
			}
			if (easyTerrain.gameObject.GetComponent<Terrain>().terrainData.alphamapResolution != easyTerrain.gameObject.GetComponent<Terrain>().terrainData.heightmapResolution -1){
				EditorUtility.DisplayDialog("Wait!", "At the moment it's not possible to calculate the splatmaps automatically if your \"Control Texture Resolution\" does not match your \"Heightmap Resolution(-1)\". Please do change these values in the terrain settings and try again.", "Ok");
			}
		}
		if (GUILayout.Button("Level")){
			if(EditorUtility.DisplayDialog("Re-leveling", "You are about to apply re-leveling to your terrains' height values based on your splatmap data. This process is additive and does not reset your heightmap data.", "Ok","Cancel")){;
			easyTerrain.Level();
			}
		}
		if (GUILayout.Button("Stitch")){
			if(EditorUtility.DisplayDialog("Stitching", "You are about to stitch this terrain together with the assigned terrain. This process automatically snaps the assigned terrain to the correct world position.", "Ok","Cancel")){;
			easyTerrain.Stitch();
			}
		}
		EditorGUILayout.EndHorizontal();
		GUILayout.Space(1);
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Reset Splats")){
			if(EditorUtility.DisplayDialog("Resetting Splatmaps", "You are about to reset the terrain splatmaps. This will reset all the pre-existing texture weights on this terrain.", "Ok","Cancel")){
			easyTerrain.ResetSplats();
			}
		}
		if (GUILayout.Button("Min")){
			if(EditorUtility.DisplayDialog("Flattening", "You are about to flatten your terrain to it's minimum height value. This will reset all the pre-existing heightmap data on your current terrain.", "Ok","Cancel")){;
			easyTerrain.Min();
			}
		}
		if (GUILayout.Button("Average")){
			if(EditorUtility.DisplayDialog("Flattening", "You are about to flatten your terrain to it's average height value. This will reset all the pre-existing heightmap data on your current terrain.", "Ok","Cancel")){;
			easyTerrain.Baselevel();
			}
		}
		if (GUILayout.Button("Max")){
			if(EditorUtility.DisplayDialog("Flattening", "You are about to flatten your terrain to it's maximum height value. This will reset all the pre-existing heightmap data on your current terrain.", "Ok","Cancel")){
			easyTerrain.Max();
			}
		}
		EditorGUILayout.EndHorizontal();
	}
}
