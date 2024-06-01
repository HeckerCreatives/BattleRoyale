using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    private event EventHandler sceneChange;
    public event EventHandler onSceneChange
    {
        add
        {
            if (sceneChange == null || !sceneChange.GetInvocationList().Contains(value))
                sceneChange += value;
        }
        remove { sceneChange -= value; }
    }
    public string CurrentScene
    {
        get => currentScene;
        set
        {
            if (currentScene != "")
                previousScene = currentScene;

            currentScene = value;
            sceneChange?.Invoke(this, EventArgs.Empty);
        }
    }
    public string LastScene
    {
        get => previousScene;
        private set => previousScene = value;
    }

    public bool ActionPass
    {
        get => actionPass;
        set => actionPass = value;
    }

    public List<IEnumerator> GetActionLoadingList
    {
        get => actionLoading;
    }
    public void AddActionLoadinList(IEnumerator action)
    {
        actionLoading.Add(action);
    }

    private event EventHandler MultiplayerSceneChange;
    public event EventHandler OnMultuplayerSceneChange
    {
        add
        {
            if (MultiplayerSceneChange == null || !MultiplayerSceneChange.GetInvocationList().Contains(value))
                MultiplayerSceneChange += value;
        }
        remove { MultiplayerSceneChange -= value; }
    }
    public bool MultiplayerScene
    {
        get => multiplayerScene;
        set
        {
            multiplayerScene = value;
            MultiplayerSceneChange?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool DoneLoading
    {
        get => doneLoading;
    }

    //  ======================================================

    [SerializeField] private GameObject loadingScreenBGObj;
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private CanvasGroup loadingCG;
    [SerializeField] private GameObject splashScreenObj;

    //[Header("TRIVIA")]
    //[SerializeField] private float triviaTransitionSpeed;
    //[SerializeField] private float triviaDelay;
    //[SerializeField] private LeanTweenType triviaEaseType;
    //[SerializeField] private Image triviaPanel;
    //[SerializeField] private Image bgImg;
    //[SerializeField] private TextMeshProUGUI triviaTMP;
    //[SerializeField] private List<string> triviaList;
    //[SerializeField] private List<Sprite> triviaImgList;

    [Header("ANIMATION")]
    [SerializeField] private float speed;
    [SerializeField] private float loadingBarSpeed;
    [SerializeField] private LeanTweenType easeType;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] private bool doneLoading;
    [ReadOnly][SerializeField] private bool firstLoading;
    [ReadOnly][SerializeField] private bool multiplayerScene;
    [ReadOnly][SerializeField] private string currentScene;
    [ReadOnly][SerializeField] private string previousScene;
    [ReadOnly][SerializeField] private bool actionPass;
    [ReadOnly][SerializeField] private float totalSceneProgress;

    private List<IEnumerator> actionLoading;
    AsyncOperation scenesLoading;

    Coroutine LoadingCoroutine, MultiplayerLoadingCoroutine;

    private void Awake()
    {
        firstLoading = true;

        actionLoading = new List<IEnumerator>();
        scenesLoading = new AsyncOperation();

        onSceneChange += SceneChange;
        OnMultuplayerSceneChange += MultiplayerChangeScene;
    }

    private void OnDisable()
    {
        onSceneChange -= SceneChange;
        OnMultuplayerSceneChange -= MultiplayerChangeScene;
    }

    private void MultiplayerChangeScene(object sender, EventArgs e)
    {
        if (MultiplayerLoadingCoroutine != null) StopCoroutine(MultiplayerLoadingCoroutine);
        MultiplayerLoadingCoroutine = StartCoroutine(MultiplayerLoading());
    }

    private void SceneChange(object sender, EventArgs e)
    {
        if (!MultiplayerScene)
            LoadingCoroutine = StartCoroutine(Loading());
    }

    public IEnumerator MultiplayerLoading()
    {
        Debug.Log("multiplayer loading");
        doneLoading = false;

        loadingScreenBGObj.SetActive(true);

        loadingScreenBGObj.SetActive(true);

        loadingSlider.value = 0f;

        LeanTween.alphaCanvas(loadingCG, 1f, speed).setEase(easeType);

        yield return new WaitWhile(() => loadingCG.alpha != 1f);

        LastScene = CurrentScene;
        currentScene = SceneManager.GetActiveScene().name;

        LeanTween.alphaCanvas(loadingCG, 1f, speed).setEase(easeType);

        yield return new WaitWhile(() => loadingCG.alpha != 1f);

        while (!actionPass) yield return null;

        actionPass = false; //  THIS IS FOR RESET

        for (int a = 0; a < GetActionLoadingList.Count; a++)
        {
            yield return StartCoroutine(GetActionLoadingList[a]);

            int index = a + 1;

            totalSceneProgress = (float)index / (1 + GetActionLoadingList.Count);

            LeanTween.value(loadingSlider.gameObject, a => loadingSlider.value = a, loadingSlider.value, totalSceneProgress, loadingBarSpeed).setEase(easeType);

            yield return new WaitWhile(() => loadingSlider.value != totalSceneProgress);

            yield return null;
        }

        totalSceneProgress = scenesLoading.progress;

        LeanTween.value(loadingSlider.gameObject, a => loadingSlider.value = a, loadingSlider.value, totalSceneProgress, loadingBarSpeed).setEase(easeType);

        yield return new WaitForSecondsRealtime(loadingBarSpeed);

        LeanTween.alphaCanvas(loadingCG, 0f, speed).setEase(easeType);

        yield return new WaitWhile(() => loadingCG.alpha != 0f);

        currentScene = SceneManager.GetActiveScene().name;

        loadingScreenBGObj.SetActive(false);

        GetActionLoadingList.Clear();

        loadingSlider.value = 0f;

        totalSceneProgress = 0f;

        MultiplayerScene = false;

        doneLoading = true;

        MultiplayerLoadingCoroutine = null;
    }


    public IEnumerator Loading()
    {

        doneLoading = false;

        Time.timeScale = 0f;

        if (firstLoading)
            splashScreenObj.SetActive(true);
        else
        {
            loadingScreenBGObj.SetActive(true);

            loadingSlider.value = 0f;

            LeanTween.alphaCanvas(loadingCG, 1f, speed).setEase(easeType);

            yield return new WaitWhile(() => loadingCG.alpha != 1f);
        }

        scenesLoading = SceneManager.LoadSceneAsync(CurrentScene, LoadSceneMode.Single);

        scenesLoading.allowSceneActivation = false;

        while (!scenesLoading.isDone)
        {
            if (scenesLoading.progress >= 0.9f)
            {
                scenesLoading.allowSceneActivation = true;

                break;
            }

            yield return null;
        }

        while (!actionPass) yield return null;

        actionPass = false; //  THIS IS FOR RESET

        Debug.Log($"ACTIONS IN LOADING: {GetActionLoadingList.Count}");

        if (GetActionLoadingList.Count > 0)
        {
            for (int a = 0; a < GetActionLoadingList.Count; a++)
            {
                yield return StartCoroutine(GetActionLoadingList[a]);

                int index = a + 1;

                totalSceneProgress = (float)index / (1 + GetActionLoadingList.Count);

                if (!firstLoading)
                {
                    LeanTween.value(loadingSlider.gameObject, a => loadingSlider.value = a, loadingSlider.value, totalSceneProgress, loadingBarSpeed).setEase(easeType);

                    yield return new WaitWhile(() => loadingSlider.value != totalSceneProgress);
                }

                yield return null;
            }

            totalSceneProgress = scenesLoading.progress;

            if (!firstLoading)
                LeanTween.value(loadingSlider.gameObject, a => loadingSlider.value = a, loadingSlider.value, totalSceneProgress, loadingBarSpeed).setEase(easeType);
        }
        else
        {
            totalSceneProgress = 1f;

            LeanTween.value(loadingSlider.gameObject, a => loadingSlider.value = a, loadingSlider.value, totalSceneProgress, loadingBarSpeed).setEase(easeType);

            yield return new WaitWhile(() => loadingSlider.value != totalSceneProgress);
        }

        if (!firstLoading)
        {
            yield return new WaitForSecondsRealtime(loadingBarSpeed);

            LeanTween.alphaCanvas(loadingCG, 0f, speed).setEase(easeType);

            yield return new WaitWhile(() => loadingCG.alpha != 0f);

            loadingScreenBGObj.SetActive(false);

            loadingSlider.value = 0f;
        }
        else
        {
            yield return new WaitForSecondsRealtime(0f);

            splashScreenObj.SetActive(false);

            firstLoading = false;
        }


        totalSceneProgress = 0f;

        GetActionLoadingList.Clear();

        doneLoading = true;

        LoadingCoroutine = null;

        Time.timeScale = 1f;
    }

    public void StopLoading()
    {
        if (DoneLoading) return;

        actionLoading.Clear();

        if (LoadingCoroutine != null)
            StopCoroutine(LoadingCoroutine);
    }
}
