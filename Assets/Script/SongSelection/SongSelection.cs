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
    public Button startButton;
    public Button settingButton;

    private Preview preview;
    private List<Meta> songList = new();

    void Start()
    {
        InitializeComponents();
        LoadSongs();
        ShowFirstSong();
    }

    private void InitializeComponents()
    {
        preview = previewUI.GetComponent<Preview>();
        startButton.onClick.AddListener(() => Util.Transition("GameScene"));
        settingButton.onClick.AddListener(() => Util.Transition("SettingScene"));
    }

    private void LoadSongs()
    {
        var songListJson = Resources.Load<TextAsset>("SongList");
        if (songListJson == null)
        {
            Debug.LogError("未能加载歌曲列表");
            return;
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

    private void ShowFirstSong()
    {
        if (songList.Count > 0)
        {
            ShowPreview(songList[0]);
        }
    }
}

[System.Serializable]
public class SongCollection
{
    public string[] songs;
}