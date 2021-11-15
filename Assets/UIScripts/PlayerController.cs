using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    

    public GameObject playerCamera;

    public float jumpSpeed = 3.5f;
    public float checkRadius = 0.5f;
    public float gravity = 9.8f;
    public float maxPlacingDistance = 5.0f;
    

   
    void Start() {

    }

    void Update() {
        Move();
    }

    void Move() {

    }
}
