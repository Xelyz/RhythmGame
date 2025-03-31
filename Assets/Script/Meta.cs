using System;
using System.Collections.Generic;

[Serializable]
public class Meta
{
    public string id;
    public string title;
    public string artist;
    public List<string> level = new(){"", "", ""};
    public int previewStart;
    public int previewEnd;
}