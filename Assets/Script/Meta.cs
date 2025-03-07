using System;

[Serializable]
public class Meta
{
    public string id;
    public string title;
    public string artist;
    public string[] level = {"", "", "", ""};
    public int previewStart;
    public int previewEnd;
}
