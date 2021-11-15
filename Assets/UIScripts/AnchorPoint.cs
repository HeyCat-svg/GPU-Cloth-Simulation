using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnchorPoint : MonoBehaviour
{
    public Text name;
    public InputField Nx, Ny, Px, Py, Pz;
    public Vector2Int clothPoint;
    public Vector4 position;

    
    // Start is called before the first frame update
    void Start()
    {
        Nx.text = clothPoint.x.ToString();
        Ny.text = clothPoint.y.ToString();
        Px.text = position.x.ToString();
        Py.text = position.y.ToString();
        Pz.text = position.z.ToString();
        position.w = 1;

        Nx.onEndEdit.AddListener((value) => {
            if (value.Equals(""))
            {
                value = "0";
                Nx.text = value;
            }
            clothPoint.x = Convert.ToInt32(value);
        });
        Ny.onEndEdit.AddListener((value) => {
            if (value.Equals(""))
            {
                value = "0";
                Ny.text = value;
            }
            clothPoint.y = Convert.ToInt32(value);
        });

        Px.onEndEdit.AddListener((value) => {
            if (value.Equals(""))
            {
                value = "0";
                Px.text = value;
            }
            position.x = Convert.ToSingle(value);
        });
        Py.onEndEdit.AddListener((value) => {
            if (value.Equals(""))
            {
                value = "0";
                Py.text = value;
            }
            position.y = Convert.ToSingle(value);
        });
        Pz.onEndEdit.AddListener((value) => {
            if (value.Equals(""))
            {
                value = "0";
                Pz.text = value;
            }
            position.z = Convert.ToSingle(value);
        });
    }

    // Update is called once per frame
    void Update()
    {
    }
}
