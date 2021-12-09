using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CutsceneTextManager : MonoBehaviour
{
    [SerializeField]
    int sceneNumber;
    [SerializeField]
    string nextSceneName;

    int currentLineNumber = 0;
    Text textDialogObject;
    List<string> availableTextLines;

    // Start is called before the first frame update
    void Start()
    {
        textDialogObject = GameObject.Find("DialogText").GetComponent<Text>();
        SetDialogForScene(sceneNumber);
        textDialogObject.text = GetLine(currentLineNumber);
    }

    public void SetDialogForScene(int sceneNumber)
    {
        availableTextLines = new List<string>() { "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.", "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat." };
    }

    public string GetLine(int lineNumber)
    {
        return availableTextLines[lineNumber];
    }

    public void SetNextLine()
    {
        if (currentLineNumber + 1 < availableTextLines.Count)
        {
            currentLineNumber++;
            textDialogObject.text = GetLine(currentLineNumber);
        }
        else
            GoToNextScene();
    }

    public void SetPreviousLine()
    {
        if (currentLineNumber - 1 >= 0)
        {
            currentLineNumber--;
            textDialogObject.text = GetLine(currentLineNumber);
        }
            
    }

    public void GoToNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
