﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject healthHUD;
    public GameObject deathHUD;
    public MouseLook cam;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            healthHUD.SetActive(pauseMenu.activeSelf);
            pauseMenu.SetActive(!pauseMenu.activeSelf);
            if (!(PlayerPrefs.GetInt("isServer") > 0))
                cam.ToggleMouseLock();
            if (deathHUD.activeSelf)
                deathHUD.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        healthHUD.SetActive(true);
        pauseMenu.SetActive(false);
        cam.SetMouseLock(CursorLockMode.Locked);
    }

    public void Disconnect()
    {
        var clientManager = FindObjectOfType<ClientManager>();
        if (clientManager != null)
        {
            Destroy(clientManager);
            var client = FindObjectOfType<CubeClient>();
            if (client != null)
                Destroy(client);
        }
        else
        {
            Destroy(FindObjectOfType<ServerEntity>());
        }
        SceneManager.LoadScene(0);
    }
}
