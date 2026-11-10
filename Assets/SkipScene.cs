using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class SkipScene : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName;

    private bool loadingNextScene;

    private void OnEnable()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
    }

    private void OnDisable()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.errorReceived -= OnVideoError;
    }

    private void Start()
    {
        if (videoPlayer == null || videoPlayer.clip == null)
        {
            Debug.LogWarning("This cutscene has no video assigned.", this);
            LoadNextScene();
            return;
        }

        videoPlayer.Play();
    }

    private void Update()
    {
        if (Input.anyKeyDown)
            LoadNextScene();
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        LoadNextScene();
    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        Debug.LogWarning($"Cutscene playback failed: {message}", this);
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (loadingNextScene)
            return;

        if (string.IsNullOrEmpty(nextSceneName) || !Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"Cutscene destination '{nextSceneName}' is missing from the build scene list.", this);
            return;
        }

        loadingNextScene = true;
        SceneManager.LoadScene(nextSceneName);
    }
}
