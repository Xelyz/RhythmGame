using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioSource musicSource;
    public AudioSource sfxSource;

    public bool isAudioReady = false;

    public string sfxResourcePath = "SoundEffects";

    private Dictionary<string, AudioClip> effectClips = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource == null)
            {
                Debug.LogError("AudioManager Error: Music Source is NOT assigned in the Inspector!", this.gameObject);
            }
            if (sfxSource == null)
            {
                Debug.LogError("AudioManager Error: SFX Source is NOT assigned in the Inspector!", this.gameObject);
            }
            else
            {
                sfxSource.playOnAwake = false;
            }

            LoadAllEffectsFromResources(sfxResourcePath);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator InitGameMusic(string songFolderId)
    {
        if (musicSource == null)
        {
            Debug.LogError("AudioManager: Cannot init music, Music Source is missing!", this.gameObject);
            yield break;
        }

        isAudioReady = false;
        string path = $"songs/{songFolderId}/track";
        var request = Resources.LoadAsync<AudioClip>(path);

        while (!request.isDone)
        {
            yield return null;
        }

        if (request.asset == null)
        {
            Debug.LogError($"AudioManager: Failed to load music clip from Resources: {path}", this.gameObject);
            yield break;
        }

        AudioClip loadedClip = request.asset as AudioClip;
        if (loadedClip == null)
        {
            Debug.LogError($"AudioManager: Loaded asset from {path} is not an AudioClip.", this.gameObject);
            yield break;
        }

        musicSource.clip = loadedClip;
        isAudioReady = true;
        Debug.Log($"AudioManager: Music '{songFolderId}' loaded successfully.");
    }

    private void LoadAllEffectsFromResources(string resourceFolderPath)
    {
        effectClips = new Dictionary<string, AudioClip>();
        AudioClip[] clips = Resources.LoadAll<AudioClip>(resourceFolderPath);

        if (clips.Length == 0)
        {
            Debug.LogWarning($"AudioManager: No sound effect AudioClips found in 'Resources/{resourceFolderPath}'.", this.gameObject);
            return;
        }

        foreach (AudioClip clip in clips)
        {
            if (!effectClips.ContainsKey(clip.name))
            {
                effectClips.Add(clip.name, clip);
            }
            else
            {
                Debug.LogWarning($"AudioManager: Duplicate SFX name '{clip.name}' found in 'Resources/{resourceFolderPath}'. Only the first one loaded will be used.", this.gameObject);
            }
        }
        Debug.Log($"AudioManager: Loaded {effectClips.Count} SFX clips from 'Resources/{resourceFolderPath}'.");
    }

    public void PlayEffect(AudioClip clipToPlay, float volumeScale = 1.0f)
    {
        if (sfxSource == null)
        {
            Debug.LogError("AudioManager: Cannot play effect, SFX Source is missing!", this.gameObject);
            return;
        }
        if (clipToPlay == null)
        {
            Debug.LogWarning("AudioManager: PlayEffect(AudioClip) called with a null clip.", this.gameObject);
            return;
        }

        sfxSource.PlayOneShot(clipToPlay, Mathf.Clamp01(volumeScale));
    }

    public void PlayEffect(string clipName, float volumeScale = 1.0f)
    {
        if (sfxSource == null)
        {
            Debug.LogError("AudioManager: Cannot play effect, SFX Source is missing!", this.gameObject);
            return;
        }
        if (string.IsNullOrEmpty(clipName))
        {
            Debug.LogWarning("AudioManager: PlayEffect(string) called with an empty or null clip name.", this.gameObject);
            return;
        }

        if (effectClips.TryGetValue(clipName, out AudioClip clipToPlay))
        {
            PlayEffect(clipToPlay, volumeScale);
        }
        else
        {
            Debug.LogWarning($"AudioManager: Sound effect clip '{clipName}' not found. Was it placed in 'Resources/{sfxResourcePath}' and loaded correctly?", this.gameObject);
        }
    }

    public void ClearMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = 1f;
        }
        isAudioReady = false;
    }
}