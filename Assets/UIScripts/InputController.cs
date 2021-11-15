using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{
    public float init = 0;
    public InputField input;
    public float value;
    // Start is called before the first frame update
    void Start()
    {
        value = init;
        if (input == null)
        {
            Debug.Log("NULLLLLLL");
            return;
        }
        input.text = init.ToString();
        input.onEndEdit.AddListener((str) =>
        {
            value = Convert.ToSingle(str);
        });
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void changeValue(float newVal)
    {
        value = newVal;
        input.text = newVal.ToString();
    }
}
