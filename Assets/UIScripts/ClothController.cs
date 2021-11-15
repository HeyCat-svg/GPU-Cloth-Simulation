using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClothController : MonoBehaviour
{

    public Toggle activeToggle;

    public InputController gridSize;
    public InputController iterCount;
    public SliderController bendingAngle;
    public SliderController lineBreakThres;
    public SliderController damping;
    public SliderController looper;
    public PointController gravity;

    public PointController wind_V;
    public InputController dragCoeff;
    public InputController liftCoeff;
    public InputController windNoiseScale;
    public InputController windNoiseGridSize;
    public InputController windNoiseStrength;
    public InputController windVibrationStrength;

    public InputController pointNumInput;

    public GameObject controlObject;

    public GameObject anchorObject;


    private ArrayList anchorPoints;
    private bool ifInit = false;
    // Start is called before the first frame update
    void Start()
    {
        controlObject = Instantiate(controlObject);
        anchorPoints = new ArrayList();

        activeToggle.onValueChanged.AddListener((value) => {
            controlObject.SetActive(value);
        });

    }
    private void OnEnable()
    {

    }

    // Update is called once per frame
    void LateUpdate()
    {
        ClothSimulation clothSimulation = controlObject.GetComponent<ClothSimulation>();
        if (ifInit)
        {
            int newValue = (int)pointNumInput.value;
            if (newValue >= 0)
            {
                if (newValue < anchorPoints.Count)
                {
                    for (int i = newValue; i < anchorPoints.Count; i++)
                    {
                        Destroy(((AnchorPoint)anchorPoints[i]).gameObject);
                        /*                        anchorPoints.RemoveAt(i);*/
                    }
                    anchorPoints.RemoveRange(newValue, anchorPoints.Count - newValue);
                }
                else if (newValue > anchorPoints.Count)
                {
                    for (int i = anchorPoints.Count; i < newValue; i++)
                    {
                        GameObject anchorPoint = Instantiate(anchorObject);
                        anchorPoint.transform.parent = this.transform;
                        anchorPoint.GetComponent<AnchorPoint>().name.text = "Point" + (i + 1);
                        anchorPoint.name = "LateUpdate" + (i + 1);
                        anchorPoints.Add(anchorPoint.GetComponent<AnchorPoint>());
                    }
                }
            }
        }
        else
        {
            ifInit = true;
            int length = clothSimulation.anchorRowAndCol.Length;
            anchorPoints = new ArrayList();
            pointNumInput.value = length;
            for (int i = 0; i < length; i++)
            {
                GameObject anchorPoint = Instantiate(anchorObject);
                anchorPoint.transform.parent = this.transform;
                AnchorPoint anchor = anchorPoint.GetComponent<AnchorPoint>();
                anchor.name.text = "Point" + (i + 1);
                anchor.clothPoint = clothSimulation.anchorRowAndCol[i];
                anchor.position = clothSimulation.anchorRelPos[i];
                anchorPoints.Add(anchor);
            }
        }


        //cloth-simulation
        clothSimulation.gridSize = gridSize.value;
        clothSimulation.iterCount = (int)iterCount.value;
        clothSimulation.bendingAngle = bendingAngle.value;
        clothSimulation.lineBreakThres = lineBreakThres.value;
        clothSimulation.damping = damping.value;
        clothSimulation.g = gravity.value;

        clothSimulation.wind_V = wind_V.value;
        clothSimulation.dragCoeff = dragCoeff.value;
        clothSimulation.liftCoeff = liftCoeff.value;
        clothSimulation.windNoiseScale = windNoiseScale.value;
        clothSimulation.windNoiceGridSize = windNoiseGridSize.value;
        clothSimulation.windNoiceStrength = windNoiseStrength.value;
        clothSimulation.windVibrationStrength = windVibrationStrength.value;
    }
    public void clickUI()
    {
        CameraLook.instance.clickUI(controlObject);
    }

    public void updateAnchor() {
        ClothSimulation clothSimulation = controlObject.GetComponent<ClothSimulation>();
        Vector2Int[] anchorRowAndCol = new Vector2Int[anchorPoints.Count];
        Vector4[] anchorRelPos = new Vector4[anchorPoints.Count];
        for (int i = 0; i < anchorPoints.Count; i++)
        {
            anchorRowAndCol[i] = ((AnchorPoint)anchorPoints[i]).clothPoint;
            anchorRelPos[i] = ((AnchorPoint)anchorPoints[i]).position;
        }
        clothSimulation.anchorRowAndCol = anchorRowAndCol;
        clothSimulation.anchorRelPos = anchorRelPos;
    }

    public void resetAnchor()
    {
        ClothSimulation clothSimulation = controlObject.GetComponent<ClothSimulation>();
        int length = clothSimulation.anchorRowAndCol.Length;
        for(int i = 0; i < anchorPoints.Count; i++)
        {
            Destroy(((AnchorPoint)anchorPoints[i]).gameObject);
        }
        anchorPoints.Clear();
        pointNumInput.changeValue(length);
        for (int i = 0; i < length; i++)
        {
            GameObject anchorPoint = Instantiate(anchorObject);
            anchorPoint.transform.parent = this.transform;
            AnchorPoint anchor = anchorPoint.GetComponent<AnchorPoint>();
            anchor.name.text = "Point" + (i + 1);
            anchor.clothPoint = clothSimulation.anchorRowAndCol[i];
            anchor.position = clothSimulation.anchorRelPos[i];
            anchorPoints.Add(anchor);
        }
    }

    public void delete()
    {
        Destroy(this.controlObject);
        Destroy(this.gameObject);
    }
}
