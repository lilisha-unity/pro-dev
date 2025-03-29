using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class HowToPlay : MonoBehaviour
{
    private Button startButton;
    private Button quitButton;
    private Button buttonHowToPlay;
    private VisualElement topContainer;

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

        string howToPlayText = "1. Select <b>Start</b> to begin.\n" +
            "2. The card will change automatically after a few seconds to show a new object.\n" +
            "3. If the object on the card has appeared before, select the card to score points.\n" +
            "4. If it’s a new object, don’t click — just wait for the next card.\n" +
            "5. Gain points for clicking on repeated objects.\n" +
            "6. Lost points for clicking on new objects or missing repeated ones.\n" +
            "7. As the game continues, the card changes more quickly, making it harder to remember past objects!\n" +
            "8. The game ends when you run out of lives.";

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

    private float spriteChangeInterval = 2f;

    private IEnumerator ChangeSprite(VisualElement imageContainer, Sprite[] sprites)
    {
        Dictionary<int, int> imageUsage = new Dictionary<int, int>();
        for (int i = 1; i <= 21; i++)
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
                yield break;
            }

            int randomIndex = random.Next(availableImages.Count);
            int selectedImageNumber = availableImages[randomIndex];

            var sprite = System.Array.Find(sprites, s => s.name.StartsWith(selectedImageNumber.ToString()));
            Debug.Log($"Selected Image: {selectedImageNumber}, Usage: {imageUsage[selectedImageNumber]}");

            if (sprite != null)
            {
                imageContainer.style.backgroundImage = new StyleBackground(sprite);
                imageUsage[selectedImageNumber]++;
            }

            yield return new WaitForSeconds(spriteChangeInterval);
        }
    }
}
