using UnityEngine;
using UnityEngine.UIElements;

public class HowToPlay : MonoBehaviour
{

    private Button startButton;
    private Button quitButton;
    private Button buttonHowToPlay;
    private VisualElement mainContainer;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();

        startButton = uiDocument.rootVisualElement.Q("start") as Button;
        quitButton = uiDocument.rootVisualElement.Q("quit") as Button;
        buttonHowToPlay = uiDocument.rootVisualElement.Q("how") as Button;

        startButton.RegisterCallback<ClickEvent>(evt => LoadGameScene());
        quitButton.RegisterCallback<ClickEvent>(evt => Application.Quit());

        buttonHowToPlay.RegisterCallback<ClickEvent>(ShowHowToPlay);

        mainContainer = uiDocument.rootVisualElement.Q("main-container");
    }

    private void ShowHowToPlay(ClickEvent evt)
    {
        mainContainer.Clear();
        string howToPlayText = "1. Select <b>Play</b> to begin.\n" +
            "2. The card will change automatically after a few seconds to show a new object.\n" +
            "3. If the object on the card has appeared before, select the card to score points.\n" +
            "4. If it’s a new object, don’t click—just wait for the next card.\n" +
            "5. Gain points for clicking on repeated objects.\n" +
            "6. Lost points for clicking on new objects or missing repeated ones.\n" +
            "7. As the game continues, the card changes more quickly, making it harder to remember past objects!\n" +
            "8. The game ends when you run out of lives.";

        // Create a ScrollView
        var scrollView = new ScrollView(ScrollViewMode.Vertical);
        scrollView.style.width = 300;
        scrollView.style.height = 500;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        // Create a Label
        var label = new Label(howToPlayText);
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.color = Color.white;
        label.style.fontSize = 18;
        // Add the Label to the ScrollView
        scrollView.Add(label);
        // Add the ScrollView to the container
        mainContainer.Add(scrollView);

    }

    private void LoadGameScene()
    {
        mainContainer.Clear();
        // Create a Label
        var label = new Label("Loading Game Scene...");
        // Add the Label to the container
        mainContainer.Add(label);
    }
}
