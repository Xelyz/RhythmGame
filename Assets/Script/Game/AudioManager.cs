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
                Debug.LogError("AudioManager Error: Music Source is NOT assigned in the Inspector!", gameObject);
            }
            if (sfxSource == null)
            {
                Debug.LogError("AudioManager Error: SFX Source is NOT assigned in the Inspector!", gameObject);
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

    /// <summary>
    /// 初始化游戏音乐（同步函数，内部启动异步加载）
    /// </summary>
    /// <param name="songFolderId">歌曲文件夹ID</param>
    public void InitGameMusic(string songFolderId)
    {
        if (musicSource == null)
        {
            Debug.LogError("AudioManager: Cannot init music, Music Source is missing!", gameObject);
            return;
        }

        // 准备工作：设置状态和停止当前播放
        isAudioReady = false;
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        musicSource.clip = null;

        // 启动异步加载协程
        StartCoroutine(LoadGameMusicAsync(songFolderId));
    }

    /// <summary>
    /// 异步加载游戏音乐的协程
    /// </summary>
    private IEnumerator LoadGameMusicAsync(string songFolderId)
    {
        string path = $"songs/{songFolderId}/track";
        var request = Resources.LoadAsync<AudioClip>(path);

        while (!request.isDone)
        {
            yield return null;
        }

        if (request.asset == null)
        {
            Debug.LogError($"AudioManager: Failed to load music clip from Resources: {path}", gameObject);
            yield break;
        }

        AudioClip loadedClip = request.asset as AudioClip;
        if (loadedClip == null)
        {
            Debug.LogError($"AudioManager: Loaded asset from {path} is not an AudioClip.", gameObject);
            yield break;
        }

        // 确保音频数据完全加载
        loadedClip.LoadAudioData();
        while (loadedClip.loadState != AudioDataLoadState.Loaded)
        {
            yield return null;
        }

        // 预缓冲音频
        musicSource.clip = loadedClip;
        
        isAudioReady = true;
        Debug.Log($"AudioManager: Music '{songFolderId}' loaded and prebuffered successfully.");
    }

    private void LoadAllEffectsFromResources(string resourceFolderPath)
    {
        effectClips = new Dictionary<string, AudioClip>();
        AudioClip[] clips = Resources.LoadAll<AudioClip>(resourceFolderPath);

        if (clips.Length == 0)
        {
            Debug.LogWarning($"AudioManager: No sound effect AudioClips found in 'Resources/{resourceFolderPath}'.", gameObject);
            return;
        }

        foreach (AudioClip clip in clips)
        {
            if (!effectClips.ContainsKey(clip.name))
            {
                effectClips.Add(clip.name, clip);
                clip.LoadAudioData();
            }
            else
            {
                Debug.LogWarning($"AudioManager: Duplicate SFX name '{clip.name}' found in 'Resources/{resourceFolderPath}'. Only the first one loaded will be used.", gameObject);
            }
        }
        Debug.Log($"AudioManager: Loaded {effectClips.Count} SFX clips from 'Resources/{resourceFolderPath}'.");
    }

    public void PlayEffect(AudioClip clipToPlay, float volumeScale = 1.0f)
    {
        if (sfxSource == null)
        {
            Debug.LogError("AudioManager: Cannot play effect, SFX Source is missing!", gameObject);
            return;
        }
        if (clipToPlay == null)
        {
            Debug.LogWarning("AudioManager: PlayEffect(AudioClip) called with a null clip.", gameObject);
            return;
        }

        sfxSource.PlayOneShot(clipToPlay, Mathf.Clamp01(volumeScale));
    }

    public void PlayEffect(string clipName, float volumeScale = 1.0f)
    {
        if (sfxSource == null)
        {
            Debug.LogError("AudioManager: Cannot play effect, SFX Source is missing!", gameObject);
            return;
        }
        if (string.IsNullOrEmpty(clipName))
        {
            Debug.LogWarning("AudioManager: PlayEffect(string) called with an empty or null clip name.", gameObject);
            return;
        }

        if (effectClips.TryGetValue(clipName, out AudioClip clipToPlay))
        {
            PlayEffect(clipToPlay, volumeScale);
        }
        else
        {
            Debug.LogWarning($"AudioManager: Sound effect clip '{clipName}' not found. Was it placed in 'Resources/{sfxResourcePath}' and loaded correctly?", gameObject);
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

    public float CurrentTime => musicSource?.time ?? 0f;

    public bool IsPlaying => musicSource != null && musicSource.isPlaying;

    public AudioClip CurrentClip => musicSource?.clip;

    public void Play()
    {
        musicSource?.Play();
    }

    public void Pause()
    {
        musicSource?.Pause();
    }

    public void UnPause()
    {
        musicSource?.UnPause();
    }

    public void Stop()
    {
        musicSource?.Stop();
    }

    public void SetTime(float time)
    {
        if (musicSource != null)
        {
            musicSource.time = time;
        }
    }

    public void SetClip(AudioClip clip)
    {
        if (musicSource != null)
        {
            musicSource.clip = clip;
        }
    }
}