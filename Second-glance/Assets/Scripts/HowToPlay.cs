using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

public class HowToPlay : MonoBehaviour
{
    private Button startButton;
    private Button quitButton;
    private Button buttonHowToPlay;
    private VisualElement topContainer;

    private float spriteChangeInterval = 2f;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();

        startButton = uiDocument.rootVisualElement.Q("start") as Button;
        quitButton = uiDocument.rootVisualElement.Q("quit") as Button;
        buttonHowToPlay = uiDocument.rootVisualElement.Q("how") as Button;

        startButton.RegisterCallback<ClickEvent>(evt => LoadGameScene());
        quitButton.RegisterCallback<ClickEvent>(evt => Application.Quit());
        buttonHowToPlay.RegisterCallback<ClickEvent>(ShowHowToPlay);

        topContainer = uiDocument.rootVisualElement.Q("top-container");
    }

    private void ShowHowToPlay(ClickEvent evt)
    {
        topContainer.Clear();

        // Load the text from the file
        string howToPlayText = File.ReadAllText("Assets/Resources/Files/HowToPlay.txt");

        var scrollView = new ScrollView(ScrollViewMode.Vertical)
        {
            verticalScrollerVisibility = ScrollerVisibility.Auto
        };

        var label = new Label(howToPlayText)
        {
            style = { whiteSpace = WhiteSpace.Normal }
        };

        label.AddToClassList("text-basic");
        scrollView.Add(label);
        topContainer.Add(scrollView);
    }

    private void LoadGameScene()
    {
        topContainer.Clear();

        var imageContainer = new VisualElement();
        imageContainer.AddToClassList("image-container");

        // Set the image container to fit the screen size
        imageContainer.style.width = Length.Percent(100);
        imageContainer.style.height = Length.Percent(100);

        topContainer.Add(imageContainer);

        var sprites = Resources.LoadAll<Sprite>("Sprites");
        StartCoroutine(ChangeSprite(imageContainer, sprites));

        var progressBar = new ProgressBar();
        progressBar.name = "progress-bar";
        progressBar.style.visibility = Visibility.Visible;
        progressBar.style.position = Position.Absolute;
        progressBar.style.bottom = 0;
        progressBar.style.left = 0;
        progressBar.style.right = 0;
        progressBar.value = 30; // Set initial value to 30
        topContainer.Add(progressBar);
    }

    private IEnumerator ChangeSprite(VisualElement imageContainer, Sprite[] sprites)
    {
        Dictionary<int, int> imageUsage = new Dictionary<int, int>();
        for (int i = 1; i <= 22; i++)
        {
            imageUsage[i] = 0;
        }

        var random = new System.Random();

        while (true)
        {
            var availableImages = new List<int>();
            foreach (var kvp in imageUsage)
            {
                if (kvp.Value < 2)
                {
                    availableImages.Add(kvp.Key);
                }
            }

            if (availableImages.Count == 0)
            {
               
                // All images have been used twice. Display game over on the screen.
                GameOver();
                // Stop the coroutine
                StopCoroutine(ChangeSprite(imageContainer, sprites));
                yield break;

            }

            int randomIndex = random.Next(availableImages.Count);
            int selectedImageNumber = availableImages[randomIndex];

            var sprite = System.Array.Find(sprites, s => s.name.StartsWith(selectedImageNumber.ToString()));
            Debug.Log($"Selected Image File: {sprite.name}, Usage: {imageUsage[selectedImageNumber]}");

            if (sprite != null)
            {
                imageContainer.style.backgroundImage = new StyleBackground(sprite);
                imageUsage[selectedImageNumber]++;
            }

            yield return new WaitForSeconds(spriteChangeInterval);
        }

    }
    private void GameOver()
    {
        topContainer.Clear();
        var gameOverLabel = new Label("Game Over");
        gameOverLabel.AddToClassList("game-over");
        topContainer.Add(gameOverLabel);
    }
    private void OnDisable()
    {
        startButton.UnregisterCallback<ClickEvent>(evt => LoadGameScene());
        quitButton.UnregisterCallback<ClickEvent>(evt => Application.Quit());
        buttonHowToPlay.UnregisterCallback<ClickEvent>(ShowHowToPlay);
    }
}