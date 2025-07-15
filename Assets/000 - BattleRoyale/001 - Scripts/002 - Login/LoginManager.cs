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
using System.Text.RegularExpressions;
using System.Security.Policy;
using System.Buffers;
using Fusion;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Analytics.Internal.Platform;

public class LoginManager : MonoBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private NetworkRunner instanceRunner;

    [Space]
    [SerializeField] private AudioClip buttonClip;

    [Space]
    [SerializeField] private GameObject loginObj;
    [SerializeField] private GameObject registerObj;

    [Space]
    [SerializeField] private TMP_InputField username;
    [SerializeField] private TMP_InputField password;
    [SerializeField] private Toggle rememberMe;
    [SerializeField] private AudioClip bgMusic;

    [Header("BRIGHTNESS")]
    [SerializeField] private float maxBrightness;
    [SerializeField] private float minBrightness;
    [SerializeField] private Volume postProcessing;

    [Space]
    [SerializeField] private TMP_InputField usernameRegister;
    [SerializeField] private TMP_InputField passwordRegister;
    [SerializeField] private TMP_InputField confirmPasswordRegister;
    [SerializeField] private TMP_InputField emailRegister;
    [SerializeField] private TMP_Dropdown countryDropdowns;

    [Space]
    [SerializeField] private GameObject loadingServerList;
    [SerializeField] private GameObject serverList;
    [SerializeField] private GameObject selectedServer;
    [SerializeField] private TextMeshProUGUI selectedServerTMP;
    [SerializeField] private Sprite goodPing;
    [SerializeField] private Sprite mediumPing;
    [SerializeField] private Sprite badPing;
    [SerializeField] private Image pingImg;
    [SerializeField] private Button startGameBtn;
    [SerializeField] private TextMeshProUGUI totalPlayersOnlineTMP;
    [SerializeField] private TextMeshProUGUI selectedServerPlayersOnlineTMP;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] public NetworkRunner currentRunnerInstance;

    //  ============================

    ColorAdjustments colorAdjustments;

    Dictionary<string, string> countryDictionary = new Dictionary<string, string>
        {
            { "Afghanistan", "AF" },
            { "Åland Islands", "AX" },
            { "Albania", "AL" },
            { "Algeria", "DZ" },
            { "American Samoa", "AS" },
            { "AndorrA", "AD" },
            { "Angola", "AO" },
            { "Anguilla", "AI" },
            { "Antarctica", "AQ" },
            { "Antigua and Barbuda", "AG" },
            { "Argentina", "AR" },
            { "Armenia", "AM" },
            { "Aruba", "AW" },
            { "Australia", "AU" },
            { "Austria", "AT" },
            { "Azerbaijan", "AZ" },
            { "Bahamas", "BS" },
            { "Bahrain", "BH" },
            { "Bangladesh", "BD" },
            { "Barbados", "BB" },
            { "Belarus", "BY" },
            { "Belgium", "BE" },
            { "Belize", "BZ" },
            { "Benin", "BJ" },
            { "Bermuda", "BM" },
            { "Bhutan", "BT" },
            { "Bolivia", "BO" },
            { "Bosnia and Herzegovina", "BA" },
            { "Botswana", "BW" },
            { "Bouvet Island", "BV" },
            { "Brazil", "BR" },
            { "British Indian Ocean Territory", "IO" },
            { "Brunei Darussalam", "BN" },
            { "Bulgaria", "BG" },
            { "Burkina Faso", "BF" },
            { "Burundi", "BI" },
            { "Cambodia", "KH" },
            { "Cameroon", "CM" },
            { "Canada", "CA" },
            { "Cape Verde", "CV" },
            { "Cayman Islands", "KY" },
            { "Central African Republic", "CF" },
            { "Chad", "TD" },
            { "Chile", "CL" },
            { "China", "CN" },
            { "Christmas Island", "CX" },
            { "Cocos (Keeling) Islands", "CC" },
            { "Colombia", "CO" },
            { "Comoros", "KM" },
            { "Congo", "CG" },
            { "Congo, The Democratic Republic of the", "CD" },
            { "Cook Islands", "CK" },
            { "Costa Rica", "CR" },
            { "Cote D'Ivoire", "CI" },
            { "Croatia", "HR" },
            { "Cuba", "CU" },
            { "Cyprus", "CY" },
            { "Czech Republic", "CZ" },
            { "Denmark", "DK" },
            { "Djibouti", "DJ" },
            { "Dominica", "DM" },
            { "Dominican Republic", "DO" },
            { "Ecuador", "EC" },
            { "Egypt", "EG" },
            { "El Salvador", "SV" },
            { "Equatorial Guinea", "GQ" },
            { "Eritrea", "ER" },
            { "Estonia", "EE" },
            { "Ethiopia", "ET" },
            { "Falkland Islands (Malvinas)", "FK" },
            { "Faroe Islands", "FO" },
            { "Fiji", "FJ" },
            { "Finland", "FI" },
            { "France", "FR" },
            { "French Guiana", "GF" },
            { "French Polynesia", "PF" },
            { "French Southern Territories", "TF" },
            { "Gabon", "GA" },
            { "Gambia", "GM" },
            { "Georgia", "GE" },
            { "Germany", "DE" },
            { "Ghana", "GH" },
            { "Gibraltar", "GI" },
            { "Greece", "GR" },
            { "Greenland", "GL" },
            { "Grenada", "GD" },
            { "Guadeloupe", "GP" },
            { "Guam", "GU" },
            { "Guatemala", "GT" },
            { "Guernsey", "GG" },
            { "Guinea", "GN" },
            { "Guinea-Bissau", "GW" },
            { "Guyana", "GY" },
            { "Haiti", "HT" },
            { "Heard Island and Mcdonald Islands", "HM" },
            { "Holy See (Vatican City State)", "VA" },
            { "Honduras", "HN" },
            { "Hong Kong", "HK" },
            { "Hungary", "HU" },
            { "Iceland", "IS" },
            { "India", "IN" },
            { "Indonesia", "ID" },
            { "Iran, Islamic Republic Of", "IR" },
            { "Iraq", "IQ" },
            { "Ireland", "IE" },
            { "Isle of Man", "IM" },
            { "Israel", "IL" },
            { "Italy", "IT" },
            { "Jamaica", "JM" },
            { "Japan", "JP" },
            { "Jersey", "JE" },
            { "Jordan", "JO" },
            { "Kazakhstan", "KZ" },
            { "Kenya", "KE" },
            { "Kiribati", "KI" },
            { "Korea, Democratic People'S Republic of", "KP" },
            { "Korea, Republic of", "KR" },
            { "Kuwait", "KW" },
            { "Kyrgyzstan", "KG" },
            { "Lao People'S Democratic Republic", "LA" },
            { "Latvia", "LV" },
            { "Lebanon", "LB" },
            { "Lesotho", "LS" },
            { "Liberia", "LR" },
            { "Libyan Arab Jamahiriya", "LY" },
            { "Liechtenstein", "LI" },
            { "Lithuania", "LT" },
            { "Luxembourg", "LU" },
            { "Macao", "MO" },
            { "Macedonia, The Former Yugoslav Republic of", "MK" },
            { "Madagascar", "MG" },
            { "Malawi", "MW" },
            { "Malaysia", "MY" },
            { "Maldives", "MV" },
            { "Mali", "ML" },
            { "Malta", "MT" },
            { "Marshall Islands", "MH" },
            { "Martinique", "MQ" },
            { "Mauritania", "MR" },
            { "Mauritius", "MU" },
            { "Mayotte", "YT" },
            { "Mexico", "MX" },
            { "Micronesia, Federated States of", "FM" },
            { "Moldova, Republic of", "MD" },
            { "Monaco", "MC" },
            { "Mongolia", "MN" },
            { "Montserrat", "MS" },
            { "Morocco", "MA" },
            { "Mozambique", "MZ" },
            { "Myanmar", "MM" },
            { "Namibia", "NA" },
            { "Nauru", "NR" },
            { "Nepal", "NP" },
            { "Netherlands", "NL" },
            { "Netherlands Antilles", "AN" },
            { "New Caledonia", "NC" },
            { "New Zealand", "NZ" },
            { "Nicaragua", "NI" },
            { "Niger", "NE" },
            { "Nigeria", "NG" },
            { "Niue", "NU" },
            { "Norfolk Island", "NF" },
            { "Northern Mariana Islands", "MP" },
            { "Norway", "NO" },
            { "Oman", "OM" },
            { "Pakistan", "PK" },
            { "Palau", "PW" },
            { "Palestinian Territory, Occupied", "PS" },
            { "Panama", "PA" },
            { "Papua New Guinea", "PG" },
            { "Paraguay", "PY" },
            { "Peru", "PE" },
            { "Philippines", "PH" },
            { "Pitcairn", "PN" },
            { "Poland", "PL" },
            { "Portugal", "PT" },
            { "Puerto Rico", "PR" },
            { "Qatar", "QA" },
            { "Reunion", "RE" },
            { "Romania", "RO" },
            { "Russian Federation", "RU" },
            { "RWANDA", "RW" },
            { "Saint Helena", "SH" },
            { "Saint Kitts and Nevis", "KN" },
            { "Saint Lucia", "LC" },
            { "Saint Pierre and Miquelon", "PM" },
            { "Saint Vincent and the Grenadines", "VC" },
            { "Samoa", "WS" },
            { "San Marino", "SM" },
            { "Sao Tome and Principe", "ST" },
            { "Saudi Arabia", "SA" },
            { "Senegal", "SN" },
            { "Serbia and Montenegro", "CS" },
            { "Seychelles", "SC" },
            { "Sierra Leone", "SL" },
            { "Singapore", "SG" },
            { "Slovakia", "SK" },
            { "Slovenia", "SI" },
            { "Solomon Islands", "SB" },
            { "Somalia", "SO" },
            { "South Africa", "ZA" },
            { "South Georgia and the South Sandwich Islands", "GS" },
            { "Spain", "ES" },
            { "Sri Lanka", "LK" },
            { "Sudan", "SD" },
            { "Suriname", "SR" },
            { "Svalbard and Jan Mayen", "SJ" },
            { "Swaziland", "SZ" },
            { "Sweden", "SE" },
            { "Switzerland", "CH" },
            { "Syrian Arab Republic", "SY" },
            { "Taiwan, Province of China", "TW" },
            { "Tajikistan", "TJ" },
            { "Tanzania, United Republic of", "TZ" },
            { "Thailand", "TH" },
            { "Timor-Leste", "TL" },
            { "Togo", "TG" },
            { "Tokelau", "TK" },
            { "Tonga", "TO" },
            { "Trinidad and Tobago", "TT" },
            { "Tunisia", "TN" },
            { "Turkey", "TR" },
            { "Turkmenistan", "TM" },
            { "Turks and Caicos Islands", "TC" },
            { "Tuvalu", "TV" },
            { "Uganda", "UG" },
            { "Ukraine", "UA" },
            { "United Arab Emirates", "AE" },
            { "United Kingdom", "GB" },
            { "United States", "US" },
            { "United States Minor Outlying Islands", "UM" },
            { "Uruguay", "UY" },
            { "Uzbekistan", "UZ" },
            { "Vanuatu", "VU" },
            { "Venezuela", "VE" },
            { "Viet Nam", "VN" },
            { "Virgin Islands, British", "VG" },
            { "Virgin Islands, U.S.", "VI" },
            { "Wallis and Futuna", "WF" },
            { "Western Sahara", "EH" },
            { "Yemen", "YE" },
            { "Zambia", "ZM" },
            { "Zimbabwe", "ZW" }
        };

    public Dictionary<string, int> AvailableServers = new Dictionary<string, int>();

    //  ============================

    private void Awake()
    {
        float brightness = GameManager.Instance.GraphicsManager.CurrentBrightness * 10f - 5f;
        if (postProcessing.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            colorAdjustments.postExposure.value = brightness;
        }

        GameManager.Instance.AudioController.SetBGMusic(bgMusic);
        GameManager.Instance.SceneController.AddActionLoadinList(CheckRememberMe());
        GameManager.Instance.SceneController.AddActionLoadinList(userData.CheckControlSettingSave());
        GameManager.Instance.SceneController.AddActionLoadinList(FillUpSignUpCountry());
        GameManager.Instance.SceneController.ActionPass = true;

        GameManager.Instance.SocketMngr.OnPlayerCountServerChange += PlayerCountChange;


        GameManager.Instance.SocketMngr.OnPlayerCountAsiaServerChange += AsiaChange;
        GameManager.Instance.SocketMngr.OnPlayerCountAfricaServerChange += AfricaChange;
        GameManager.Instance.SocketMngr.OnPlayerCounUAEtServerChange += UAEChange;
        GameManager.Instance.SocketMngr.OnPlayerCountAmericaEastServerChange += USChange;
        GameManager.Instance.SocketMngr.OnPlayerCountAmericaWestServerChange += USWChange;
    }

    private void OnDisable()
    {
        GameManager.Instance.SocketMngr.OnPlayerCountServerChange -= PlayerCountChange;

        GameManager.Instance.SocketMngr.OnPlayerCountAsiaServerChange -= AsiaChange;
        GameManager.Instance.SocketMngr.OnPlayerCountAfricaServerChange -= AfricaChange;
        GameManager.Instance.SocketMngr.OnPlayerCounUAEtServerChange -= UAEChange;
        GameManager.Instance.SocketMngr.OnPlayerCountAmericaEastServerChange -= USChange;
        GameManager.Instance.SocketMngr.OnPlayerCountAmericaWestServerChange -= USWChange;
    }

    private void AsiaChange(object sender, EventArgs e)
    {
        CheckServerCount();
    }

    private void AfricaChange(object sender, EventArgs e)
    {
        CheckServerCount();
    }

    private void UAEChange(object sender, EventArgs e)
    {
        CheckServerCount();
    }

    private void USChange(object sender, EventArgs e)
    {
        CheckServerCount();
    }

    private void USWChange(object sender, EventArgs e)
    {
        CheckServerCount();
    }

    private void PlayerCountChange(object sender, EventArgs e)
    {
        totalPlayersOnlineTMP.text = $"Online: <color=green>{GameManager.Instance.SocketMngr.PlayerCountServer:n0}</color>";
    }

    private IEnumerator CheckRememberMe()
    {
        rememberMe.isOn = userData.RememberMe;

        Debug.Log($"Remember Me: {userData.RememberMe}");

        if (!userData.RememberMe) yield break;

        username.text = userData.Username;
        password.text = userData.Password;
    }

    private IEnumerator FillUpSignUpCountry()
    {
        List<string> itemNames = new List<string>(countryDictionary.Keys);

        countryDropdowns.ClearOptions();
        countryDropdowns.AddOptions(itemNames);

        yield return null;
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


                Debug.Log($"Remember Me login: {rememberMe.isOn}");

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

                Debug.Log("Getting available regions");

                GameManager.Instance.AddJob(() =>
                {
                    GetAvailableRegions(() =>
                    {
                        CheckSelectedServer();
                    });
                });
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

    public void StartGame()
    {
        GameManager.Instance.SocketMngr.EmitEvent("selectregion", userData.SelectedServer);

        GameManager.Instance.SceneController.CurrentScene = "Lobby";
    }

    public void CheckSelectedServer()
    {
        CheckServerCount();

        if (userData.SelectedServer == "")
        {
            var lowest = AvailableServers.Aggregate((x, y) => x.Value < y.Value ? x : y);

            userData.SelectedServer = lowest.Key;

            selectedServerTMP.text = $"{GameManager.GetRegionName(lowest.Key)} <size=25>(Ping: {(lowest.Value < 100 ? $"<color=#00BA0D>{lowest.Value}</color>" : lowest.Value > 100 && lowest.Value < 250 ? $"<color=orange>{lowest.Value}</color>" : $"<color=red>{lowest.Value}</color>")})</size>";
            pingImg.sprite = lowest.Value < 100 ? goodPing : lowest.Value > 100 && lowest.Value < 250 ? mediumPing : badPing;
            startGameBtn.interactable = true;
        }
        else
        {
            if (AvailableServers.ContainsKey(userData.SelectedServer))
            {
                selectedServerTMP.text = $"{GameManager.GetRegionName(userData.SelectedServer)} <size=25>{(AvailableServers[userData.SelectedServer] < 100 ? $"<color=#00BA0D>{AvailableServers[userData.SelectedServer]}ms</color>" : AvailableServers[userData.SelectedServer] > 100 && AvailableServers[userData.SelectedServer] < 250 ? $"<color=#D26E05>{AvailableServers[userData.SelectedServer]}ms</color>" : $"<color=red>{AvailableServers[userData.SelectedServer]}ms</color>")}</size>";
                pingImg.sprite = AvailableServers[userData.SelectedServer] < 100 ? goodPing : AvailableServers[userData.SelectedServer] > 100 && AvailableServers[userData.SelectedServer] < 250 ? mediumPing : badPing;
                startGameBtn.interactable = true;
            }
            else
            {
                selectedServerTMP.text = $"{GameManager.GetRegionName(userData.SelectedServer)} <size=25>(<color=red>Not Available</color>)</size>";
                pingImg.sprite = badPing;
                startGameBtn.interactable = false;
            }
        }

        if (rememberMe.isOn)
            PlayerPrefs.SetString("server", userData.SelectedServer);
    }

    private void CheckServerCount()
    {
        if (userData.SelectedServer == "asia")
            selectedServerPlayersOnlineTMP.text = GameManager.Instance.SocketMngr.PlayerAsiaCountServer.ToString("n0");
        else if (userData.SelectedServer == "za")
            selectedServerPlayersOnlineTMP.text = GameManager.Instance.SocketMngr.PlayerAfricaCountServer.ToString("n0");
        else if (userData.SelectedServer == "uae")
            selectedServerPlayersOnlineTMP.text = GameManager.Instance.SocketMngr.PlayerUAECountServer.ToString("n0");
        else if (userData.SelectedServer == "us")
            selectedServerPlayersOnlineTMP.text = GameManager.Instance.SocketMngr.PlayerAmericaEastCountServer.ToString("n0");
        else if (userData.SelectedServer == "usw")
            selectedServerPlayersOnlineTMP.text = GameManager.Instance.SocketMngr.PlayerAmericaWestCountServer.ToString("n0");
    }

    public void Register()
    {
        if (usernameRegister.text == "")
        {
            GameManager.Instance.NotificationController.ShowError("Please enter your username first and try again!", null);
            return;
        }
        else if (passwordRegister.text == "")
        {
            GameManager.Instance.NotificationController.ShowError("Please enter your password first and try again!", null);
            return;
        }
        else if (confirmPasswordRegister.text == "")
        {
            GameManager.Instance.NotificationController.ShowError("Please enter confirm password first and try again!", null);
            return;
        }
        else if (passwordRegister.text != confirmPasswordRegister.text)
        {
            GameManager.Instance.NotificationController.ShowError("Password does not match confirm password!", null);
            return;
        }
        else if (emailRegister.text == "")
        {
            GameManager.Instance.NotificationController.ShowError("Please enter your email first and try again!", null);
            return;
        }
        else if (!countryDictionary.ContainsKey(countryDropdowns.options[countryDropdowns.value].text))
        {
            GameManager.Instance.NotificationController.ShowError("Please select your country first!", null);
            return;
        }
        else if (usernameRegister.text.Length < 5 || usernameRegister.text.Length > 15)
        {
            GameManager.Instance.NotificationController.ShowError("Minimum of 5 and maximum of 15 characters only for username! Please try again.", null);
            return;
        }
        else if (passwordRegister.text.Length < 5 || passwordRegister.text.Length > 20)
        {
            GameManager.Instance.NotificationController.ShowError("Minimum of 5 and maximum of 20 characters only for password! Please try again.", null);
            return;
        }
        else if (Regex.IsMatch(usernameRegister.text, @"[^\w]"))
        {
            GameManager.Instance.NotificationController.ShowError("Username contains spaces or special characters.", null);
            return;
        }
        else if (Regex.IsMatch(passwordRegister.text, @"[^a-zA-Z0-9\s\[\]@]"))
        {
            GameManager.Instance.NotificationController.ShowError("Password contains spaces or special characters (excluding [ ] and @).", null);
            return;
        }
        else if (!Regex.IsMatch(emailRegister.text, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
        {
            GameManager.Instance.NotificationController.ShowError("The email is invalid! Please enter a valid email.", null);
            return;
        }

        GameManager.Instance.NoBGLoading.SetActive(true);

        StartCoroutine(GameManager.Instance.PostRequest("/auth/register", "", new Dictionary<string, object>
        {
            { "username", usernameRegister.text },
            { "password", passwordRegister.text },
            { "email", emailRegister.text },
            { "country", countryDropdowns.options[countryDropdowns.value].text }
        }, false, (response) =>
        {
            GameManager.Instance.NotificationController.ShowError("You are now registered! You can now login your account.", () =>
            {
                loginObj.SetActive(true);
                registerObj.SetActive(false);
                usernameRegister.text = "";
                passwordRegister.text = "";
                confirmPasswordRegister.text = "";
                emailRegister.text = "";
            });
        }, () => GameManager.Instance.NoBGLoading.SetActive(false), false));
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

    public void OpenSocialLinks(string titlename)
    {
        GameManager.Instance.NoBGLoading.SetActive(true);

        StartCoroutine(GameManager.Instance.GetRequest("/sociallinks/getspecificsociallink", $"?title={titlename}", false, (response) =>
        {
            if (response != null)
            {
                SocialLinkData tempdata = JsonConvert.DeserializeObject<SocialLinkData>(response.ToString());

                if (tempdata == null)
                {
                    GameManager.Instance.NotificationController.ShowError("This social media link is still not yet live! Please stay tuned.");
                    return;
                }

                Application.OpenURL(tempdata.link);
            }
            else
                GameManager.Instance.NotificationController.ShowError("This social media link is still not yet live! Please stay tuned.");
        }, null, false));
    }

    public void ButtonPress() => GameManager.Instance.AudioController.PlaySFX(buttonClip);


    public async void GetAvailableRegions(Action action = null)
    {
        if (currentRunnerInstance != null)
        {
            Destroy(currentRunnerInstance.gameObject);

            currentRunnerInstance = null;
        }
        else
        {
            currentRunnerInstance = Instantiate(instanceRunner);
        }

        var _tokenSource = new CancellationTokenSource();

        var regions = await NetworkRunner.GetAvailableRegions(cancellationToken: _tokenSource.Token);

        AvailableServers.Clear();

        foreach (var region in regions)
        {
            if (userData.SelectedServer != "asia" && userData.SelectedServer != "za" && userData.SelectedServer != "uae" && userData.SelectedServer != "us" && userData.SelectedServer != "usw") continue;

            AvailableServers.Add(region.RegionCode, region.RegionPing);
        }

        action?.Invoke();

        Destroy(currentRunnerInstance.gameObject);

        currentRunnerInstance = null;

        loginObj.SetActive(false);
        selectedServer.SetActive(true);

        GameManager.Instance.NoBGLoading.SetActive(false);
    }

    public async void RefreshAvailableRegions()
    {
        loadingServerList.SetActive(true);
        serverList.SetActive(false);

        Debug.Log("Starting refreshing available regions");

        if (currentRunnerInstance != null)
        {
            Destroy(currentRunnerInstance.gameObject);

            currentRunnerInstance = null;
        }
        else
        {
            currentRunnerInstance = Instantiate(instanceRunner);
        }


        Debug.Log("Start refresh api call");

        var _tokenSource = new CancellationTokenSource();

        var regions = await NetworkRunner.GetAvailableRegions(cancellationToken: _tokenSource.Token);

        Debug.Log("Clearing Available Servers");

        AvailableServers.Clear();

        foreach (var region in regions)
        {
            AvailableServers.Add(region.RegionCode, region.RegionPing);
        }

        Destroy(currentRunnerInstance.gameObject);

        currentRunnerInstance = null;

        loadingServerList.SetActive(false);
        serverList.SetActive(true);

        Debug.Log("Done Refresh");
    }
}

[System.Serializable]
public class SocialLinkData
{
    public string _id;
    public string link;
    public string title;
    public string type;
}