using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongSelection : MonoBehaviour
{
    public GameObject songButtonPrefab;
    public GameObject songHolder;
    public GameObject previewUI;

    private Preview preview;
    private List<Meta> songList = new();

    void Start()
    {
        InitializeComponents();
        StartCoroutine(LoadSongs());
    }

    private void InitializeComponents()
    {
        preview = previewUI.GetComponent<Preview>();
    }

    public void StartGame()
    {
        PlayInfo.isTutorial = false;
        Util.Transition("GameScene");
    }

    public void Setting()
    {
        Util.Transition("SettingScene");
    }

    private IEnumerator LoadSongs()
    {
        var songListJson = Resources.Load<TextAsset>("SongList");
        if (songListJson == null)
        {
            Debug.LogError("未能加载歌曲列表");
            yield break;
        }

        var collection = JsonUtility.FromJson<SongCollection>(songListJson.text);
        
        // 预加载所有资源
        foreach (var id in collection.songs)
        {
            preview.PreloadResources(id);
        }

        songList = collection.songs
            .Select(id => LoadSongMeta(id))
            .Where(meta => meta != null)
            .ToList();

        Debug.Log("开始预加载所有歌曲资源...");
        List<Coroutine> preloadCoroutines = new();
        foreach (var id in collection.songs)
        {
            if (!string.IsNullOrEmpty(id)) // Basic check for valid ID
            {
                // PreloadResources should return a Coroutine
                preloadCoroutines.Add(preview.PreloadResources(id));
            }
        }

        // Wait for all preloading coroutines to finish
        foreach (var coroutine in preloadCoroutines)
        {
            yield return coroutine;
        }

        Debug.Log("所有歌曲资源预加载完成!");

        Transfer.sceneReady = true;
        ShowSelectedSong();
    }

    private Meta LoadSongMeta(string id)
    {
        var metaJson = Resources.Load<TextAsset>($"Songs/{id}/meta");
        if (metaJson == null)
        {
            Debug.LogError($"未能加载歌曲信息: {id}");
            return null;
        }

        var meta = JsonUtility.FromJson<Meta>(metaJson.text);
        meta.id = id;
        GenerateSongButton(meta);
        return meta;
    }

    private void GenerateSongButton(Meta meta)
    {
        var song = Instantiate(songButtonPrefab, songHolder.transform);
        song.GetComponentInChildren<TextMeshProUGUI>().text = meta.title;
        song.GetComponent<Button>().onClick.AddListener(() => ShowPreview(meta));
    }

    private void ShowPreview(Meta meta)
    {
        preview.UpdatePreview(meta);
    }

    private void ShowSelectedSong()
    {
        if (PlayInfo.meta != null)
            ShowPreview(PlayInfo.meta);
        else
            ShowPreview(songList[0]);
    }
}

[System.Serializable]
public class SongCollection
{
    public string[] songs;
}