using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public MouseLook cam;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pauseMenu.SetActive(!pauseMenu.activeSelf);
            cam.ToggleMouseLock();
        }
    }

    public void ResumeGame()
    {
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
    }
}
