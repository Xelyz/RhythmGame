using UnityEngine;
using UnityEngine.Pool;

public class NotePool : MonoBehaviour
{
    public GameObject tapPrefab;
    public GameObject dragPrefab;

    ObjectPool<GameObject> tapPool;
    ObjectPool<GameObject> dragPool;

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

        dragPool = new(() =>
        {
            return Instantiate(dragPrefab);
        }, note =>
        {
            note.SetActive(true);
        }, note =>
        {
            note.SetActive(false);
        }, note =>
        {
            Destroy(note);
        }, false, 15, 30);
    }

    public ObjectPool<GameObject> GetPool(NoteType type)
    {
        return type switch
        {
            NoteType.Tap => tapPool,
            NoteType.Drag => dragPool,
            _ => null,
        };
    }

    public GameObject Get(NoteType type)
    {
        return type switch
        {
            NoteType.Tap => tapPool.Get(),
            NoteType.Drag => dragPool.Get(),
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
            case NoteType.Drag:
                dragPool.Release(note);
                break;
        }
    }
}
