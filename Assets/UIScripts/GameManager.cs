using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject clothUI, sphereUI,bunnyUI,cubeUI;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void createCube()
    {
        GameObject gameObject = Instantiate(cubeUI);
        gameObject.transform.SetParent(transform);
    }

    public void createSphere()
    {
        GameObject gameObject = Instantiate(sphereUI);
        gameObject.transform.SetParent(transform);
    }

    public void createBunny()
    {
        GameObject gameObject = Instantiate(bunnyUI);
        gameObject.transform.SetParent(transform);
    }

    public void createCloth()
    {
        GameObject gameObject = Instantiate(clothUI);
        gameObject.transform.SetParent(transform);
    }
}
