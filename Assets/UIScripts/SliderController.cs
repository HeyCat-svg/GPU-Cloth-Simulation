using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
    public float min, max;
    public float init;
    public float value;
    public Slider slider;
    public InputField input;

    // Start is called before the first frame update
    void Start()
    {
        value = init;
        slider.value = (init - min) / (max - min);
        input.text = init.ToString();

        slider.onValueChanged.AddListener((single) => {
            single = (max - min) * single + min;
            input.text = single.ToString();
        });
        input.onEndEdit.AddListener((str) => {
            float newValue = Convert.ToSingle(str);
            if (newValue > max)
            {
                newValue = max;
                input.text = newValue.ToString();
            }
            else if (newValue < min)
            {
                newValue = min;
                input.text = newValue.ToString();
            }
            else
            {
                slider.value = (newValue - min) / (max - min);
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        value = (max - min) * slider.value + min;
    }
}
