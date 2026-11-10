using System.Collections.Generic;
using System.IO;
using System.Text;
using Aranderay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CutsceneTextManager : MonoBehaviour
{
    [SerializeField] private TextAsset dialog;
    [SerializeField] private Text textDialogObject;
    [SerializeField] private Font readingFont;
    [SerializeField] private string nextSceneName;

    private int currentPage;
    private readonly List<string> pages = new List<string>();
    private Text pageLabel;
    private Text nextLabel;
    private Text previousLabel;
    private Button previousButton;
    private bool loadingNextScene;
    private bool ready;

    private void Start()
    {
        if (dialog == null || textDialogObject == null)
        {
            Debug.LogError("Assign the dialogue asset and its UI Text in the Inspector.", this);
            return;
        }

        string title = ReadPages(dialog.text);
        CreateReadingLayout(title);
        ready = true;
        ShowPage();
    }

    private void Update()
    {
        if (!ready || loadingNextScene)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            SetPreviousLine();
        else if (Input.GetKeyDown(KeyCode.RightArrow) ||
            ((EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))))
            SetNextLine();
    }

    public void SetNextLine()
    {
        if (!ready || loadingNextScene)
            return;

        if (currentPage + 1 < pages.Count)
        {
            currentPage++;
            ShowPage();
        }
        else
        {
            GoToNextScene();
        }
    }

    public void SetPreviousLine()
    {
        if (!ready || loadingNextScene || currentPage == 0)
            return;

        currentPage--;
        ShowPage();
    }

    private void ShowPage()
    {
        textDialogObject.text = pages[currentPage];
        if (pageLabel != null)
            pageLabel.text = $"{currentPage + 1:00} / {pages.Count:00}";
        if (previousButton != null)
        {
            previousButton.interactable = currentPage > 0;
            if (previousLabel != null)
                previousLabel.color = previousButton.interactable ? TvOverlay.Ink : TvOverlay.Muted;
            if (!previousButton.interactable && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == previousButton.gameObject)
                EventSystem.current.SetSelectedGameObject(null);
        }
        if (nextLabel != null)
            nextLabel.text = currentPage + 1 == pages.Count ? "Continuar  ›" : "Siguiente  ›";
    }

    // A heading starts with "# " and "---" starts the next page. Older assets
    // without either convention still show one line per page.
    private string ReadPages(string source)
    {
        string title = "Interludio";
        var page = new StringBuilder();
        using var reader = new StringReader(source);
        string line = reader.ReadLine();
        bool authored = (line != null && line.StartsWith("# ")) || source.Contains("\n---");
        if (line != null && line.StartsWith("# "))
        {
            title = line.Substring(2).Trim();
            line = reader.ReadLine();
        }

        while (line != null)
        {
            line = line.Trim();
            if (line == "---")
                AddPage(page);
            else if (line.Length > 0 || page.Length > 0)
            {
                if (line.Length > 0)
                    AppendLine(page, line);
                page.AppendLine();
                if (!authored)
                    AddPage(page);
            }
            line = reader.ReadLine();
        }
        AddPage(page);
        if (pages.Count == 0)
            pages.Add(string.Empty);
        return title;
    }

    private static void AppendLine(StringBuilder page, string line)
    {
        int separator = line.IndexOf(" — ", System.StringComparison.Ordinal);
        if (separator > 0 && separator <= 24)
        {
            page.Append("<b><color=#97EAB1>").Append(line, 0, separator).Append("</color></b>");
            page.Append(line, separator, line.Length - separator);
        }
        else
        {
            page.Append(line);
        }
    }

    private void AddPage(StringBuilder page)
    {
        string content = page.ToString().Trim();
        if (content.Length > 0)
            pages.Add(content);
        page.Clear();
    }

    private void CreateReadingLayout(string title)
    {
        var canvas = textDialogObject.canvas;
        if (canvas == null)
            return;

        var root = (RectTransform)canvas.transform;
        var displayFont = textDialogObject.font;
        var font = readingFont != null ? readingFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var panel = TvOverlay.AddPanel(root, "Dialogue reading panel", new Color32(4, 15, 13, 204));
        TvOverlay.Place(panel, 0.091f, 0.225f, 0.722f, 0.863f);
        textDialogObject.rectTransform.SetParent(panel, false);
        TvOverlay.Place(textDialogObject.rectTransform, 0.031f, 0.045f, 0.969f, 0.764f);
        TvOverlay.Format(textDialogObject, font, 32, TvOverlay.Ink, TextAnchor.UpperLeft);
        textDialogObject.lineSpacing = 1.12f;
        textDialogObject.supportRichText = true;
        textDialogObject.resizeTextForBestFit = true;
        textDialogObject.resizeTextMinSize = 24;
        textDialogObject.resizeTextMaxSize = 32;

        var label = TvOverlay.AddText(panel, "Section", font, 20, TvOverlay.Green, TextAnchor.MiddleLeft);
        TvOverlay.Place(label.rectTransform, 0.031f, 0.886f, 0.81f, 0.975f);
        label.fontStyle = FontStyle.Bold;
        label.text = nextSceneName == "MainMenu" ? "EPÍLOGO" : "INTERLUDIO";
        pageLabel = TvOverlay.AddText(panel, "Page count", font, 23, TvOverlay.Green, TextAnchor.MiddleRight);
        TvOverlay.Place(pageLabel.rectTransform, 0.81f, 0.886f, 0.969f, 0.975f);
        var heading = TvOverlay.AddText(panel, "Title", displayFont != null ? displayFont : font,
            49, TvOverlay.Ink, TextAnchor.MiddleLeft);
        TvOverlay.Place(heading.rectTransform, 0.031f, 0.792f, 0.969f, 0.897f);
        heading.text = title;
        var rule = TvOverlay.AddPanel(panel, "Title divider", new Color32(151, 234, 177, 110));
        TvOverlay.Place(rule, 0.031f, 0.787f, 0.969f, 0.790f);

        foreach (var button in canvas.GetComponentsInChildren<Button>())
        {
            var onClick = button.onClick;
            for (int i = 0; i < onClick.GetPersistentEventCount(); i++)
            {
                if (onClick.GetPersistentTarget(i) != this)
                    continue;

                switch (onClick.GetPersistentMethodName(i))
                {
                    case nameof(SetNextLine):
                        button.transform.SetParent(root, false);
                        TvOverlay.Place((RectTransform)button.transform, 0.823f, 0.765f, 0.943f, 0.833f);
                        nextLabel = TvOverlay.StyleButton(button, font, true);
                        break;
                    case nameof(SetPreviousLine):
                        previousButton = button;
                        button.transform.SetParent(root, false);
                        TvOverlay.Place((RectTransform)button.transform, 0.823f, 0.674f, 0.943f, 0.742f);
                        previousLabel = TvOverlay.StyleButton(button, font, false);
                        if (previousLabel != null)
                            previousLabel.text = "‹  Anterior";
                        break;
                }
            }
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
