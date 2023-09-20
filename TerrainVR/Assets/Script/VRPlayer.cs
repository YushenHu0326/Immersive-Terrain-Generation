using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VRPlayer : MonoBehaviour
{
    public enum TerrainType
    {
        Mountain,
        Canyon,
        Glacier
    }

    public GameObject leftController;
    public GameObject rightController;

    private GameObject stroke;
    private int strokeIndex;
    private Vector3 lastStrokePosition;
    private bool strokeButtonPressed;

    private float xMin, xMax, yMin, yMax, radius;

    Texture2D userInput;

    private bool leftPrimaryButtonPressed_f,
                 rightPrimaryButtonPressed_f,
                 leftSecondaryButtonPressed_f,
                 rightSecondaryButtonPressed_f,
                 leftTriggerButtonPressed_f,
                 rightTriggerButtonPressed_f;

    private InputDevice leftDevice;
    private InputDevice rightDevice;

    private TerrainTool terrainTool;

    private TerrainType terrainType;

    // Start is called before the first frame update
    void Start()
    {
        List<InputDevice> leftDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftDevices);

        if (leftDevices.Count > 0) leftDevice = leftDevices[0];

        List<InputDevice> rightDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightDevices);

        if (rightDevices.Count > 0) rightDevice = rightDevices[0];

        terrainTool = GetComponent<TerrainTool>();

        terrainType = TerrainType.Mountain;

        userInput = new Texture2D(500, 500, TextureFormat.ARGB32, false);

        xMin = 10000f;
        xMax = -10000f;
        yMin = 10000f;
        yMax = -10000f;
        radius = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        leftDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPrimaryButtonPressed);
        rightDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightPrimaryButtonPressed);

        leftDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftSecondaryButtonPressed);
        rightDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightSecondaryButtonPressed);

        leftDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftTriggerButtonPressed);
        rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightTriggerButtonPressed);

        if (leftTriggerButtonPressed)
        {
            if (!leftTriggerButtonPressed_f)
                leftTriggerButtonPressed_f = true;
        }
        else
        {
            if (leftTriggerButtonPressed_f)
            {
                leftTriggerButtonPressed_f = false;
            }
        }

        if (rightTriggerButtonPressed)
        {
            if (!rightTriggerButtonPressed_f)
            {
                rightTriggerButtonPressed_f = true;

                if (terrainType == TerrainType.Canyon)
                    terrainTool.OnCanyon();
            }

            if (Physics.Raycast(new Vector3(rightController.transform.position.x, 500f, rightController.transform.position.z), Vector3.down, out var hit)
                && (leftController != null && rightController != null))
            {
                float offset = rightController.transform.position.y - hit.point.y;

                if (Mathf.Abs(offset) * 2f > radius) radius = Mathf.Abs(offset) * 2f;

                if (terrainType != TerrainType.Canyon)
                {
                    float brushSize = Mathf.Abs(offset) * 3f;
                    terrainTool.RaiseTerrain(hit.point, Mathf.Abs(offset), (int)brushSize, (int)brushSize);
                }
                else
                {
                    float brushSize = Mathf.Abs(offset) * 5f;
                    terrainTool.LowerTerrain(hit.point, Mathf.Abs(offset), (int)brushSize, (int)brushSize);
                }
            }

            if (rightController.transform.position.x < xMin) xMin = rightController.transform.position.x;
            if (rightController.transform.position.x > xMax) xMax = rightController.transform.position.x;
            if (rightController.transform.position.z < yMin) yMin = rightController.transform.position.z;
            if (rightController.transform.position.z > yMax) yMax = rightController.transform.position.z;

            if (!strokeButtonPressed)
            {
                strokeButtonPressed = true;
                lastStrokePosition = rightController.transform.position;
                stroke = new GameObject("Stroke");
                stroke.AddComponent<LineRenderer>();
                stroke.GetComponent<LineRenderer>().material = new Material(Shader.Find("Sprites/Default"));

                if (terrainType == TerrainType.Mountain)
                {
                    stroke.GetComponent<LineRenderer>().startColor = Color.red;
                    stroke.GetComponent<LineRenderer>().endColor = Color.red;
                }
                else if (terrainType == TerrainType.Canyon)
                {
                    stroke.GetComponent<LineRenderer>().startColor = Color.blue;
                    stroke.GetComponent<LineRenderer>().endColor = Color.blue;
                }
                else
                {
                    stroke.GetComponent<LineRenderer>().startColor = Color.white;
                    stroke.GetComponent<LineRenderer>().endColor = Color.white;
                }
            }
            /*
            if ((rightController.transform.position.x > -250f && rightController.transform.position.x < 250f) &&
                (rightController.transform.position.z > -250f && rightController.transform.position.z < 250f))
            {
                if (redStroke)
                    userInput.SetPixel((int)rightController.transform.position.x - 250, (int)rightController.transform.position.z - 250, 
                        new Color(1f, 0f, 0f));
                else
                    userInput.SetPixel((int)rightController.transform.position.x - 250, (int)rightController.transform.position.z - 250,
                        new Color(0f, 0f, 1f));
            }
            */
            if (Vector3.Distance(lastStrokePosition, rightController.transform.position) > 1f)
            {
                stroke.GetComponent<LineRenderer>().positionCount = strokeIndex + 1;
                stroke.GetComponent<LineRenderer>().SetPosition(strokeIndex, rightController.transform.position);
                strokeIndex++;
                lastStrokePosition = rightController.transform.position;
            }
        }
        else
        {
            if (rightTriggerButtonPressed_f)
            {
                rightTriggerButtonPressed_f = false;

                int terrainTypeNum = 0;
                if (terrainType == TerrainType.Canyon) terrainTypeNum = 1;
                else if (terrainType == TerrainType.Glacier) terrainTypeNum = 2;

                terrainTool.ApplyTerrain(terrainTypeNum, xMin, xMax, yMin, yMax, radius);

                Destroy(stroke);

                xMin = 10000f;
                xMax = -10000f;
                yMin = 10000f;
                yMax = -10000f;
                radius = 0f;
            }

            strokeButtonPressed = false;
            strokeIndex = 0;
        }

        if (leftPrimaryButtonPressed)
        {
            if (!leftPrimaryButtonPressed_f)
            {
                leftPrimaryButtonPressed_f = true;
            }
        }
        else
        {
            if (leftPrimaryButtonPressed_f)
                leftPrimaryButtonPressed_f = false;
        }

        if (leftSecondaryButtonPressed)
        {
            if (!leftSecondaryButtonPressed_f)
            {
                leftSecondaryButtonPressed_f = true;
            }
        }
        else
        {
            if (leftSecondaryButtonPressed_f)
                leftSecondaryButtonPressed_f = false;
        }

        if (rightPrimaryButtonPressed)
        {
            if (!rightPrimaryButtonPressed_f)
            {
                rightPrimaryButtonPressed_f = true;
                terrainTool.ClearTerrain();
            }
        }
        else
        {
            if (rightPrimaryButtonPressed_f)
                rightPrimaryButtonPressed_f = false;
        }

        if (rightSecondaryButtonPressed)
        {
            if (!rightSecondaryButtonPressed_f)
            {
                rightSecondaryButtonPressed_f = true;
                if (terrainType == TerrainType.Mountain) terrainType = TerrainType.Canyon;
                else if (terrainType == TerrainType.Canyon) terrainType = TerrainType.Glacier;
                else terrainType = TerrainType.Mountain;
            }
            /*
            byte[] bytes = userInput.EncodeToPNG();

            System.IO.File.WriteAllBytes(Application.dataPath + "/i.png", bytes);
            */
        }
        else
        {
            if (rightSecondaryButtonPressed_f)
                rightSecondaryButtonPressed_f = false;
        }
    }
}
