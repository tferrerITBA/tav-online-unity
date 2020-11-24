using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    public TMP_InputField serverIPInput;
    public TMP_InputField clientPortInput;
    public GameObject errorGO;
    public TMP_Text errorText;

    public AudioMixer audioMixer;

    private void Start()
    {
        var error = PlayerPrefs.GetString("connectionError");
        if (!string.IsNullOrWhiteSpace(error))
        {
            errorText.text = error;
            errorGO.SetActive(true);
            PlayerPrefs.SetString("connectionError", "");
            PlayerPrefs.Save();
        }
    }

    public void StartClient()
    {
        PlayerPrefs.SetInt("isServer", 0);
        PlayerPrefs.SetString("serverIP", serverIPInput.text);
        PlayerPrefs.SetInt("clientPort", Int32.Parse(clientPortInput.text));
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void StartServer()
    {
        PlayerPrefs.SetInt("isServer", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void HideError()
    {
        errorGO.SetActive(false);
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }

    public void ToggleAudioMute()
    {
        audioMixer.GetFloat("Volume", out var volume);
        audioMixer.SetFloat("Volume", volume == 0 ? -80 : 0);
    }
}
