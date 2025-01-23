using UnityEngine;
using UnityEngine.UIElements;

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

        // Create a ScrollView
        var scrollView = new ScrollView(ScrollViewMode.Vertical);
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        // Create a Label
        var label = new Label(howToPlayText);
        label.style.whiteSpace = WhiteSpace.Normal;

        // Add a class selector to the Label
        label.AddToClassList("text-basic");
        // Add the Label to the ScrollView
        scrollView.Add(label);
        // Add the ScrollView to the top-container
        topContainer.Add(scrollView);

    }

    private void LoadGameScene()
    {
        topContainer.Clear();
        // Create a Label
        var labelStart = new Label("Loading Game Scene...");
        labelStart.AddToClassList("text-basic");
        // Add the Label to the container
        topContainer.Add(labelStart);
    }
}
