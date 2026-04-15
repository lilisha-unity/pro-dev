using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
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

    private float baseSpriteChangeInterval = 2f;
    private float minSpriteChangeInterval = 0.8f;
    private float currentInterval;
    
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
    private AudioClip victoryFanfare;
    private List<AudioClip> howToPlayVOs = new List<AudioClip>();

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
        clickSound = Resources.Load<AudioClip>("Audio/click");
        correctSound = Resources.Load<AudioClip>("Audio/correct");
        penaltySound = Resources.Load<AudioClip>("Audio/penalty");
        backgroundMusic = Resources.Load<AudioClip>("Audio/background_music");
        victoryFanfare = Resources.Load<AudioClip>("Audio/victory_fanfare");
        
        howToPlayVOs.Clear();
        for (int i = 1; i <= 4; i++)
        {
            var vo = Resources.Load<AudioClip>($"Audio/how_to_play_vo_{i}");
            if (vo != null) howToPlayVOs.Add(vo);
        }

        TextAsset instructionsAsset = Resources.Load<TextAsset>("Files/HowToPlay");
    if (instructionsAsset != null)
        {
            instructionsText = instructionsAsset.text;
        }
        else
        {
            instructionsText = "Instructions file not found.";
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

        if (musicSource != null && howToPlayVOs.Count > 0)
        {
            StartCoroutine(PlayVOSequence());
        }

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

        startButton.style.display = DisplayStyle.None;
        quitButton.style.display = DisplayStyle.None;
        buttonHowToPlay.style.display = DisplayStyle.None;
    }

    private IEnumerator PlayVOSequence()
    {
        if (musicSource == null) yield break;
        musicSource.Stop();
        musicSource.loop = false;
        musicSource.volume = 1.0f;

        foreach (var clip in howToPlayVOs)
        {
            musicSource.clip = clip;
            musicSource.Play();
            while (musicSource.isPlaying) yield return null;
            yield return new WaitForSeconds(0.4f);
        }
    }

    private void LoadGameScene()
    {
        StopAllCoroutines();
        ClearVisualFeedback();
        topContainer.Clear();
        PlayBackgroundMusic();

        score = 0;
        lives = 3;
        currentInterval = baseSpriteChangeInterval;
        seenImages.Clear();
        currentImageName = "";

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

    private IEnumerator FlashVisualFeedback(string className)
    {
        if (topContainer == null) yield break;
        topContainer.AddToClassList(className);
        yield return new WaitForSeconds(0.15f);
        topContainer.RemoveFromClassList(className);
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

    private IEnumerator ChangeSprite(VisualElement imageContainer, List<Sprite> gameDeck)
    {
        int count = 0;
        foreach (Sprite sprite in gameDeck)
        {
            if (lives <= 0) yield break;
            
            count++;
            if (count % 5 == 0) currentInterval = Mathf.Max(minSpriteChangeInterval, currentInterval - 0.1f);

            currentImageName = sprite.name;
            clickedThisTurn = false;
            imageContainer.style.backgroundImage = new StyleBackground(sprite);
            yield return StartCoroutine(AnimateOpacity(imageContainer, 0, 1, 0.2f));

            float timer = 0;
            var progressBar = topContainer.Q<ProgressBar>("progress-bar");
            while (timer < currentInterval && !clickedThisTurn)
            {
                timer += Time.deltaTime;
                if (progressBar != null) progressBar.value = (1 - (timer / currentInterval)) * 100;
                yield return null;
            }

            if (seenImages.Contains(currentImageName) && !clickedThisTurn)
            {
                lives--;
                score = Mathf.Max(0, score - 5);
                PlaySFX(penaltySound);
                StartCoroutine(FlashVisualFeedback("flash-incorrect"));
                UpdateStats();
                if (lives <= 0) { GameOver("GAME OVER", "A repeat was missed."); yield break; }
            }

            seenImages.Add(currentImageName);
            yield return StartCoroutine(AnimateOpacity(imageContainer, 1, 0, 0.1f));
            imageContainer.style.backgroundImage = null;
            currentImageName = "";
            yield return new WaitForSeconds(0.1f);
        }
        if (lives > 0) Victory();
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

    private void Victory()
    {
        if (musicSource != null) musicSource.Stop();
        StopAllCoroutines();
        ClearVisualFeedback();
        topContainer.Clear();
        PlaySFX(victoryFanfare);

        var titleLabel = new Label("CONGRATULATIONS!");
        titleLabel.AddToClassList("victory-title");
        
        var subTitleLabel = new Label($"Victory Achieved with {lives} Lives Remaining!\nFinal Score: {score}");
        subTitleLabel.AddToClassList("victory-subtitle");
        subTitleLabel.AddToClassList("text-basic");

        var backButton = new Button(() => { PlaySFX(clickSound); ShowMainMenu(); }) { text = "PLAY AGAIN" };
        backButton.AddToClassList("button");
        backButton.AddToClassList("text-basic");
        backButton.style.width = 250;
        backButton.style.height = 70;
        backButton.style.alignSelf = Align.Center;
        backButton.style.marginTop = 30;

        topContainer.Add(titleLabel);
        topContainer.Add(subTitleLabel);
        topContainer.Add(backButton);

        StartCoroutine(ConfettiBurstRoutine());
    }

    private IEnumerator ConfettiBurstRoutine()
    {
        Color[] colors = { Color.yellow, Color.cyan, Color.magenta, Color.green, Color.white };
        for (int i = 0; i < 50; i++)
        {
            var confetti = new VisualElement();
            confetti.AddToClassList("confetti");
            confetti.style.backgroundColor = colors[Random.Range(0, colors.Length)];
            confetti.style.left = Length.Percent(Random.Range(0, 100));
            confetti.style.top = Length.Percent(Random.Range(0, 50));
            topContainer.Add(confetti);

            StartCoroutine(AnimateConfetti(confetti));
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator AnimateConfetti(VisualElement c)
    {
        float duration = Random.Range(1.5f, 3f);
        float elapsed = 0;
        float drift = Random.Range(-100, 100);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            c.style.top = Length.Percent(t * 100);
            c.style.left = Length.Percent(c.style.left.value.value + drift * Time.deltaTime);
            c.style.opacity = 1 - t;
            yield return null;
        }
        c.RemoveFromHierarchy();
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
        backButton.AddToClassList("text-basic");
        backButton.style.width = 250;
        backButton.style.height = 70;
        backButton.style.alignSelf = Align.Center;
        backButton.style.marginTop = 30;

        topContainer.Add(gameOverLabel);
        topContainer.Add(messageLabel);
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