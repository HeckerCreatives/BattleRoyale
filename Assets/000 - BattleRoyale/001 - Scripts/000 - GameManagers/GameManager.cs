using CandyCoded.env;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using Fusion;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    Queue<Action> jobs = new Queue<Action>();

    //  ============================

    [SerializeField] private UserData userData;
    [SerializeField] private AudioClip btnClip;

    [Header("DONT DESTROY ON LOAD")]
    [SerializeField] private List<GameObject> dontDestroyOnLoadList;

    [Header("LOADING")]
    [SerializeField] private string startSceneName;

    [SerializeField] private NetworkRunner instanceRunner;

    [field: Header("CAMERA")]
    [field: SerializeField] public Camera UICamera { get; private set; }
    [field: SerializeField] public Camera Camera { get; private set; }
    [field: SerializeField] public Camera BackCamera { get; private set; }

    [field: Header("SCRIPTS")]
    [field: SerializeField] public SceneController SceneController { get; private set; }
    [field: SerializeField] public SocketManager SocketMngr { get; private set; }
    [field: SerializeField] public AudioManager AudioController { get; private set; }
    [field: SerializeField] public GraphicsController GraphicsManager { get; private set; }
    [field: SerializeField] public GameplaySettingsController GameSettingManager { get; private set; }
    [field: SerializeField] public NotificationController NotificationController { get; private set; }
    [field: SerializeField] public GameObject NoBGLoading { get; private set; }


    private void Awake()
    {
        Application.targetFrameRate = 60;

        //if (env.TryParseEnvironmentVariable("APP_VERSION", out string version))
        //    currentVersion.text = $"v{version}";

        for (int a = 0; a < dontDestroyOnLoadList.Count; a++)
            DontDestroyOnLoad(dontDestroyOnLoadList[a]);

        Instance = this;

        SceneController.CurrentScene = startSceneName;
    }

    private void Start()
    {
        userData.ResetLogin();
    }

    private void Update()
    {
        if (jobs.Count > 0)
            jobs.Dequeue().Invoke();
    }

    public IEnumerator GetRequest(string route, string query, bool loaderEndState, System.Action<System.Object> callback, System.Action errorAction, bool requiredtoken = true)
    {
        if (requiredtoken)
            while (userData.UserToken == "") yield return null;

        UnityWebRequest apiRquest;
        if (env.TryParseEnvironmentVariable("API_URL", out string httpRequest))
        {
            apiRquest = UnityWebRequest.Get(httpRequest + route + query);
        }
        else
        {
            //  ERROR PANEL HERE
            Debug.Log("Error API CALL! Error Code: ENV FAILED TO PARSE");
            NotificationController.ShowError("Error API CALL! Error Code: ENV FAILED TO PARSE", null);

            errorAction?.Invoke();

            yield break;
        }
        apiRquest.SetRequestHeader("Content-Type", "application/json");
        apiRquest.SetRequestHeader("Authorization", "Bearer " + userData.UserToken);

        yield return apiRquest.SendWebRequest();

        if (apiRquest.result == UnityWebRequest.Result.Success)
        {
            string response = apiRquest.downloadHandler.text;

            if (response[0] == '{' && response[response.Length - 1] == '}')
            {
                try
                {
                    Dictionary<string, object> apiresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);

                    if (!apiresponse.ContainsKey("message"))
                    {
                        //  ERROR PANEL HERE
                        Debug.Log("Error API CALL! Error Code: " + response);
                        NotificationController.ShowError("There's a problem with the server! Please try again later.", null);
                        errorAction?.Invoke();
                        yield break;
                    }

                    if (apiresponse["message"].ToString() != "success")
                    {
                        //  ERROR PANEL HERE
                        Debug.Log(apiresponse["data"].ToString());
                        NotificationController.ShowError(apiresponse["data"].ToString(), null);
                        errorAction?.Invoke();
                        yield break;
                    }

                    if (apiresponse.ContainsKey("data"))
                        callback?.Invoke(apiresponse["data"].ToString());
                    else
                        callback?.Invoke("");

                    NoBGLoading.SetActive(loaderEndState);
                    Debug.Log("SUCCESS API CALL");
                }
                catch (Exception ex)
                {
                    //  ERROR PANEL HERE
                    Debug.Log("Error API CALL! Error Code: " + ex.Message);
                    Debug.Log("API response: " + response);
                    NotificationController.ShowError("There's a problem with the server! Please try again later.", null);
                    errorAction?.Invoke();
                }
            }
            else
            {
                //  ERROR PANEL HERE
                Debug.Log("Error API CALL! Error Code: " + response);
                NotificationController.ShowError("There's a problem with the server! Please try again later.", null);
                errorAction?.Invoke();
            }
        }
        else
        {
            try
            {
                NoBGLoading.SetActive(loaderEndState);

                errorAction?.Invoke();

                Dictionary<string, object> apiresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(apiRquest.downloadHandler.text);


                switch (apiRquest.responseCode)
                {
                    case 400:
                        NotificationController.ShowError($"{apiresponse["data"]}", null);
                        break;
                    case 300:
                        NotificationController.ShowError($"{apiresponse["data"]}", null);
                        SceneController.StopLoading();
                        SceneController.CurrentScene = "Login";
                        break;
                    case 301:
                        NotificationController.ShowError($"{apiresponse["data"]}", null);
                        SceneController.StopLoading();
                        SceneController.CurrentScene = "Lobby";
                        break;
                    case 405:
                        NotificationController.ShowError($"{apiresponse["data"]}", null);
                        SceneController.StopLoading();
                        SocketMngr.Socket.Disconnect();
                        break;
                }

            }
            catch (Exception ex)
            {
                NoBGLoading.SetActive(loaderEndState);
                Debug.Log("Error API CALL! Error Code: " + apiRquest.result + ", " + apiRquest.downloadHandler.text);
                NotificationController.ShowError("There's a problem with your internet connection! Please check your connection and try again.", null);
                errorAction?.Invoke();
            }
        }
    }

    public IEnumerator PostRequest(string route, string query, Dictionary<string, object> paramsBody, bool loaderEndState, System.Action<System.Object> callback, System.Action errorAction, bool useUserToken = true)
    {
        if (useUserToken)
            while (userData.UserToken == "") yield return null;

        UnityWebRequest apiRquest;

        if (env.TryParseEnvironmentVariable("API_URL", out string httpRequest))
        {
            apiRquest = UnityWebRequest.PostWwwForm(httpRequest + route + query, UnityWebRequest.kHttpVerbPOST);
        }
        else
        {
            //  ERROR PANEL HERE
            Debug.Log("Error API CALL! Error Code: ENV FAILED TO PARSE");
            NotificationController.ShowError("Error API CALL! Error Code: ENV FAILED TO PARSE", null);
            errorAction?.Invoke();
            yield break;
        }

        byte[] credBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(paramsBody));
        UploadHandler uploadHandler = new UploadHandlerRaw(credBytes);

        apiRquest.uploadHandler = uploadHandler;

        apiRquest.SetRequestHeader("Content-Type", "application/json");
        apiRquest.SetRequestHeader("Authorization", "Bearer " + userData.UserToken);

        yield return apiRquest.SendWebRequest();

        if (apiRquest.result == UnityWebRequest.Result.Success)
        {
            string response = apiRquest.downloadHandler.text;

            if (response[0] == '{' && response[response.Length - 1] == '}')
            {
                try
                {
                    Dictionary<string, object> apiresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);

                    if (!apiresponse.ContainsKey("message"))
                    {
                        //  ERROR PANEL HERE
                        Debug.Log("Error API CALL! Error Code: " + response);
                        NotificationController.ShowError("There's a problem with the server! Please try again later.", null);

                        NoBGLoading.SetActive(false);

                        errorAction?.Invoke();
                        yield break;
                    }

                    if (apiresponse["message"].ToString() != "success")
                    {
                        //  ERROR PANEL HERE
                        if (!apiresponse.ContainsKey("data"))
                        {
                            Debug.Log("Error API CALL! Error Code: " + response);
                            NotificationController.ShowError("Error Process! Error Code: " + apiresponse["message"].ToString(), () => errorAction?.Invoke());

                            yield break;
                        }
                        Debug.Log($"Error API CALL! Error Code: {apiresponse["data"]}");
                        NotificationController.ShowError($"{apiresponse["data"]}", () => errorAction?.Invoke());
                        NoBGLoading.SetActive(false);
                        yield break;
                    }

                    NoBGLoading.SetActive(loaderEndState);

                    if (apiresponse.ContainsKey("data"))
                        callback?.Invoke(apiresponse["data"].ToString());
                    else
                        callback?.Invoke("");

                    Debug.Log("SUCCESS API CALL");
                }
                catch (Exception ex)
                {
                    //  ERROR PANEL HERE
                    Debug.Log("Error API CALL! Error Code: " + ex.Message);
                    NotificationController.ShowError("There's a problem with the server! Please try again later.", null);
                    NoBGLoading.SetActive(false);
                    errorAction?.Invoke();
                }
            }
            else
            {
                //  ERROR PANEL HERE
                Debug.Log("Error API CALL! Error Code: " + response);
                NotificationController.ShowError("There's a problem with the server! Please try again later.", null);
                NoBGLoading.SetActive(false);
                errorAction?.Invoke();
            }
        }

        else
        {
            try
            {
                NoBGLoading.SetActive(false);
                errorAction?.Invoke();

                Dictionary<string, object> apiresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(apiRquest.downloadHandler.text);


                switch (apiRquest.responseCode)
                {
                    case 400:
                        NotificationController.ShowError($"{apiresponse["data"]}", null);
                        NoBGLoading.SetActive(false);
                        break;
                    case 300:
                        NotificationController.ShowError($"{apiresponse["data"]}", null);
                        SceneController.StopLoading();
                        SceneController.CurrentScene = "Login";
                        NoBGLoading.SetActive(false);
                        break;
                    case 301:
                        NotificationController.ShowError($"{apiresponse["data"]}", null);
                        SceneController.StopLoading();
                        SceneController.CurrentScene = "Lobby";
                        NoBGLoading.SetActive(false);
                        break;
                    case 405:
                        NotificationController.ShowError($"{apiresponse["data"]}", null);
                        SceneController.StopLoading();
                        SocketMngr.Socket.Disconnect();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Error API CALL! Error Code: " + apiRquest.result + ", " + apiRquest.downloadHandler.text);
                NotificationController.ShowError("There's a problem with your internet connection! Please check your connection and try again.", null);
                NoBGLoading.SetActive(false);
                errorAction?.Invoke();
            }
        }
    }

    //public IEnumerator GetRequestPicture(string route, string query, bool loaderEndState, System.Action<System.Object> callback)
    //{
        //    while (userData.Usertoken == "") yield return null;

        //    if (route == "") yield break;

        //    if (env.TryParseEnvironmentVariable("MONMONLAND_API", out string httpRequest))
        //    {
        //        using UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(httpRequest + "/" + route + query);

        //        yield return uwr.SendWebRequest();

        //        if (uwr.result != UnityWebRequest.Result.Success)
        //        {
        //            Debug.Log(httpRequest + "/" + route + query);
        //            Debug.Log("Error API CALL! Error Code: " + uwr.error);
        //            NotificationController.ShowError("Your profile picture is corrupted! Please upload a new one", null);
        //            NoBGLoading.SetActive(loaderEndState);

        //            yield break;
        //        }

        //        var texture = DownloadHandlerTexture.GetContent(uwr);

        //        callback(texture);
        //    }
        //    else
        //    {
        //        //  ERROR PANEL HERE
        //        Debug.Log("Error API CALL! Error Code: ENV FAILED TO PARSE");
        //        NotificationController.ShowError("Error API CALL! Error Code: ENV FAILED TO PARSE", null);
        //        NoBGLoading.SetActive(false);

        //        yield break;
        //    }
        //}

    public string LimitText(string originalText, int maxLength)
    {
        if (originalText.Length <= maxLength)
        {
            return originalText;
        }
        else
        {
            // Truncate the text and add ellipsis
            return originalText.Substring(0, maxLength - 3) + "...";
        }
    }

    public DateTime GetTime()
    {
        DateTime utcNow = DateTime.UtcNow;

        // Specify the UTC offset for GMT+8 (8 hours ahead of UTC)
        TimeSpan gmtPlus8Offset = TimeSpan.FromHours(8);

        // Apply the offset to get GMT+8 time
        DateTime gmtPlus8Time = utcNow + gmtPlus8Offset;

        return gmtPlus8Time;
    }

    public long GetTimestamp()
    {
        DateTime utcNow = DateTime.UtcNow;

        // Specify the UTC offset for GMT+8 (8 hours ahead of UTC)
        TimeSpan gmtPlus8Offset = TimeSpan.FromHours(8);

        // Apply the offset to get GMT+8 time
        DateTime gmtPlus8Time = utcNow + gmtPlus8Offset;

        // Convert GMT+8 time to Unix timestamp
        long unixTimestamp = new DateTimeOffset(gmtPlus8Time).ToUnixTimeSeconds();

        return unixTimestamp;
    }

    public long GetUTCTimestamp()
    {
        DateTime utcNow = DateTime.UtcNow;

        // Convert GMT+8 time to Unix timestamp
        long unixTimestamp = new DateTimeOffset(utcNow).ToUnixTimeSeconds();

        return unixTimestamp;
    }

    public long GetTimestampWithExpiration(int expiration)
    {
        DateTime utcNow = DateTime.UtcNow;

        // Specify the UTC offset for GMT+8 (8 hours ahead of UTC)
        TimeSpan gmtPlus8Offset = TimeSpan.FromHours(8);

        // Apply the offset to get GMT+8 time
        DateTime gmtPlus8Time = utcNow + gmtPlus8Offset;

        // Convert GMT+8 time to Unix timestamp
        long unixTimestamp = new DateTimeOffset(gmtPlus8Time).ToUnixTimeSeconds();

        long expirationUnixtime = unixTimestamp + (expiration * 24 * 60 * 60);

        return expirationUnixtime;
    }

    // Convert DateTime to Unix timestamp (GMT+8)
    long DateTimeToUnixTimestamp(DateTime dateTime)
    {
        DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime);
        return dateTimeOffset.ToUnixTimeSeconds();
    }

    public string GetExpirationTime(long expirationTimestamp)
    {
        DateTime utcNow = DateTime.UtcNow;

        // Specify the UTC offset for GMT+8 (8 hours ahead of UTC)
        TimeSpan gmtPlus8Offset = TimeSpan.FromHours(8);

        // Apply the offset to get GMT+8 time
        DateTime gmtPlus8Time = utcNow + gmtPlus8Offset;

        // Convert GMT+8 time to Unix timestamp
        long currentTimestamp = new DateTimeOffset(gmtPlus8Time).ToUnixTimeSeconds();

        // Calculate the remaining time
        long timeDifference = expirationTimestamp - currentTimestamp;

        // Calculate hours, minutes, and seconds
        int days = (int)(timeDifference / 86400); // 86400 seconds in a day
        int hours = (int)((timeDifference % 86400) / 3600) % 24; // Ensure hours are within 0 to 23
        int minutes = (int)((timeDifference % 3600) / 60);
        int seconds = (int)(timeDifference % 60);

        return $"{days:D2} : {hours:D2} : {minutes:D2} : {seconds:D2}";
    }

    public string GetMinuteSecondsTime(float time)
    {
        int minutes = (int)((time % 3600) / 60);
        int seconds = (int)(time % 60);

        return $"{minutes:D2} : {seconds:D2}";
    }

    public string GetUTCExpirationTimestamp(long expirationTimestamp)
    {
        DateTime utcNow = DateTime.UtcNow;

        long currentTimestamp = new DateTimeOffset(utcNow).ToUnixTimeSeconds();

        // Calculate the remaining time
        long timeDifference = expirationTimestamp - currentTimestamp;

        // Calculate hours, minutes, and seconds
        int days = (int)(timeDifference / 86400); // 86400 seconds in a day
        int hours = (int)((timeDifference % 86400) / 3600) % 24; // Ensure hours are within 0 to 23
        int minutes = (int)((timeDifference % 3600) / 60);
        int seconds = (int)(timeDifference % 60);

        return $"{days:D2} : {hours:D2} : {minutes:D2} : {seconds:D2}";
    }

    public string GetTotalTimeBasedOnSeconds(int totalSeconds)
    {
        // Calculate days, hours, minutes, and seconds
        int days = totalSeconds / 86400; // 86400 seconds in a day
        int hours = (totalSeconds % 86400) / 3600;
        int minutes = ((totalSeconds % 86400) % 3600) / 60;
        int seconds = ((totalSeconds % 86400) % 3600) % 60;

        return $"{days:D2} : {hours:D2} : {minutes:D2} : {seconds:D2}";
    }

    public string GetTotalMinutesBasedOnSeconds(int totalSeconds)
    {
        int minutes = ((totalSeconds % 86400) % 3600) / 60;
        int seconds = ((totalSeconds % 86400) % 3600) % 60;

        return $"{minutes:D2} : {seconds:D2}";
    }

    internal void AddJob(Action newJob)
    {
        jobs.Enqueue(newJob);
    }

    public static string GetRegionName(string code)
    {
        switch (code)
        {
            case "asia": return "Asia";
            case "au": return "Australia";
            case "cae": return "Canada East";
            case "cn": return "Chinese Mainland";
            case "eu": return "Europe";
            case "hk": return "HongKong";
            case "in": return "India";
            case "jp": return "Japan";
            case "za": return "South Africa";
            case "sa": return "South America";
            case "kr": return "South Korea";
            case "tr": return "Turkey";
            case "uae": return "U.A.E.";
            case "us": return "USA East";
            case "usw": return "USA West";
            case "ussc": return "USA South Central";
            default: return $"Unknown Region ({code})";
        }
    }

    public static string GetServerRegionName(string code)
    {
        switch (code)
        {
            case "asia": return "Asia";
            case "au": return "Australia";
            case "cae": return "CanadaEast";
            case "cn": return "ChineseMainland";
            case "eu": return "Europe";
            case "hk": return "HongKong";
            case "in": return "India";
            case "jp": return "Japan";
            case "za": return "SouthAfrica";
            case "sa": return "SouthAmerica";
            case "kr": return "SouthKorea";
            case "tr": return "Turkey";
            case "uae": return "UAE";
            case "us": return "USAEast";
            case "usw": return "USAWest";
            case "ussc": return "USASouthCentral";
            default: return $"Unknown Region ({code})";
        }
    }
}

public static class ClipboardExtension
{
    /// <summary>
    /// Puts the string into the Clipboard.
    /// </summary>
    public static void CopyToClipboard(this string str)
    {
        GUIUtility.systemCopyBuffer = str;
    }
}

public static class Shuffler
{
    private static System.Random rng = new System.Random();

    public static async Task Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;

            await Task.Delay(100);
        }
    }
}

public static class CommandLineHelper
{
    public static Dictionary<string, string> GetArgs()
    {
        var args = Environment.GetCommandLineArgs();
        var result = new Dictionary<string, string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("-"))
            {
                string key = args[i].TrimStart('-');
                string value = (i + 1 < args.Length && !args[i + 1].StartsWith("-")) ? args[i + 1] : "true";
                result[key] = value;
            }
        }

        return result;
    }
}