using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    internal AudioSource musicSource;
    internal AudioSource effectSource;

    public bool isAudioReady = false;

    public Dictionary<string, AudioClip> effectClips;

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        musicSource = GetComponent<AudioSource>();
    }

    public IEnumerator InitGameMusic(string id)
    {
        var request = Resources.LoadAsync<AudioClip>($"songs/{id}/track");

        while (!request.isDone)
        {
            yield return null;
        }

        if (request.asset == null)
        {
            Debug.LogError($"未能加载音频文件: {id}");
            yield break;
        }

        musicSource.clip = request.asset as AudioClip;
        musicSource.clip.LoadAudioData();
        isAudioReady = true;
    }

    public void Clear()
    {
        musicSource.clip = null;
        musicSource.Stop();
        musicSource.volume = 1f;
        isAudioReady = false;
    }
}
