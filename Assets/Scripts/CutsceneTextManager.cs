using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CutsceneTextManager : MonoBehaviour
{
    [SerializeField] private TextAsset dialog;
    [SerializeField] private Text textDialogObject;
    [SerializeField] private string nextSceneName;

    private int currentLineNumber;
    private readonly List<string> availableTextLines = new List<string>();
    private bool loadingNextScene;

    private void Start()
    {
        if (dialog == null || textDialogObject == null)
        {
            Debug.LogError("Assign the dialogue asset and its UI Text in the Inspector.", this);
            return;
        }

        using (var reader = new StringReader(dialog.text))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
                availableTextLines.Add(line);
        }

        if (availableTextLines.Count > 0)
            textDialogObject.text = availableTextLines[currentLineNumber];
    }

    public void SetNextLine()
    {
        if (currentLineNumber + 1 < availableTextLines.Count)
        {
            currentLineNumber++;
            textDialogObject.text = availableTextLines[currentLineNumber];
        }
        else
        {
            GoToNextScene();
        }
    }

    public void SetPreviousLine()
    {
        if (currentLineNumber > 0)
        {
            currentLineNumber--;
            textDialogObject.text = availableTextLines[currentLineNumber];
        }
    }

    public void GoToNextScene()
    {
        if (loadingNextScene)
            return;

        if (string.IsNullOrEmpty(nextSceneName) || !Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"Dialogue destination '{nextSceneName}' is missing from the build scene list.", this);
            return;
        }

        loadingNextScene = true;
        SceneManager.LoadScene(nextSceneName);
    }
}
