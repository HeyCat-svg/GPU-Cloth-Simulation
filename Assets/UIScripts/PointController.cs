using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointController : MonoBehaviour
{
    public InputField x, y, z;
    public Vector3 value;
    public Vector3 init;

    // Start is called before the first frame update
    void Start()
    {
        value = init;
        x.text = init.x.ToString();
        y.text = init.y.ToString();
        z.text = init.z.ToString();

        x.onEndEdit.AddListener((str) =>
        {
            if (str.Equals(""))
            {
                str = "0";
                x.text = str;
            }
            value.x = Convert.ToSingle(str);
        });

        y.onEndEdit.AddListener((str) =>
        {
            if (str.Equals(""))
            {
                str = "0";
                y.text = str;
            }
            value.y = Convert.ToSingle(str);
        });

        z.onEndEdit.AddListener((str) =>
        {
            if (str.Equals(""))
            {
                str = "0";
                z.text = str;
            }
            value.z = Convert.ToSingle(str);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
