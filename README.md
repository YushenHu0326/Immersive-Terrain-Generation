# Immersive-Terrain-Generation

## Warning: Before you play the demo
Github doesn't allow any file larger than 100Mb to be uploaded, so all the ONNX models made for this project is separated in Google drive files and requires manual configuration.<br>
<br>To download the files, please use the following links:<br>
Mountain-like Terrain: https://drive.google.com/file/d/1LMs6symQz8ItPHMxHzUX0C9Q6qzXpL9V/view?usp=sharing<br>
Canyon-like Terrain: https://drive.google.com/file/d/1TkHkMnM3B_-xrIrwldBfbqqNQ42HITCT/view?usp=sharing<br>
Glacier-like Terrain: https://drive.google.com/file/d/19zsDq-aCc0-PRuO5dvEI_GmQxC54YjiC/view?usp=sharing<br>

When opening the Unity project, please make sure that all ONNX models are in the right place: 
/Assets/ProceduralTerrainGenerator/Model/. Also, please make sure that the TerrainModifier prefab has the correct references:<br>
<br>![1](https://github.com/YushenHu0326/Immersive-Terrain-Generation/assets/62900433/8882cd04-412f-44bc-812c-dd8c29fa81e4)<br>
<br>The prefab can be found in: /Assets/ProceduralTerrainGenerator/Prefab/TerrainModifier
## How to play
This system provides artists an immersive way of authoring terrains. To play the demo, connect your VR headset, open the Terrain scene located in /Scenes/Terrain.unity.<br>
Press the right controller trigger to draw the stroke. Press the primary button (A) of the right controller to clear the terrain, press the secondary button (B) of the 
right controller to switch between different types of terrain. Each of these types of terrain is represented in different colors, mountain-red/canyon-blue/glacier/white.<br>
## Issues/Future work
Please don't draw the terrain strokes near the edge of the terrain.
