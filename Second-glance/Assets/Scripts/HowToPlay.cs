using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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
    private HashSet<int> seenImages = new HashSet<int>();
    private bool clickedThisTurn = false;
    private int currentImageNumber = -1;

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
            // If already playing, don't restart
            if (musicSource.clip == backgroundMusic && musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = 0.5f;
            musicSource.Play();
            Debug.Log("Playing background music on musicSource.");
        }
        else
        {
            Debug.LogWarning($"Cannot play music: musicSource={musicSource != null}, backgroundMusic={backgroundMusic != null}");
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
        StopAllCoroutines();
        topContainer.Clear();
        
        // Ensure background music is playing
        PlayBackgroundMusic();

        score = 0;
        lives = 3;
        seenImages.Clear();

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

        // Click detection on the image container
        imageContainer.RegisterCallback<ClickEvent>(OnImageClicked);
        topContainer.Add(imageContainer);

        var sprites = Resources.LoadAll<Sprite>("Sprites");
        StartCoroutine(ChangeSprite(imageContainer, sprites));

        var progressBar = new ProgressBar();
        progressBar.name = "progress-bar";
        progressBar.style.visibility = Visibility.Visible;
        progressBar.value = 100; // Reset progress bar
        topContainer.Add(progressBar);
    }

    private void OnImageClicked(ClickEvent evt)
    {
        if (clickedThisTurn || currentImageNumber == -1) return;

        clickedThisTurn = true;
        bool isRepeat = seenImages.Contains(currentImageNumber);

        if (isRepeat)
        {
            score += 10;
            PlaySFX(correctSound);
            Debug.Log("Correct! Score: " + score);
        }
        else
        {
            lives--;
            PlaySFX(penaltySound);
            Debug.Log("Wrong! Life lost. Lives: " + lives);
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

    private IEnumerator ChangeSprite(VisualElement imageContainer, Sprite[] sprites)
    {
        Dictionary<int, int> imageUsage = new Dictionary<int, int>();
        for (int i = 1; i <= 25; i++) imageUsage[i] = 0;

        var random = new System.Random();

        while (lives > 0)
        {
            var availableImages = new List<int>();
            foreach (var kvp in imageUsage)
            {
                if (kvp.Value < 2) availableImages.Add(kvp.Key);
            }

            if (availableImages.Count == 0)
            {
                GameOver("Victory! All images cleared.");
                yield break;
            }

            int randomIndex = random.Next(availableImages.Count);
            int selectedImageNumber = availableImages[randomIndex];
            currentImageNumber = selectedImageNumber;
            clickedThisTurn = false;

            var sprite = System.Array.Find(sprites, s => s.name.StartsWith(selectedImageNumber.ToString()));
            if (sprite != null)
            {
                imageContainer.style.backgroundImage = new StyleBackground(sprite);
            }

            // Wait for interval or click
            float timer = 0;
            var progressBar = topContainer.Q<ProgressBar>("progress-bar");
            while (timer < spriteChangeInterval && !clickedThisTurn)
            {
                timer += Time.deltaTime;
                if (progressBar != null) progressBar.value = (1 - (timer / spriteChangeInterval)) * 100;
                yield return null;
            }

            // End of turn logic
            bool isRepeat = seenImages.Contains(selectedImageNumber);
            if (isRepeat && !clickedThisTurn)
            {
                lives--;
                PlaySFX(penaltySound);
                Debug.Log("Missed a repeat! Lives: " + lives);
                UpdateStats();
                if (lives <= 0)
                {
                    GameOver("Game Over - Missed too many");
                    yield break;
                }
            }

            // Record that we've seen this image
            seenImages.Add(selectedImageNumber);
            imageUsage[selectedImageNumber]++;

            // Short pause between images
            imageContainer.style.backgroundImage = null;
            yield return new WaitForSeconds(0.2f);
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

        // Reset state
        currentImageNumber = -1;
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