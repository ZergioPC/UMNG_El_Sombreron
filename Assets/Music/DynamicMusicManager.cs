
using UnityEngine;
using System.Collections;

public class DynamicMusicManager : MonoBehaviour
{
    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip normalMusic;
    public AudioClip activatedMusic;

    [Header("Settings")]
    public float fadeDuration = 1f;

    private Coroutine transition;

    private void Start()
    {
        musicSource.clip = normalMusic;
        musicSource.loop = true;
        musicSource.volume = 1f;
        musicSource.Play();
    }

    public void SetFeatureActive(bool isActive)
    {
        AudioClip targetMusic = isActive
            ? activatedMusic
            : normalMusic;

        if (transition != null)
            StopCoroutine(transition);

        transition = StartCoroutine(ChangeMusic(targetMusic));
    }

    private IEnumerator ChangeMusic(AudioClip newMusic)
    {
        // Fade out
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(
                startVolume, 0f, elapsed / fadeDuration
            );
            yield return null;
        }

        musicSource.volume = 0f;

        // Change music
        musicSource.clip = newMusic;
        musicSource.Play();

        // Fade in
        elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(
                0f, 1f, elapsed / fadeDuration
            );
            yield return null;
        }

        musicSource.volume = 1f;
        transition = null;
    }
}
