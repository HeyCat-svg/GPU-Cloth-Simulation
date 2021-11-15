using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLook : MonoBehaviour {

    public static CameraLook instance;
    public GameObject arrowObejct;
    public float mouseSensitivity = 100.0f;
    public Transform playerBody;
    public float velocity=100.0f;

    public CharacterController controller;
    public float speed = 12.0f;

    private float verticalSpeed = 0.0f;

    float xRotation = 0.0f;
    float yRotation = 0.0f;

    private GameObject controlObject;
    private Vector3 distance;




    void Start() {
        if (instance == null)
        {
            instance = this;
        }
        Cursor.lockState = CursorLockMode.None;
    }

    // Update is called once per frame
    void Update() {
        CheckCursorUnlock();
        if (Cursor.lockState== CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;    // 左手系的原因 旋转方向和mouseY符号相反
            xRotation = Mathf.Clamp(xRotation, -90.0f, 90.0f);
            yRotation += mouseX;
            this.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0.0f);

            Vector3 move = new Vector3(0, 0, 0);
            if (Input.GetKey(KeyCode.W))
            {
                move += transform.forward * speed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S))
            {
                move += -transform.forward * speed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.A))
            {
                move += -transform.right * speed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.D))
            {
                move += transform.right * speed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                move += -transform.up * speed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                move += transform.up * speed * Time.deltaTime;
            }

            controller.Move(move);
            if (controlObject != null)
            {
                distance = transform.position - controlObject.transform.position;
                arrowObejct.GetComponent<ArrowController>().distance = Vector3.Distance(distance, new Vector3(0, 0, 0));
            }
        }
        if (controlObject != null && Input.GetKey(KeyCode.Z))
        {
            controlObject = null;
            arrowObejct.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (controlObject != null)
        {
            controlObject.transform.position = arrowObejct.transform.position;
        }
    }

    void CheckCursorUnlock()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            arrowObejct.SetActive(false);
        }

        if (Input.GetMouseButtonUp(1))
        {
            Cursor.lockState = CursorLockMode.None;
            if (controlObject != null)
                arrowObejct.SetActive(true);
        }
    }

    public void clickUI(GameObject gameObject)
    {
        controlObject = gameObject;
/*        transform.rotation = Quaternion.Euler(0, 0, 0);*/

        distance = transform.position-controlObject.transform.position;
        if(distance.magnitude>4.0f)
            distance = distance * 4.0f /distance.magnitude;
        transform.position = controlObject.transform.position + distance;
        arrowObejct.SetActive(true);
        arrowObejct.transform.position = controlObject.transform.position;
/*        arrowObejct.GetComponent<ArrowController>().originDistance = Vector3.Distance(distance, new Vector3(0, 0, 0));*/
        arrowObejct.GetComponent<ArrowController>().distance = Vector3.Distance(distance, new Vector3(0, 0, 0));

    }
}
