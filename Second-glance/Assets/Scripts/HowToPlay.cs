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
        howAction = evt => { PlaySFX(clickSound); ShowHowToPlay(evt); };

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
        else if (sources.Length == 1)
        {
            musicSource = sources[0];
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        LoadAudioAssets();
    }

    private void LoadAudioAssets()
    {
#if UNITY_EDITOR
        clickSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/click.wav");
        correctSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/correct.wav");
        penaltySound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/penalty.wav");
        backgroundMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/background_music.wav");
#endif
    }

    private void PlayBackgroundMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            if (musicSource.clip == backgroundMusic && musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = 0.5f;
            musicSource.Play();
            Debug.Log("Playing background music.");
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void ShowHowToPlay(ClickEvent evt)
    {
        StopAllCoroutines();
        topContainer.Clear();

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

        imageContainer.RegisterCallback<ClickEvent>(OnImageClicked);
        topContainer.Add(imageContainer);

        var sprites = Resources.LoadAll<Sprite>("Sprites");
        if (sprites.Length == 0)
        {
            Debug.LogError("No sprites found in Resources/Sprites!");
            return;
        }

        // Create a deck with 2 of each sprite and shuffle it
        List<Sprite> gameDeck = new List<Sprite>();
        gameDeck.AddRange(sprites);
        gameDeck.AddRange(sprites);
        
        // Fisher-Yates shuffle
        System.Random rnd = new System.Random();
        for (int i = gameDeck.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            Sprite temp = gameDeck[i];
            gameDeck[i] = gameDeck[j];
            gameDeck[j] = temp;
        }

        StartCoroutine(ChangeSprite(imageContainer, gameDeck));

        var progressBar = new ProgressBar();
        progressBar.name = "progress-bar";
        progressBar.style.visibility = Visibility.Visible;
        progressBar.value = 100;
        topContainer.Add(progressBar);
    }

    private void OnImageClicked(ClickEvent evt)
    {
        if (clickedThisTurn || string.IsNullOrEmpty(currentImageName)) return;

        clickedThisTurn = true;
        bool isRepeat = seenImages.Contains(currentImageName);

        if (isRepeat)
        {
            score += 10;
            PlaySFX(correctSound);
            Debug.Log($"Correct! {currentImageName} was a repeat. Score: {score}");
        }
        else
        {
            lives--;
            PlaySFX(penaltySound);
            Debug.Log($"Wrong! {currentImageName} was new. Lives: {lives}");
        }

        UpdateStats();

        if (lives <= 0)
        {
            GameOver("Game Over - No Lives Left");
        }
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

            float timer = 0;
            var progressBar = topContainer.Q<ProgressBar>("progress-bar");
            while (timer < spriteChangeInterval && !clickedThisTurn)
            {
                timer += Time.deltaTime;
                if (progressBar != null) progressBar.value = (1 - (timer / spriteChangeInterval)) * 100;
                yield return null;
            }

            // End of turn logic
            bool isRepeat = seenImages.Contains(currentImageName);
            
            // Penalty if user missed a repeat
            if (isRepeat && !clickedThisTurn)
            {
                lives--;
                PlaySFX(penaltySound);
                Debug.Log($"Missed a repeat of {currentImageName}! Lives: {lives}");
                UpdateStats();
                if (lives <= 0)
                {
                    GameOver("Game Over - Missed too many");
                    yield break;
                }
            }

            // After showing it, add to seenImages
            seenImages.Add(currentImageName);

            // Short pause between images
            imageContainer.style.backgroundImage = null;
            currentImageName = "";
            yield return new WaitForSeconds(0.2f);
        }

        if (lives > 0)
        {
            GameOver("Victory! All images cleared.");
        }
    }

    private void GameOver(string message)
    {
        if (musicSource != null) musicSource.Stop();
        StopAllCoroutines();
        topContainer.Clear();
        var gameOverLabel = new Label($"{message}\nFinal Score: {score}");
        gameOverLabel.AddToClassList("game-over");
        gameOverLabel.style.fontSize = 30;
        topContainer.Add(gameOverLabel);

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