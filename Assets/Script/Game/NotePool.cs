using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class NotePool : MonoBehaviour
{
    public GameObject tapPrefab;
    public GameObject slidePrefab;

    ObjectPool<GameObject> tapPool;
    ObjectPool<GameObject> slidePool;

    public static NotePool Instance;

    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        tapPool = new(() =>
        {
            return Instantiate(tapPrefab);
        }, note =>
        {
            note.SetActive(true);
        }, note =>
        {
            note.SetActive(false);
        }, note =>
        {
            Destroy(note);
        }, false, 10, 20);

        slidePool = new(() =>
        {
            return Instantiate(slidePrefab);
        }, note =>
        {
            note.SetActive(true);
        }, note =>
        {
            note.SetActive(false);
        }, note =>
        {
            Destroy(note);
        }, false, 10, 20);
    }

    public ObjectPool<GameObject> GetPool(NoteType type)
    {
        return type switch
        {
            NoteType.Tap => tapPool,
            NoteType.Slide => slidePool,
            _ => null,
        };
    }

    public GameObject Get(NoteType type)
    {
        return type switch
        {
            NoteType.Tap => tapPool.Get(),
            NoteType.Slide => slidePool.Get(),
            _ => null,
        };
    }

    public void Release(NoteType type, GameObject note)
    {
        switch (type)
        {
            case NoteType.Tap:
                tapPool.Release(note);
                break;
            case NoteType.Slide:
                slidePool.Release(note);
                break;
        }
    }
}
