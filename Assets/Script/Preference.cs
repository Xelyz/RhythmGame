using UnityEngine;

[System.Serializable]
public class Preference
{
    public float sensitivity = 3f;
    public float noteSpeed = 8f;
    public float NoteSpeed => noteSpeed / 2;
    public int offsetms = 0;
}