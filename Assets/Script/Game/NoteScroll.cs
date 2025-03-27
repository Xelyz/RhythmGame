using UnityEngine;

public class NoteScroll : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.isGamePlaying)
            transform.position -= new Vector3(0, 0, Values.Preference.noteSpeed * Time.deltaTime);
    }
}
