﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public float xRotation = 0;

    public Transform player;

    void Start()
    {
        if (PlayerPrefs.GetInt("isServer") > 0)
        {
            enabled = false;
            return;
        }
        Cursor.lockState = CursorLockMode.Locked;
    }


    void Update()
    {
        if (!player)
            return;
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        player.Rotate(Vector3.up * mouseX);
    }
    
    void OnGUI(){
        // Draw Crosshair
        GUI.Box(new Rect(
            Screen.width/2.0f,Screen.height/2.0f, 10, 10
            ), "");
    }
}
