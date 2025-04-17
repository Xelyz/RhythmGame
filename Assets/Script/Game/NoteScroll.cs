using UnityEngine;

public class NoteScroll : MonoBehaviour
{
    public bool isActive = true;

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.isGamePlaying && isActive)
            transform.position -= new Vector3(0, 0, Values.Preference.noteSpeed * Time.deltaTime);
    }
}
