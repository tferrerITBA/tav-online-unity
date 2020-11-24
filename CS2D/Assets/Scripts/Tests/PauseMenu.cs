using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject healthHUD;
    public MouseLook cam;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            healthHUD.SetActive(pauseMenu.activeSelf);
            pauseMenu.SetActive(!pauseMenu.activeSelf);
            cam.ToggleMouseLock();
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
