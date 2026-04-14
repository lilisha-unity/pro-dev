using UnityEngine;
using System.Collections;

public class FireworksController : MonoBehaviour
{
    public static FireworksController Instance;

    public GameObject fireworkPrefab;
    public AudioClip fireworkPopSound;
    public int burstCount = 8;
    public float delayBetweenBursts = 0.25f;
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
        for (int i = 0; i < burstCount; i++)
        {
            Vector3 spawnPos = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                0
            );

            if (fireworkPrefab != null)
            {
                GameObject fw = Instantiate(fireworkPrefab, spawnPos, Quaternion.identity);
                Destroy(fw, 2f);
            }
            
            if (fireworkPopSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(fireworkPopSound, 0.4f);
            }
            
            yield return new WaitForSeconds(delayBetweenBursts);
        }
    }
}
