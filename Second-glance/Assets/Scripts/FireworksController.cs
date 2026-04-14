using UnityEngine;
using System.Collections;

public class FireworksController : MonoBehaviour
{
    public static FireworksController Instance;

    public GameObject fireworkPrefab;
    public AudioClip fireworkPopSound;
    public int burstCount = 10;
    public float delayBetweenBursts = 0.2f;
    public Vector2 spawnAreaMin = new Vector2(-6, -4);
    public Vector2 spawnAreaMax = new Vector2(6, 4);

    private AudioSource audioSource;

    private void Awake()
    {
        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void PlayFireworks()
    {
        if (this == null) return;
        StartCoroutine(FireworksRoutine());
    }

    private IEnumerator FireworksRoutine()
    {
        Color[] festiveColors = new Color[] {
            new Color(1f, 0.2f, 0.2f),   // Red
            new Color(0.2f, 1f, 0.5f),   // Emerald
            new Color(0.2f, 0.5f, 1f),   // Sapphire
            new Color(1f, 0.84f, 0f),    // Gold
            new Color(1f, 0.2f, 1f),     // Magenta
            new Color(0f, 1f, 1f)        // Cyan
        };

        for (int i = 0; i < burstCount; i++)
        {
            Vector3 spawnPos = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                -1f
            );

            if (fireworkPrefab != null)
            {
                GameObject fw = Instantiate(fireworkPrefab, spawnPos, Quaternion.identity);
                Color randomColor = festiveColors[Random.Range(0, festiveColors.Length)];
                
                var systems = fw.GetComponentsInChildren<ParticleSystem>();
                foreach (var s in systems) {
                    var main = s.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(randomColor);
                    s.Play();
                }

                Destroy(fw, 4f);
            }
            
            if (fireworkPopSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(fireworkPopSound, 0.6f);
            }
            
            yield return new WaitForSeconds(delayBetweenBursts);
        }
    }
}
