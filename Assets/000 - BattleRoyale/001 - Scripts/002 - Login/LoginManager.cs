using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField username;
    [SerializeField] private TMP_InputField password;
    [SerializeField] private Toggle rememberMe;

    public void Login()
    {
        if (username.text == "")
        {
            Debug.Log("no username");
            return;
        }

        if (password.text == "")
        {
            Debug.Log("no password");
            return;
        }

        SceneManager.LoadScene("CharacterCreation");
    }
}
