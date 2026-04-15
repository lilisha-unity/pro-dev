using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameController : MonoBehaviour
{
    private Button startButton;
    private Button quitButton;
    private Button buttonHowToPlay;
    private VisualElement topContainer;
    private Label scoreLabel;
    private Label livesLabel;
    private Label levelLabel;

    private int score = 0;
    private int lives = 3;
    private int currentLevel = 1;
    
    private HashSet<string> seenImagesInLevel = new HashSet<string>();
    private bool clickedThisTurn = false;
    private string currentImageName = "";
    private string instructionsText = "";

    private EventCallback<ClickEvent> startAction;
    private EventCallback<ClickEvent> quitAction;
    private EventCallback<ClickEvent> howAction;

    [Header("Audio")]
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip clickSound;
    private AudioClip correctSound;
    private AudioClip penaltySound;
    private AudioClip backgroundMusic;
    private AudioClip victoryFanfare;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        startButton = root.Q<Button>("start");
        quitButton = root.Q<Button>("quit");
        buttonHowToPlay = root.Q<Button>("how");

        startAction = evt => { PlaySFX(clickSound); StartGame(); };
        quitAction = evt => { PlaySFX(clickSound); Application.Quit(); };
        howAction = evt => { PlaySFX(clickSound); ShowHowToPlay(); };

        startButton.RegisterCallback(startAction);
        quitButton.RegisterCallback(quitAction);
        buttonHowToPlay.RegisterCallback(howAction);

        topContainer = root.Q<VisualElement>("top-container");

        var sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            musicSource = sources[0];
            sfxSource = sources[1];
        }
        else
        {
            musicSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        LoadAssets();
    }

    private void LoadAssets()
    {
        clickSound = Resources.Load<AudioClip>("Audio/click");
        correctSound = Resources.Load<AudioClip>("Audio/correct");
        penaltySound = Resources.Load<AudioClip>("Audio/penalty");
        backgroundMusic = Resources.Load<AudioClip>("Audio/background_music");
        victoryFanfare = Resources.Load<AudioClip>("Audio/victory_fanfare");

        if (backgroundMusic == null) Debug.LogError("Failed to load background_music from Resources/Audio/background_music");
        if (victoryFanfare == null) Debug.LogError("Failed to load victory_fanfare from Resources/Audio/victory_fanfare");

        TextAsset instructionsAsset = Resources.Load<TextAsset>("Files/HowToPlay");
        if (instructionsAsset != null)
        {
            instructionsText = instructionsAsset.text;
        }
        else
        {
            instructionsText = "Each card is shown for a brief moment. Click the card if you have seen it before in the current level! If you miss a repeat or click a new card, you lose a life.";
        }
    }

    private void PlayBackgroundMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            if (musicSource.clip == backgroundMusic && musicSource.isPlaying) return;
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = 0.5f;
            musicSource.Play();
            Debug.Log("Started playing background music: " + backgroundMusic.name);
        }
        else
        {
            if (musicSource == null) Debug.LogWarning("PlayBackgroundMusic failed: musicSource is null");
            if (backgroundMusic == null) Debug.LogWarning("PlayBackgroundMusic failed: backgroundMusic clip is null");
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void ShowMainMenu()
    {
        if (musicSource != null) musicSource.Stop();
        StopAllCoroutines();
        ClearVisualFeedback();
        topContainer.Clear();
        
        var imageContainer = new VisualElement();
imageContainer.AddToClassList("image-container");
        
        var label = new Label("Second Glance");
        label.AddToClassList("game-name");
        
        imageContainer.Add(label);
        topContainer.Add(imageContainer);
        
        startButton.style.display = DisplayStyle.Flex;
        quitButton.style.display = DisplayStyle.Flex;
        buttonHowToPlay.style.display = DisplayStyle.Flex;
    }

    private void ShowHowToPlay()
    {
        StopAllCoroutines();
        ClearVisualFeedback();
        topContainer.Clear();

        var scrollView = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
        var label = new Label(instructionsText);
        label.AddToClassList("how-to-play-text");
        label.style.whiteSpace = WhiteSpace.Normal;

        var backButton = new Button(() => { PlaySFX(clickSound); ShowMainMenu(); }) { text = "BACK" };
backButton.AddToClassList("button");
        backButton.style.alignSelf = Align.Center;

        scrollView.Add(label);
        scrollView.Add(backButton);
        topContainer.Add(scrollView);

        startButton.style.display = DisplayStyle.None;
        quitButton.style.display = DisplayStyle.None;
        buttonHowToPlay.style.display = DisplayStyle.None;
    }

    private void StartGame()
    {
        score = 0;
        lives = 3;
        currentLevel = 1;
        LoadLevel(currentLevel);
    }

    private void LoadLevel(int level)
    {
        StopAllCoroutines();
        ClearVisualFeedback();
        topContainer.Clear();
        PlayBackgroundMusic();

        seenImagesInLevel.Clear();
        currentImageName = "";
        clickedThisTurn = false;

        // UI Setup
        var statsRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 10 } };
        scoreLabel = new Label($"Score: {score}");
        scoreLabel.AddToClassList("stats-text");
        livesLabel = new Label($"Lives: {lives}");
        livesLabel.AddToClassList("stats-text");
        levelLabel = new Label($"Level: {level}");
        levelLabel.AddToClassList("stats-text");
        
        statsRow.Add(levelLabel);
        statsRow.Add(scoreLabel);
        statsRow.Add(livesLabel);
        topContainer.Add(statsRow);

        var imageContainer = new VisualElement();
        imageContainer.AddToClassList("image-container");
        imageContainer.style.flexGrow = 1;
        imageContainer.RegisterCallback<ClickEvent>(OnImageClicked);
        topContainer.Add(imageContainer);

        var progressBar = new ProgressBar { name = "progress-bar", value = 100 };
        topContainer.Add(progressBar);

        startButton.style.display = DisplayStyle.None;
        quitButton.style.display = DisplayStyle.None;
        buttonHowToPlay.style.display = DisplayStyle.None;

        // Level Config
        int numCards = 8;
        float interval = 2.5f;
        if (level == 2) { numCards = 16; interval = 1.6f; }
        else if (level == 3) { numCards = 25; interval = 1.0f; }

        StartCoroutine(RunLevelRoutine(imageContainer, numCards, interval));
    }

    private void OnImageClicked(ClickEvent evt)
    {
        if (clickedThisTurn || string.IsNullOrEmpty(currentImageName)) return;
        clickedThisTurn = true;
        
        if (seenImagesInLevel.Contains(currentImageName))
        {
            score += 10;
            PlaySFX(correctSound);
            StartCoroutine(FlashVisualFeedback("flash-correct"));
        }
        else
        {
            lives--;
            score = Mathf.Max(0, score - 5);
            PlaySFX(penaltySound);
            StartCoroutine(FlashVisualFeedback("flash-incorrect"));
        }
        
        UpdateStats();
        if (lives <= 0) GameOver("GAME OVER", "You ran out of lives.");
    }

    private IEnumerator RunLevelRoutine(VisualElement imageContainer, int numCards, float interval)
    {
        var sprites = Resources.LoadAll<Sprite>("Sprites").ToList();
        if (sprites.Count == 0) yield break;

        // Generate a deck for this level that ensures some repeats
        List<Sprite> levelDeck = new List<Sprite>();
        System.Random rnd = new System.Random();
        
        // Strategy: take random unique sprites and repeat them
        int uniqueCount = Mathf.CeilToInt(numCards / 2f);
        var selectedUniques = sprites.OrderBy(x => rnd.Next()).Take(uniqueCount).ToList();
        
        foreach(var s in selectedUniques) {
            levelDeck.Add(s);
            levelDeck.Add(s);
        }
        // Shuffle and trim to numCards
        levelDeck = levelDeck.OrderBy(x => rnd.Next()).Take(numCards).ToList();

        var progressBar = topContainer.Q<ProgressBar>("progress-bar");

        for (int i = 0; i < levelDeck.Count; i++)
        {
            if (lives <= 0) yield break;

            Sprite sprite = levelDeck[i];
            currentImageName = sprite.name;
            clickedThisTurn = false;
            
            imageContainer.style.backgroundImage = new StyleBackground(sprite);
            imageContainer.style.opacity = 1;

            float timer = 0;
            while (timer < interval && !clickedThisTurn)
            {
                timer += Time.deltaTime;
                if (progressBar != null) progressBar.value = (1 - (timer / interval)) * 100;
                yield return null;
            }

            // Check if player missed a repeat
            if (seenImagesInLevel.Contains(currentImageName) && !clickedThisTurn)
            {
                lives--;
                score = Mathf.Max(0, score - 5);
                PlaySFX(penaltySound);
                StartCoroutine(FlashVisualFeedback("flash-incorrect"));
                UpdateStats();
                if (lives <= 0) { GameOver("GAME OVER", "A repeat was missed."); yield break; }
            }

            seenImagesInLevel.Add(currentImageName);
            imageContainer.style.opacity = 0;
            imageContainer.style.backgroundImage = null;
            yield return new WaitForSeconds(0.15f);
        }

        if (lives > 0) LevelComplete();
    }

    private void LevelComplete()
    {
        StopAllCoroutines();
        ClearVisualFeedback();
        topContainer.Clear();

        // Trigger Fireworks
        if (FireworksController.Instance != null)
        {
            FireworksController.Instance.PlayFireworks();
        }
        
        var congrats = new Label("CONGRATULATIONS!");
        congrats.AddToClassList("congratulations-text");
        topContainer.Add(congrats);
        
        var title = new Label($"LEVEL {currentLevel} COMPLETE!");
        title.AddToClassList("victory-title");
        
        var scoreInfo = new Label($"Score: {score} | Lives: {lives}");
        scoreInfo.AddToClassList("victory-subtitle");
        scoreInfo.style.marginTop = 20;

        var buttonContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.Center, marginTop = 30 } };

        if (currentLevel < 3)
        {
            var nextButton = new Button(() => { PlaySFX(clickSound); currentLevel++; LoadLevel(currentLevel); }) { text = "NEXT LEVEL" };
            nextButton.AddToClassList("button");
            buttonContainer.Add(nextButton);
        }
        else
        {
            if (musicSource != null) musicSource.Stop();
            title.text = "GAME COMPLETE!";
            PlaySFX(victoryFanfare);
        }

        var endButton = new Button(() => { PlaySFX(clickSound); ShowMainMenu(); }) { text = "END GAME" };
        endButton.AddToClassList("button");
        buttonContainer.Add(endButton);

        topContainer.Add(title);
        topContainer.Add(scoreInfo);
        topContainer.Add(buttonContainer);
    }

    private void ClearVisualFeedback()
    {
        if (topContainer == null) return;
        topContainer.RemoveFromClassList("flash-correct");
        topContainer.RemoveFromClassList("flash-incorrect");
    }

    private void UpdateStats()
    {
        if (scoreLabel != null) scoreLabel.text = $"Score: {score}";
        if (livesLabel != null) livesLabel.text = $"Lives: {lives}";
    }

    private IEnumerator FlashVisualFeedback(string className)
    {
        if (topContainer == null) yield break;
        topContainer.AddToClassList(className);
        yield return new WaitForSeconds(0.2f);
        topContainer.RemoveFromClassList(className);
    }

    private void GameOver(string title, string message)
    {
        if (musicSource != null) musicSource.Stop();
        StopAllCoroutines();
        ClearVisualFeedback();
        topContainer.Clear();
        
        var gameOverLabel = new Label(title);
        gameOverLabel.AddToClassList("game-over");
        
        var messageLabel = new Label($"{message}\nFinal Score: {score}");
        messageLabel.AddToClassList("text-basic");
        messageLabel.style.fontSize = 36;
        messageLabel.style.alignSelf = Align.Center;
        messageLabel.style.marginTop = 20;
        messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        
        var backButton = new Button(() => { PlaySFX(clickSound); ShowMainMenu(); }) { text = "RESTART" };
        backButton.AddToClassList("button");
        backButton.style.alignSelf = Align.Center;
        backButton.style.marginTop = 30;

        topContainer.Add(gameOverLabel);
        topContainer.Add(messageLabel);
        topContainer.Add(backButton);
    }

    private void OnDisable()
    {
        if (startButton != null) startButton.UnregisterCallback(startAction);
        if (quitButton != null) quitButton.UnregisterCallback(quitAction);
        if (buttonHowToPlay != null) buttonHowToPlay.UnregisterCallback(howAction);
    }
}
