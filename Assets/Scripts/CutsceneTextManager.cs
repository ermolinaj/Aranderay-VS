using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    List<string> availableTextLines = new List<string>();

    // Start is called before the first frame update
    void Start()
    {
        textDialogObject = GameObject.Find("DialogText").GetComponent<Text>();
        SetDialogForScene(sceneNumber);
        textDialogObject.text = GetLine(currentLineNumber);
    }

    public void SetDialogForScene(int sceneNumber)
    {
        //string path = Directory.GetCurrentDirectory() + "\\Assets\\DialogText\\DialogScene" + sceneNumber + ".txt";
        string path = Directory.GetCurrentDirectory() + "\\Aranderay_Data\\Resources\\DialogScene" + sceneNumber + ".txt";
        string[] logFile = File.ReadAllLines(path);
        foreach (string s in logFile) availableTextLines.Add(s);
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
