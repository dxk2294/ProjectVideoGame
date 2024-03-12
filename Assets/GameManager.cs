using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum SceneIndices
{
    TitleScreen = 1,
    MenuTest = 2,
    ComputeTest = 3,
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject transitionalLoadingScreen;
    public GameObject introLoadingScreen;

    public string loadGameSceneName;

    public SpookyGameSummaryState pastState;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;

        SceneManager.LoadSceneAsync((int)SceneIndices.TitleScreen, LoadSceneMode.Additive);
    }

    List<AsyncOperation> scenesLoading = new List<AsyncOperation>();

    private GameObject _activeLoadingScreen;

    public void LoadGame()
    {
        _activeLoadingScreen = introLoadingScreen;
        _activeLoadingScreen.SetActive(true);
        scenesLoading.Add(SceneManager.UnloadSceneAsync((int)SceneIndices.TitleScreen));

        if (loadGameSceneName == null)
        {
            scenesLoading.Add(SceneManager.LoadSceneAsync((int)SceneIndices.ComputeTest, LoadSceneMode.Additive));
        } else
        {
            scenesLoading.Add(SceneManager.LoadSceneAsync(loadGameSceneName, LoadSceneMode.Additive));
        }

        StartCoroutine(GetSceneLoadProgress());
    }

    public void ShowLoseScreen(SpookyGameSummaryState state)
    {
        _activeLoadingScreen = transitionalLoadingScreen;
        pastState = state;
        _activeLoadingScreen.SetActive(true);
        scenesLoading.Add(SceneManager.UnloadSceneAsync(loadGameSceneName));

        scenesLoading.Add(SceneManager.LoadSceneAsync("LoseScene", LoadSceneMode.Additive));

        StartCoroutine(GetSceneLoadProgress());
    }

    public void ShowWinScreen(SpookyGameSummaryState state)
    {
        _activeLoadingScreen = transitionalLoadingScreen;
        pastState = state;
        _activeLoadingScreen.SetActive(true);
        scenesLoading.Add(SceneManager.UnloadSceneAsync(loadGameSceneName));

        scenesLoading.Add(SceneManager.LoadSceneAsync("RealWinScene", LoadSceneMode.Additive));

        StartCoroutine(GetSceneLoadProgress());
    }

    public void ShowMainWindow()
    {
        _activeLoadingScreen = transitionalLoadingScreen;
        _activeLoadingScreen.SetActive(true);

        // Absolute fucking hack
        if (SceneManager.GetSceneByName(loadGameSceneName).isLoaded)
        {
            scenesLoading.Add(SceneManager.UnloadSceneAsync(loadGameSceneName));
        }
        if (SceneManager.GetSceneByName("LoseScene").isLoaded)
        {
            scenesLoading.Add(SceneManager.UnloadSceneAsync("LoseScene"));
        }
        if (SceneManager.GetSceneByName("RealWinScene").isLoaded)
        {
            scenesLoading.Add(SceneManager.UnloadSceneAsync("RealWinScene"));
        }

        scenesLoading.Add(SceneManager.LoadSceneAsync("TitleScene", LoadSceneMode.Additive));

        StartCoroutine(GetSceneLoadProgress());
    }

    bool _waitingForSpaceToContinue;
    public IEnumerator GetSceneLoadProgress()
    {
        foreach(var sceneLoad in scenesLoading)
        {
            while (!sceneLoad.isDone)
            {
                float totalProgress = 0;
                foreach(var operation in scenesLoading)
                {
                    totalProgress += operation.progress;
                }
                _activeLoadingScreen.GetComponentInChildren<Slider>().value = (totalProgress / scenesLoading.Count);
                yield return null;
            }
        }

        if(_activeLoadingScreen == transitionalLoadingScreen)
        {
            _activeLoadingScreen.gameObject.SetActive(false);
            scenesLoading.Clear();
        }
        else
        {
            // O M E G A L U L
            _activeLoadingScreen.GetComponentInChildren<Slider>().value = 1.0f;
            var loading = _activeLoadingScreen.transform.FindDeepChild("Loading");
            loading.GetComponent<TextMeshProUGUI>().text= "press space to continue";
            _waitingForSpaceToContinue = true;
            Time.timeScale = 0.0f;
        }
    }

    private void Update()
    {
        if (_waitingForSpaceToContinue && Input.GetKeyDown(KeyCode.Space))
        {
            _activeLoadingScreen.gameObject.SetActive(false);
            scenesLoading.Clear();
            Time.timeScale = 1.0f;
        }
    }
}

public static class TransformDeepChildExtension
{
    public static Transform FindDeepChild(this Transform aParent, string aName)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(aParent);
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (c.name == aName)
                return c;
            foreach (Transform t in c)
                queue.Enqueue(t);
        }
        return null;
    }
}