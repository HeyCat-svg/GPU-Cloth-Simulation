using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectController : MonoBehaviour
{
    public GameObject controlObject;
    public Toggle activeToggle;
    public InputField Px, Py, Pz, Rx, Ry, Rz;
    public SliderController scale;
    // Start is called before the first frame update
    void Start()
    {
        controlObject = Instantiate(controlObject);
        Px.text = controlObject.transform.position.x.ToString();
        Py.text = controlObject.transform.position.y.ToString();
        Pz.text = controlObject.transform.position.z.ToString();

        Rx.text = controlObject.transform.rotation.eulerAngles.x.ToString();
        Ry.text = controlObject.transform.rotation.eulerAngles.y.ToString();
        Rz.text = controlObject.transform.rotation.eulerAngles.z.ToString();

        activeToggle.onValueChanged.AddListener((value) => {
            controlObject.SetActive(value);
        });

        Px.onEndEdit.AddListener((value) =>
        {
            if (value.Equals(""))
            {
                value = "0";
                Px.text = value;
            }

            Vector3 vec = controlObject.transform.position;
            vec.x = Convert.ToSingle(value);
            controlObject.transform.position = vec;
        });
        Py.onEndEdit.AddListener((value) =>
        {
            if (value.Equals(""))
            {
                value = "0";
                Py.text = value;
            }
            Vector3 vec = controlObject.transform.position;
            vec.y = Convert.ToSingle(value);
            controlObject.transform.position = vec;
        });
        Pz.onEndEdit.AddListener((value) =>
        {
            if (value.Equals(""))
            {
                value = "0";
                Pz.text = value;
            }
            Vector3 vec = controlObject.transform.position;
            vec.z = Convert.ToSingle(value);
            controlObject.transform.position = vec;
        });

        Rx.onEndEdit.AddListener((value) => {
            if (value.Equals(""))
            {
                value = "0";
                Rx.text = value;
            }
            Vector3 vec = controlObject.transform.rotation.eulerAngles;
            vec.x = Convert.ToSingle(value);
            controlObject.transform.rotation = Quaternion.Euler(vec);
        });
        Ry.onEndEdit.AddListener((value) => {
            if (value.Equals(""))
            {
                value = "0";
                Ry.text = value;
            }
            Vector3 vec = controlObject.transform.rotation.eulerAngles;
            vec.y = Convert.ToSingle(value);
            controlObject.transform.rotation = Quaternion.Euler(vec);
        });
        Rz.onEndEdit.AddListener((value) => {
            if (value.Equals(""))
            {
                value = "0";
                Rz.text = value;
            }
            Vector3 vec = controlObject.transform.rotation.eulerAngles;
            vec.z = Convert.ToSingle(value);
            controlObject.transform.rotation = Quaternion.Euler(vec);
        });
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Px.text = controlObject.transform.position.x.ToString();
        Py.text = controlObject.transform.position.y.ToString();
        Pz.text = controlObject.transform.position.z.ToString();

        Rx.text = controlObject.transform.rotation.x.ToString();
        Ry.text = controlObject.transform.rotation.y.ToString();
        Rz.text = controlObject.transform.rotation.z.ToString();

        controlObject.transform.localScale = new Vector3(scale.value, scale.value, scale.value);
    }

    public void clickUI()
    {
        CameraLook.instance.clickUI(controlObject);
    }

    public void delete()
    {
        Destroy(this.controlObject);
        Destroy(this.gameObject);
    }
}
