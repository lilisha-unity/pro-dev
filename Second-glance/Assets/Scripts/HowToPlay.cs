using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HowToPlay : MonoBehaviour
{
    private Button startButton;
    private Button quitButton;
    private Button buttonHowToPlay;
    private VisualElement topContainer;
    private Label scoreLabel;
    private Label livesLabel;

    private float spriteChangeInterval = 2f;
    private int score = 0;
    private int lives = 3;
    private HashSet<string> seenImages = new HashSet<string>();
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

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();

        startButton = uiDocument.rootVisualElement.Q("start") as Button;
        quitButton = uiDocument.rootVisualElement.Q("quit") as Button;
        buttonHowToPlay = uiDocument.rootVisualElement.Q("how") as Button;

        startAction = evt => { PlaySFX(clickSound); LoadGameScene(); };
        quitAction = evt => { PlaySFX(clickSound); Application.Quit(); };
        howAction = evt => { PlaySFX(clickSound); ShowHowToPlay(); };

        startButton.RegisterCallback(startAction);
        quitButton.RegisterCallback(quitAction);
        buttonHowToPlay.RegisterCallback(howAction);

        topContainer = uiDocument.rootVisualElement.Q("top-container");

        // Audio Setup
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
#if UNITY_EDITOR
        clickSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/click.wav");
        correctSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/correct.wav");
        penaltySound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/penalty.wav");
        backgroundMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/background_music.wav");
#endif
        // Cache instructions
        string path = "Assets/Resources/Files/HowToPlay.txt";
        if (File.Exists(path))
        {
            instructionsText = File.ReadAllText(path);
        }
        else
        {
            instructionsText = "Instructions file not found at " + path;
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
        StopAllCoroutines();
        topContainer.Clear();
        
        var imageContainer = new VisualElement();
        imageContainer.AddToClassList("image-container");
        
        var label = new Label("Second Glance");
        label.AddToClassList("game-name");
        
        imageContainer.Add(label);
        topContainer.Add(imageContainer);
        
        // Show start/how buttons if they were hidden
        startButton.style.display = DisplayStyle.Flex;
        quitButton.style.display = DisplayStyle.Flex;
        buttonHowToPlay.style.display = DisplayStyle.Flex;
    }

    private void ShowHowToPlay()
    {
        StopAllCoroutines();
        topContainer.Clear();

        var scrollView = new ScrollView(ScrollViewMode.Vertical)
        {
            verticalScrollerVisibility = ScrollerVisibility.Auto,
            style = { flexGrow = 1 }
        };

        var label = new Label(instructionsText);
        label.AddToClassList("how-to-play-text");
        label.style.whiteSpace = WhiteSpace.Normal;

        var backButton = new Button(() => { PlaySFX(clickSound); ShowMainMenu(); }) { text = "BACK" };
        backButton.AddToClassList("button");
        backButton.AddToClassList("text-basic");
        backButton.style.width = 200;
        backButton.style.height = 60;
        backButton.style.alignSelf = Align.Center;
        backButton.style.marginTop = 20;

        scrollView.Add(label);
        scrollView.Add(backButton);
        topContainer.Add(scrollView);

        // Hide main buttons while viewing instructions
        startButton.style.display = DisplayStyle.None;
        quitButton.style.display = DisplayStyle.None;
        buttonHowToPlay.style.display = DisplayStyle.None;
    }

    private void LoadGameScene()
    {
        StopAllCoroutines();
        topContainer.Clear();
        PlayBackgroundMusic();

        score = 0;
        lives = 3;
        seenImages.Clear();
        currentImageName = "";

        // Stats Container
        var statsContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 10 } };
        scoreLabel = new Label($"Score: {score}");
        scoreLabel.AddToClassList("stats-text");
        livesLabel = new Label($"Lives: {lives}");
        livesLabel.AddToClassList("stats-text");
        statsContainer.Add(scoreLabel);
        statsContainer.Add(livesLabel);
        topContainer.Add(statsContainer);

        var imageContainer = new VisualElement();
        imageContainer.AddToClassList("image-container");
        imageContainer.style.flexGrow = 1;
        imageContainer.style.opacity = 0;

        imageContainer.RegisterCallback<ClickEvent>(OnImageClicked);
        topContainer.Add(imageContainer);

        var sprites = Resources.LoadAll<Sprite>("Sprites");
        if (sprites.Length == 0) return;

        List<Sprite> gameDeck = new List<Sprite>();
        gameDeck.AddRange(sprites);
        gameDeck.AddRange(sprites);
        
        System.Random rnd = new System.Random();
        for (int i = gameDeck.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            Sprite temp = gameDeck[i];
            gameDeck[i] = gameDeck[j];
            gameDeck[j] = temp;
        }

        StartCoroutine(ChangeSprite(imageContainer, gameDeck));

        var progressBar = new ProgressBar { name = "progress-bar", value = 100 };
        topContainer.Add(progressBar);

        // Hide main buttons during game
        startButton.style.display = DisplayStyle.None;
        quitButton.style.display = DisplayStyle.None;
        buttonHowToPlay.style.display = DisplayStyle.None;
    }

    private void OnImageClicked(ClickEvent evt)
    {
        if (clickedThisTurn || string.IsNullOrEmpty(currentImageName)) return;
        clickedThisTurn = true;
        if (seenImages.Contains(currentImageName))
        {
            score += 10;
            PlaySFX(correctSound);
        }
        else
        {
            lives--;
            PlaySFX(penaltySound);
        }
        UpdateStats();
        if (lives <= 0) GameOver("Game Over - No Lives Left");
    }

    private void UpdateStats()
    {
        if (scoreLabel != null) scoreLabel.text = $"Score: {score}";
        if (livesLabel != null) livesLabel.text = $"Lives: {lives}";
    }

    private IEnumerator ChangeSprite(VisualElement imageContainer, List<Sprite> gameDeck)
    {
        foreach (Sprite sprite in gameDeck)
        {
            if (lives <= 0) yield break;
            currentImageName = sprite.name;
            clickedThisTurn = false;
            imageContainer.style.backgroundImage = new StyleBackground(sprite);
            yield return StartCoroutine(AnimateOpacity(imageContainer, 0, 1, 0.3f));

            float timer = 0;
            var progressBar = topContainer.Q<ProgressBar>("progress-bar");
            while (timer < spriteChangeInterval && !clickedThisTurn)
            {
                timer += Time.deltaTime;
                if (progressBar != null) progressBar.value = (1 - (timer / spriteChangeInterval)) * 100;
                yield return null;
            }

            if (seenImages.Contains(currentImageName) && !clickedThisTurn)
            {
                lives--;
                PlaySFX(penaltySound);
                UpdateStats();
                if (lives <= 0) { GameOver("Game Over - Missed too many"); yield break; }
            }

            seenImages.Add(currentImageName);
            yield return StartCoroutine(AnimateOpacity(imageContainer, 1, 0, 0.2f));
            imageContainer.style.backgroundImage = null;
            currentImageName = "";
            yield return new WaitForSeconds(0.1f);
        }
        if (lives > 0) GameOver("Victory! All images cleared.");
    }

    private IEnumerator AnimateOpacity(VisualElement element, float from, float to, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            element.style.opacity = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        element.style.opacity = to;
    }

    private void GameOver(string message)
    {
        if (musicSource != null) musicSource.Stop();
        StopAllCoroutines();
        topContainer.Clear();
        var gameOverLabel = new Label($"{message}\nFinal Score: {score}");
        gameOverLabel.AddToClassList("game-over");
        gameOverLabel.style.fontSize = 30;
        
        var backButton = new Button(() => { PlaySFX(clickSound); ShowMainMenu(); }) { text = "RESTART" };
        backButton.AddToClassList("button");
        backButton.AddToClassList("text-basic");
        backButton.style.width = 200;
        backButton.style.height = 60;
        backButton.style.alignSelf = Align.Center;
        backButton.style.marginTop = 20;

        topContainer.Add(gameOverLabel);
        topContainer.Add(backButton);
        currentImageName = "";
    }

    private void OnDisable()
    {
        if (startButton != null) startButton.UnregisterCallback(startAction);
        if (quitButton != null) quitButton.UnregisterCallback(quitAction);
        if (buttonHowToPlay != null) buttonHowToPlay.UnregisterCallback(howAction);
        if (musicSource != null) musicSource.Stop();
        if (sfxSource != null) sfxSource.Stop();
    }
}