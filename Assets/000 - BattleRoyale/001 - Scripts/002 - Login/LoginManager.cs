using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System;
using CandyCoded.env;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LoginManager : MonoBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private TMP_InputField username;
    [SerializeField] private TMP_InputField password;
    [SerializeField] private Toggle rememberMe;
    [SerializeField] private AudioClip bgMusic;

    [Header("BRIGHTNESS")]
    [SerializeField] private float maxBrightness;
    [SerializeField] private Volume postProcessing;

    //  ============================

    ColorAdjustments colorAdjustments;

    //  ============================

    private void Awake()
    {
        if (postProcessing.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            colorAdjustments.postExposure.value = maxBrightness * GameManager.Instance.GraphicsManager.CurrentBrightness;
        }

        GameManager.Instance.AudioController.SetBGMusic(bgMusic);
        GameManager.Instance.SceneController.AddActionLoadinList(CheckRememberMe());
        GameManager.Instance.SceneController.AddActionLoadinList(userData.CheckControlSettingSave());
        GameManager.Instance.SceneController.ActionPass = true;
    }

    private IEnumerator CheckRememberMe()
    {
        rememberMe.isOn = userData.RememberMe;

        if (!userData.RememberMe) yield break;

        username.text = userData.Username;
        password.text = userData.Password;
    }

    IEnumerator LoginUser()
    {
        Debug.Log("start login user api");
        GameManager.Instance.NoBGLoading.SetActive(true);

        UnityWebRequest apiRquest;

        if (env.TryParseEnvironmentVariable("API_URL", out string httpRequest))
        {
            apiRquest = UnityWebRequest.Get($"{httpRequest}/auth/login?username={username.text}&password={password.text}");
        }
        else
        {
            //  ERROR PANEL HERE
            Debug.Log("Error API CALL! Error Code: ENV FAILED TO PARSE");
            GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later", null);
            yield break;
        }
        Debug.Log("done candy coded env");
        apiRquest.SetRequestHeader("Content-Type", "application/json");
        Debug.Log("starting call api");

        yield return apiRquest.SendWebRequest();

        Debug.Log("done call api");
        Debug.Log("api response" + "  " + apiRquest.downloadHandler.text);
        if (apiRquest.result == UnityWebRequest.Result.Success)
        {
            string response = apiRquest.downloadHandler.text;

            Debug.Log("api response" + "  " + response);
            if (response[0] == '{' && response[response.Length - 1] == '}')
            {
                Dictionary<string, object> apiresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);

                if (!apiresponse.ContainsKey("message"))
                {
                    //  ERROR PANEL HERE
                    Debug.Log("Error API CALL! Error Code: " + response);
                    GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later.", null);
                    GameManager.Instance.NoBGLoading.SetActive(false);
                    yield break;
                }

                if (apiresponse["message"].ToString() != "success")
                {
                    //  ERROR PANEL HERE
                    Debug.Log("Error API CALL! Error Code: " + apiresponse["data"].ToString());
                    GameManager.Instance.NotificationController.ShowError($"{apiresponse["data"]}", null);
                    GameManager.Instance.NoBGLoading.SetActive(false);
                    yield break;
                }

                if (!apiresponse.ContainsKey("data"))
                {
                    //  ERROR PANEL HERE
                    Debug.Log("Error API CALL! Error Code: " + apiresponse["message"].ToString());
                    GameManager.Instance.NotificationController.ShowError($"{apiresponse["data"]}", null);
                    GameManager.Instance.NoBGLoading.SetActive(false);
                    yield break;
                }

                Dictionary<string, object> dataresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(apiresponse["data"].ToString());

                if (!dataresponse.ContainsKey("token"))
                {
                    //  ERROR PANEL HERE
                    Debug.Log("Error API CALL! Error Code: " + apiresponse["data"].ToString());
                    GameManager.Instance.NotificationController.ShowError($"{apiresponse["data"]}", null);
                    GameManager.Instance.NoBGLoading.SetActive(false);
                    yield break;
                }

                userData.UserToken = dataresponse["token"].ToString();
                userData.Username = username.text;

                if (rememberMe.isOn)
                {
                    userData.RememberMe = true;
                    userData.Password = password.text;
                    userData.RememberMeSave();
                }
                else
                {
                    userData.RememberMeDelete();
                }
                Debug.Log("starting initialize socket");
                GameManager.Instance.SocketMngr.InitializeSocket();

                while (GameManager.Instance.SocketMngr.ConnectionStatus != "Connected") yield return null;

                GameManager.Instance.NoBGLoading.SetActive(false);
                GameManager.Instance.SceneController.CurrentScene = "Lobby";
            }
            else
            {
                //  ERROR PANEL HERE
                Debug.Log("Error API CALL! Error Code: " + response);
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please contact customer support for more details.", null);
                GameManager.Instance.NoBGLoading.SetActive(false);
            }
        }
        else
        {
            try
            {
                Dictionary<string, object> apiresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(apiRquest.downloadHandler.text);

                GameManager.Instance.NotificationController.ShowError($"{apiresponse["data"]}", null);
                GameManager.Instance.NoBGLoading.SetActive(false);
            }
            catch (Exception ex)
            {
                Debug.Log("Error API CALL! Error Code: " + apiRquest.result + ", " + apiRquest.downloadHandler.text);
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please contact customer support for more details.", null);
                GameManager.Instance.NoBGLoading.SetActive(false);
            }
        }
    }

    public void Login()
    {
        if (username.text == "")
        {
            GameManager.Instance.NotificationController.ShowError("Please enter your username first and try again!", null);
            return;
        }

        if (password.text == "")
        {
            GameManager.Instance.NotificationController.ShowError("Please enter your password first and try again!", null);
            return;
        }

        StartCoroutine(LoginUser());
    }

    public void OpenURL(string url) => Application.OpenURL(url);
}
