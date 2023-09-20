/// <summary>
/// Original work by Mika Islander, Rougelike Games. 2018.
/// http://www.rougelikegames.com
/// this script is a part of the "eTerrain" asset package.
/// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class eTerrain : MonoBehaviour {

	[Header("Enter smoothing iteration count")]
	[Range(1,10)]
	[Tooltip("How many times do you wish to iterate the smoothing algorithm before applying the calculations? Note: High iteration counts take a bit longer to calculate")]
	public int smoothingIterations = 1;
	public void Smooth() {

		Terrain thisterrain = GetComponent<Terrain>();
		TerrainData tData = thisterrain.terrainData;

		int tWidth = tData.heightmapResolution;
		int tHeight = tData.heightmapResolution;
		float[,] heightPoints = tData.GetHeights(xBase:0, yBase:0, width:tWidth, height:tHeight);
		float curHeight = heightPoints[4,4];
		float curHeight2 = heightPoints[4,4];

		for(int i = 0; i < smoothingIterations; i++){
			for(int w = 0; w < (tWidth - 1); w++){
				if (w > 1){
					for(int h = 0; h < (tHeight - 1); h++){
						if(h > 1){
							if ( heightPoints[w,h] != curHeight){
								heightPoints[w,h] = (heightPoints[w,h] + curHeight) / 2;
							}
						}
						curHeight = heightPoints[w,h];
					}
				}
			}
			for(int h = (tHeight - 1); h > 0; h--){
				if (h < (tHeight - 1)){
					for(int w = (tWidth - 1 ); w > 0; w--){
						if(w < (tWidth -1 )){
							if ( heightPoints[w,h] != curHeight2){
								heightPoints[w,h] = (heightPoints[w,h] + curHeight2) / 2;
							}
						}
						curHeight2 = heightPoints[w,h];
					}
				}
			}
		}
		tData.SetHeights(0,0,heightPoints);
		Debug.Log("Smoothing completed successfully!");
	}

	[Space(10)]
	[Header("Assign autosplatmap values")]
	[Tooltip("Which texture do you wish to use for ground areas above the desired baselevel? Enter the texture index here. This is the so called basic ground which covers most of your terrain.\n\nNote: the terrain texture numbering starts from index 0 so the first texture is 0, the second one is 1 and so forth...")]
	public int flatGroundTexNo = 0;
	[Tooltip("Which texture do you wish to use for ground areas below the desired baselevel? Enter the texture index here. This is basically everything below your basic ground, holes, crevices, etc.\n\nNote: the terrain texture numbering starts from index 0 so the first texture is 0, the second one is 1 and so forth...")]
	public int belowBaselevelTexNo = 1;
	[Tooltip("Which texture do you wish to use for angled surfaces above the desired baselevel? Enter the texture index here. This is all the slopes and rises above your ground level, stuff like cliffs and mountains etc.\n\nNote: the terrain texture numbering starts from index 0 so the first texture is 0, the second one is 1 and so forth...")]
	public int highSteepTexNo = 2;
	[Tooltip("Which texture do you wish to use for angled surfaces below the desired baselevel? Enter the texture index here. This is all the slopes and depressions below your ground level.\n\nNote: the terrain texture numbering starts from index 0 so the first texture is 0, the second one is 1 and so forth..")]
	public int lowSteepTexNo = 3;
	[Range(0,1f)]
	[Tooltip("Baselevel is the ground level of your terrain. If you flatten your terrain to \"Average\" the baselevel will be exactly 0.5 because that's the exact half of the maximum height of your terrain.\n\nIf you're using heightmaps you might need to try different values _usually_ between 0.4 and 0.6 to find your desired ground level.")]
	public float baselevelHeight = 0.5f;
	[Range(0.001f,0.999f)]
	[Tooltip("The strength of texture weights on angled surfaces in relation to the angle steepness. Lower values make the angled surface weights more attenuated while higher values make them more prominent. Experiment to get the best results.")]
	public float steepnessFactor = 0.5f;

	public void Splat () {
		float storeFloat = steepnessFactor;
		steepnessFactor = 1 - steepnessFactor;
		steepnessFactor *= 20000;
		Terrain terrain = GetComponent<Terrain>();
		TerrainData tData = terrain.terrainData;
		float[, ,] splatmapData = new float[tData.alphamapWidth, tData.alphamapHeight, tData.alphamapLayers];
		float terrainTotalHeight = tData.heightmapScale.y;
		baselevelHeight -= 0.01f;
		int pow2HmapRes = tData.heightmapResolution - 1;
		int texRes = tData.alphamapResolution;
		int multiplier = 1;

		if (pow2HmapRes < texRes){
			multiplier = (texRes / pow2HmapRes);
		}
		if (pow2HmapRes > texRes){
			multiplier = (pow2HmapRes / texRes);
		}

		for (int h = 0; h < texRes; h++)
		{
			for (int w = 0; w < texRes; w++)
			{
				float h_normalized = (float)h/((float)tData.alphamapHeight / multiplier);
				float w_normalized = (float)w/((float)tData.alphamapWidth / multiplier);
				float steepness = tData.GetSteepness(h_normalized,w_normalized);
				float[] splatWeights = new float[tData.alphamapLayers];
				float height = tData.GetHeight(h,w);

				if (height >= ((baselevelHeight + 0.005f) * terrainTotalHeight)){
					splatWeights[flatGroundTexNo] = Mathf.Clamp01(1f - Mathf.Clamp01(height* Mathf.Clamp01(steepness/steepnessFactor)));
				}
				if (height > ((baselevelHeight + 0.01f) * terrainTotalHeight)){
					splatWeights[highSteepTexNo] = Mathf.Clamp01(height* Mathf.Clamp01(steepness/steepnessFactor));
				}
				if (height <= ((baselevelHeight + 0.01f) * terrainTotalHeight)){
					splatWeights[lowSteepTexNo] = Mathf.Clamp01(height* Mathf.Clamp01(steepness/steepnessFactor));
				}
				if (height < ((baselevelHeight + 0.001f) * terrainTotalHeight)){
					splatWeights[belowBaselevelTexNo] = Mathf.Clamp01(1f - height* Mathf.Clamp01(steepness/steepnessFactor));
				}
				if (height >= (baselevelHeight + 0.004f) * terrainTotalHeight && height < (baselevelHeight + 0.005f) * terrainTotalHeight){
					splatWeights[belowBaselevelTexNo] = Mathf.Clamp01(0.2f - height* Mathf.Clamp01(steepness/steepnessFactor) * (height / (terrainTotalHeight)));
					splatWeights[flatGroundTexNo] = Mathf.Clamp01(0.8f - height* Mathf.Clamp01(steepness/steepnessFactor) * (height / (terrainTotalHeight)));
				}
				if (height >= (baselevelHeight + 0.003f) * terrainTotalHeight && height < (baselevelHeight + 0.004f) * terrainTotalHeight){
					splatWeights[belowBaselevelTexNo] = Mathf.Clamp01(0.4f - height* Mathf.Clamp01(steepness/steepnessFactor) * (height / (terrainTotalHeight)));
					splatWeights[flatGroundTexNo] = Mathf.Clamp01(0.6f - height* Mathf.Clamp01(steepness/steepnessFactor) * (height / (terrainTotalHeight)));
				}
				if (height >= (baselevelHeight + 0.002f) * terrainTotalHeight && height < (baselevelHeight + 0.003f) * terrainTotalHeight){
					splatWeights[belowBaselevelTexNo] = Mathf.Clamp01(0.6f - height* Mathf.Clamp01(steepness/steepnessFactor) * (height / (terrainTotalHeight)));
					splatWeights[flatGroundTexNo] = Mathf.Clamp01(0.4f - height* Mathf.Clamp01(steepness/steepnessFactor) * (height / (terrainTotalHeight)));
				}
				if (height >= (baselevelHeight + 0.001f) * terrainTotalHeight && height < (baselevelHeight + 0.002f) * terrainTotalHeight){
					splatWeights[belowBaselevelTexNo] = Mathf.Clamp01(0.8f - height* Mathf.Clamp01(steepness/steepnessFactor) * (height / (terrainTotalHeight)));
					splatWeights[flatGroundTexNo] = Mathf.Clamp01(0.2f - height* Mathf.Clamp01(steepness/steepnessFactor) * (height / (terrainTotalHeight)));
				}
				float z = splatWeights.Sum();
				if (z == 0){
					splatWeights[flatGroundTexNo] = 1f;
				}
				for(int i = 0; i<tData.alphamapLayers; i++){

					if (pow2HmapRes == texRes){
						splatWeights[i] /= z;
						splatmapData[w, h, i] = splatWeights[i];
					}
				}
			}
		}
		if (texRes == pow2HmapRes){
		tData.SetAlphamaps(0, 0, splatmapData);
		baselevelHeight += 0.01f;
		steepnessFactor = storeFloat;
		Debug.Log("Splatmapping completed successfully!");
		}
	}
	[Space(10)]
	[Header("Assign texture re-leveling values")]
	[Tooltip("If you wish to apply noise to the height values of any part of the terrain textured with the textures of your choice. Select the corresponding elements below.\n\nFor an example if you choose \"Element 0\"; noise will be applied to all the areas of the terrain with \"Texture 0\" weights on it.")]
	public bool[] ApplyNoise = new bool[4];
	[Range(-10, 10)]
	[Tooltip("Adjust the depression/elevation value for the parts of the terrain painted with Texture index \"0\" weights. Negative values depress the terrain and positive values elevate the terrain at the weighted areas")]
	public int tex0ReLevel = 0;
	[Range(-10, 10)]
	[Tooltip("Adjust the depression/elevation value for the parts of the terrain painted with Texture index \"1\" weights. Negative values depress the terrain and positive values elevate the terrain at the weighted areas")]
	public int tex1ReLevel = 0;
	[Range(-10, 10)]
	[Tooltip("Adjust the depression/elevation value for the parts of the terrain painted with Texture index \"2\" weights. Negative values depress the terrain and positive values elevate the terrain at the weighted areas")]
	public int tex2ReLevel = 0;
	[Range(-10, 10)]
	[Tooltip("Adjust the depression/elevation value for the parts of the terrain painted with Texture index \"3\" weights. Negative values depress the terrain and positive values elevate the terrain at the weighted areas")]
	public int tex3ReLevel = 0;

	public void Level(){

		Terrain thisTerrain = GetComponent<Terrain>();
		TerrainData tData = thisTerrain.terrainData;
		Texture2D splatMapTexture = tData.alphamapTextures[0];
		int tSize = tData.heightmapResolution;
		Texture2D reSizedSplat = new Texture2D(tSize, tSize);
		int textRes = splatMapTexture.width;
		int realHmapRes = tData.heightmapResolution-1;
		float[,] heightValues = tData.GetHeights(xBase:0, yBase:0, width:tSize, height:tSize);
		int multiplierRatio = 1;

		if (realHmapRes < textRes){
			multiplierRatio = (textRes / realHmapRes);
		}
		if (realHmapRes > textRes){
			multiplierRatio = (realHmapRes / textRes);
		}
			
		for(int w = 0; w < tSize; w++){
			for(int h = 0; h < tSize; h++){
				if (textRes > realHmapRes){
					reSizedSplat.SetPixel(w,h, splatMapTexture.GetPixel(w*multiplierRatio,h*multiplierRatio));
				}
				if (textRes < realHmapRes){
					reSizedSplat.SetPixel(w,h, splatMapTexture.GetPixel(w/multiplierRatio,h/multiplierRatio));
				}
				if(textRes == realHmapRes){
					reSizedSplat.SetPixel(w,h, splatMapTexture.GetPixel(w,h));
				}
			}
		}

		for(int w = 0; w < tSize; w++){
			for(int h = 0; h < tSize; h++){
				heightValues[w,h] = heightValues[w,h] + ((reSizedSplat.GetPixel(h,w).r * 0.002f) * tex0ReLevel);
				heightValues[w,h] = heightValues[w,h] + ((reSizedSplat.GetPixel(h,w).g * 0.002f) * tex1ReLevel);
				heightValues[w,h] = heightValues[w,h] + ((reSizedSplat.GetPixel(h,w).b * 0.002f) * tex2ReLevel);
				heightValues[w,h] = heightValues[w,h] + ((reSizedSplat.GetPixel(h,w).a * 0.002f) * tex3ReLevel);
				if (ApplyNoise[0] == true && reSizedSplat.GetPixel(h,w).r > 0.2f){
					heightValues[w,h] = heightValues[w,h] * Random.Range(0.997f, 1.003f);
				}
				if (ApplyNoise[1] == true && reSizedSplat.GetPixel(h,w).g > 0.2f){
					heightValues[w,h] = heightValues[w,h] * Random.Range(0.997f, 1.003f);
				}
				if (ApplyNoise[2] == true && reSizedSplat.GetPixel(h,w).b > 0.2f){
					heightValues[w,h] = heightValues[w,h] * Random.Range(0.997f, 1.003f);
				}
				if (ApplyNoise[3] == true && reSizedSplat.GetPixel(h,w).a > 0.2f){
					heightValues[w,h] = heightValues[w,h] * Random.Range(0.997f, 1.003f);
				}
			}
		}
		tData.SetHeights(0,0,heightValues);
		tex0ReLevel = 0;
		tex1ReLevel = 0;
		tex2ReLevel = 0;
		tex3ReLevel = 0;
		ApplyNoise[0] = false;
		ApplyNoise[1] = false;
		ApplyNoise[2] = false;
		ApplyNoise[3] = false;
		Debug.Log("Re-leveling completed succesfully!");
	}

	public void ResetSplats(){

		Terrain thisTerrain = GetComponent<Terrain>();
		TerrainData tData = thisTerrain.terrainData;
		int tSize = tData.alphamapResolution;
		float[, ,] splatmapData = new float[tData.alphamapWidth, tData.alphamapHeight, tData.alphamapLayers];

		for (int w = 0; w < tSize; w++){
			for (int h = 0; h < tSize; h++){
				for (int i = 0; i < tData.alphamapLayers; i++){
					splatmapData[w,h,i] = 0;
					splatmapData[w,h,0] = 1;
				}
			}
		}
		tData.SetAlphamaps(0,0,splatmapData);
		Debug.Log("Splatmap data has been reset.");
	}

	public void Min(){

		Terrain thisTerrain = GetComponent<Terrain>();
		TerrainData tData = thisTerrain.terrainData;
		int tSize = tData.heightmapResolution;
		float[,] heightValues = tData.GetHeights(xBase:0, yBase:0, width:tSize, height:tSize);

		for(int w = 0; w < tSize; w++){
			for(int h = 0; h < tSize; h++){
				heightValues[w,h] = 0;
			}
		}
		tData.SetHeights(0,0,heightValues);
		Debug.Log("Terrain flattened to minimum height value");
	}
	public void Baselevel(){

		Terrain thisTerrain = GetComponent<Terrain>();
		TerrainData tData = thisTerrain.terrainData;
		int tSize = tData.heightmapResolution;
		float[,] heightValues = tData.GetHeights(xBase:0, yBase:0, width:tSize, height:tSize);

		for(int w = 0; w < tSize; w++){
			for(int h = 0; h < tSize; h++){
				heightValues[w,h] = 0.5f;
			}
		}
		tData.SetHeights(0,0,heightValues);
		Debug.Log("Terrain flattened to average height value");
	}
	public void Max(){

		Terrain thisTerrain = GetComponent<Terrain>();
		TerrainData tData = thisTerrain.terrainData;
		int tSize = tData.heightmapResolution;
		float[,] heightValues = tData.GetHeights(xBase:0, yBase:0, width:tSize, height:tSize);

		for(int w = 0; w < tSize; w++){
			for(int h = 0; h < tSize; h++){
				heightValues[w,h] = 1;
			}
		}
		tData.SetHeights(0,0,heightValues);
		Debug.Log("Terrain flattened to maximum height value");
	}
	[Space(10)]
	[Header("Stitch multiple terrains together")]
	[Tooltip("Insert the desired terrain here that you wish to stitch this terrain with")]
	public Terrain stitchWithThisTerrain;
	[Range(1,10)]
	[Tooltip("This variable allows you to average the height values surrounding the stitching seams with a procedural algorithm. Basically it smoothens the transition between the 2 terrains at the seam area.\n\n" +
		"Uses arbitrary values. 1 is quite conservative while 10 is more clearly visible. Higher amount of iterations and a longer smoothing distance gives a smoother end result but also makes the seam area more pronounced so experiment to find the best match to suit your personal needs.\n\n" +
		"NOTICE: The more drastic the gaps between your 2 terrains the more visible the transition between them so obviously you want to have your neighbouring terrains look as continuous as possible at the seam areas before stitching them together.")]
	public int stitchSmoothingDistance;
	[Range(1,10)]
	[Tooltip("Higher amount of iterations and a longer smoothing distance gives a smoother end result but also makes the seam area more pronounced so experiment to find the best match to suit your personal needs.")]
	public int iterations = 1;

	public void Stitch(){
		
		stitchSmoothingDistance *= 33;

		Terrain thisterrain = GetComponent<Terrain>();
		TerrainData tData = thisterrain.terrainData;
		TerrainData stData = stitchWithThisTerrain.terrainData;

		int tWidth = tData.heightmapResolution;
		int tHeight = tData.heightmapResolution;
		float[,] heightPoints = tData.GetHeights(xBase:0, yBase:0, width:tWidth, height:tHeight);
		float[,] stheightPoints = stData.GetHeights(xBase:0, yBase:0, width:tWidth, height:tHeight);
		float[] theseHeights = new float[tHeight + 2];
		float[] stitcherHeights = new float[tHeight + 2];
		float[] lastRowHeights = new float[tHeight + 2];

		Transform thisTerTrans = thisterrain.transform;
		Transform stitchTerTrans = stitchWithThisTerrain.transform;
		Vector3 thisTerPos = thisterrain.transform.position;
		Vector3 stitchTerPos = stitchWithThisTerrain.transform.position;

		Vector3 topAnchor = new Vector3(thisTerPos.x, thisTerPos.y, thisTerPos.z + stData.size.z);
		Vector3 bottomAnchor = new Vector3(thisTerPos.x, thisTerPos.y, thisTerPos.z - stData.size.z);
		Vector3 leftAnchor = new Vector3(thisTerPos.x - stData.size.x, thisTerPos.y, thisTerPos.z);
		Vector3 rightAnchor = new Vector3(thisTerPos.x + stData.size.x, thisTerPos.y, thisTerPos.z);

		if(Vector3.Distance(stitchTerPos, leftAnchor) < Vector3.Distance(stitchTerPos, topAnchor) && Vector3.Distance(stitchTerPos, leftAnchor) < Vector3.Distance(stitchTerPos, rightAnchor) && Vector3.Distance(stitchTerPos, leftAnchor) < Vector3.Distance(stitchTerPos, bottomAnchor)){
			stitchTerTrans.SetPositionAndRotation(thisTerPos, thisTerTrans.rotation);
			stitchTerTrans.SetPositionAndRotation(new Vector3(thisTerPos.x - tData.size.x, thisTerPos.y, thisTerPos.z), stitchTerTrans.rotation);
			for (int i = 0; i < iterations; i++){
				for(int h = 0; h < tHeight; h++){
					theseHeights[h] = heightPoints[h,0];
				}
				for(int nh = 0; nh < tHeight; nh++){
					stitcherHeights[nh] = stheightPoints[nh,tHeight-1];
				}
				for(int s = 0; s < tHeight; s++){
					heightPoints[s,0] = (theseHeights[s] + stitcherHeights[s]) / 2;
					stheightPoints[s,tHeight-1] = (theseHeights[s] + stitcherHeights[s]) / 2;
				}
				for(int smH = 0; smH < stitchSmoothingDistance; smH++){
					for (int smW = 0; smW < tHeight; smW++){
						if (smH > 0){
							heightPoints[smW, smH] = ((heightPoints[smW, smH] * smH) + lastRowHeights[smW]) / (smH + 1);
						}
						lastRowHeights[smW] = heightPoints[smW,smH];
					}
				}
				for(int smS = (tHeight-1); smS > (tHeight - stitchSmoothingDistance); smS--){
					for (int smW = 0; smW < tHeight; smW++){
						if (smS < (tHeight-1)){
							stheightPoints[smW, smS] = ((stheightPoints[smW, smS] * (tHeight - smS)) + lastRowHeights[smW]) / ((tHeight - smS) + 1);
						}
						lastRowHeights[smW] = stheightPoints[smW,smS];
					}
				}
				tData.SetHeights(0,0,heightPoints);
				stData.SetHeights(0,0,stheightPoints);
			}
		}

		if(Vector3.Distance(stitchTerPos, rightAnchor) < Vector3.Distance(stitchTerPos, topAnchor) && Vector3.Distance(stitchTerPos, rightAnchor) < Vector3.Distance(stitchTerPos, leftAnchor) && Vector3.Distance(stitchTerPos, rightAnchor) < Vector3.Distance(stitchTerPos, bottomAnchor)){
			stitchTerTrans.SetPositionAndRotation(thisTerPos, thisTerTrans.rotation);
			stitchTerTrans.SetPositionAndRotation(new Vector3(thisTerPos.x + tData.size.x, thisTerPos.y, thisTerPos.z), stitchTerTrans.rotation);
			for (int i = 0; i < iterations; i++){
				for(int h = 0; h < tHeight; h++){
					theseHeights[h] = heightPoints[h,tHeight-1];
				}
				for(int nh = 0; nh < tHeight; nh++){
					stitcherHeights[nh] = stheightPoints[nh,0];
				}
				for(int s = 0; s < tHeight; s++){
					heightPoints[s,tHeight-1] = (theseHeights[s] + stitcherHeights[s]) / 2;
					stheightPoints[s,0] = (theseHeights[s] + stitcherHeights[s]) / 2;
				}
				for(int smH = 0; smH < stitchSmoothingDistance; smH++){
					for (int smW = 0; smW < tHeight; smW++){
						if (smH > 0){
							stheightPoints[smW, smH] = ((stheightPoints[smW, smH] * smH) + lastRowHeights[smW]) / (smH + 1);
						}
						lastRowHeights[smW] = stheightPoints[smW,smH];
					}
				}
				for(int smS = (tHeight-1); smS > (tHeight - stitchSmoothingDistance); smS--){
					for (int smW = 0; smW < tHeight; smW++){
						if (smS < (tHeight-1)){
							heightPoints[smW, smS] = ((heightPoints[smW, smS] * (tHeight - smS)) + lastRowHeights[smW]) / ((tHeight - smS) + 1);
						}
						lastRowHeights[smW] = heightPoints[smW,smS];
					}
				}
				tData.SetHeights(0,0,heightPoints);
				stData.SetHeights(0,0,stheightPoints);
			}
		}

		if(Vector3.Distance(stitchTerPos, topAnchor) < Vector3.Distance(stitchTerPos, leftAnchor) && Vector3.Distance(stitchTerPos, topAnchor) < Vector3.Distance(stitchTerPos, rightAnchor) && Vector3.Distance(stitchTerPos, topAnchor) < Vector3.Distance(stitchTerPos, bottomAnchor)){
			stitchTerTrans.SetPositionAndRotation(thisTerPos, thisTerTrans.rotation);
			stitchTerTrans.SetPositionAndRotation(new Vector3(thisTerPos.x, thisTerPos.y, thisTerPos.z + tData.size.z), stitchTerTrans.rotation);
			for(int i = 0; i < iterations; i++){
				for(int h = 0; h < tHeight; h++){
					theseHeights[h] = heightPoints[tHeight-1,h];
				}
				for(int nh = 0; nh < tHeight; nh++){
					stitcherHeights[nh] = stheightPoints[0,nh];
				}
				for(int s = 0; s < tHeight; s++){
					heightPoints[tHeight-1,s] = (theseHeights[s] + stitcherHeights[s]) / 2;
					stheightPoints[0,s] = (theseHeights[s] + stitcherHeights[s]) / 2;
				}
				for(int smH = 0; smH < tHeight-1; smH++){
					for (int smW = tHeight-1; smW > (tHeight - stitchSmoothingDistance); smW--){
						if (smW < tHeight-1){
							heightPoints[smW, smH] = ((heightPoints[smW, smH] * (tHeight - smW)) + lastRowHeights[smH]) / ((tHeight - smW) + 1);
						}
						lastRowHeights[smH] = heightPoints[smW,smH];
					}
				}
				for(int smS = 0; smS < tHeight-1; smS++){
					for (int smW = 0; smW < stitchSmoothingDistance; smW++){
						if (smW > 0){
							stheightPoints[smW, smS] = ((stheightPoints[smW, smS] * smW) + lastRowHeights[smS]) / (smW + 1);
						}
						lastRowHeights[smS] = stheightPoints[smW,smS];
					}
				}
				tData.SetHeights(0,0,heightPoints);
				stData.SetHeights(0,0,stheightPoints);
			}
		}
		if(Vector3.Distance(stitchTerPos, bottomAnchor) < Vector3.Distance(stitchTerPos, leftAnchor) && Vector3.Distance(stitchTerPos, bottomAnchor) < Vector3.Distance(stitchTerPos, rightAnchor) && Vector3.Distance(stitchTerPos, bottomAnchor) < Vector3.Distance(stitchTerPos, topAnchor)){
			stitchTerTrans.SetPositionAndRotation(thisTerPos, thisTerTrans.rotation);
			stitchTerTrans.SetPositionAndRotation(new Vector3(thisTerPos.x, thisTerPos.y, thisTerPos.z - tData.size.z), stitchTerTrans.rotation);
			for(int i = 0; i < iterations; i++){
				for(int h = 0; h < tHeight; h++){
					theseHeights[h] = heightPoints[0,h];
				}
				for(int nh = 0; nh < tHeight; nh++){
					stitcherHeights[nh] = stheightPoints[tHeight-1,nh];
				}
				for(int s = 0; s < tHeight; s++){
					heightPoints[0,s] = (theseHeights[s] + stitcherHeights[s]) / 2;
					stheightPoints[tHeight-1,s] = (theseHeights[s] + stitcherHeights[s]) / 2;
				}
				for(int smH = 0; smH < tHeight-1; smH++){
					for (int smW = tHeight-1; smW > (tHeight - stitchSmoothingDistance); smW--){
						if (smW < tHeight-1){
							stheightPoints[smW, smH] = ((stheightPoints[smW, smH] * (tHeight - smW)) + lastRowHeights[smH]) / ((tHeight - smW) + 1);
						}
						lastRowHeights[smH] = stheightPoints[smW,smH];
					}
				}
				for(int smS = 0; smS < tHeight-1; smS++){
					for (int smW = 0; smW < stitchSmoothingDistance; smW++){
						if (smW > 0){
							heightPoints[smW, smS] = ((heightPoints[smW, smS] * smW) + lastRowHeights[smS]) / (smW + 1);
						}
						lastRowHeights[smS] = heightPoints[smW,smS];
					}
				}
				tData.SetHeights(0,0,heightPoints);
				stData.SetHeights(0,0,stheightPoints);
			}
		}
		stitchSmoothingDistance /= 33;
		Debug.Log("Stitching completed successfully!");
	}
}


