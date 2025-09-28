using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EditorGridVisualizer : MonoBehaviour
{
    [Header("Grid Settings")]
    public RectTransform gridContainer;
    public Color normalCellColor = new Color(1f, 1f, 1f, 0.2f);
    public Color highlightCellColor = new Color(1f, 1f, 0f, 0.3f);
    
    [Header("Note Display")]
    public GameObject tapNotePrefab;
    public GameObject dragNotePrefab;
    public GameObject blockNotePrefab;
    
    private GridCell[,] gridCells;
    private Dictionary<Vector2Int, GameObject> displayedNotes = new Dictionary<Vector2Int, GameObject>();
    
    public struct GridCell
    {
        public GameObject cellObject;
        public Image cellImage;
        public Vector2Int gridPosition;
    }
    
    void Start()
    {
        CreateGrid();
    }
    
    void CreateGrid()
    {
        if (gridContainer == null) return;
        
        gridCells = new GridCell[Values.gridColumns, Values.gridRows];
        
        // 计算网格大小
        RectTransform containerRect = gridContainer.GetComponent<RectTransform>();
        float containerWidth = containerRect.rect.width;
        float containerHeight = containerRect.rect.height;
        
        float cellWidth = containerWidth / Values.gridColumns;
        float cellHeight = containerHeight / Values.gridRows;
        
        // 创建网格单元格
        for (int x = 0; x < Values.gridColumns; x++)
        {
            for (int y = 0; y < Values.gridRows; y++)
            {
                // 创建网格单元格
                GameObject cell = new GameObject($"GridCell_{x}_{y}");
                cell.transform.SetParent(gridContainer, false);
                
                // 添加RectTransform和Image
                RectTransform cellRect = cell.AddComponent<RectTransform>();
                Image cellImage = cell.AddComponent<Image>();
                
                // 设置尺寸和位置
                cellRect.sizeDelta = new Vector2(cellWidth, cellHeight);
                cellRect.anchoredPosition = new Vector2(
                    (x - (Values.gridColumns - 1) * 0.5f) * cellWidth,
                    ((Values.gridRows - 1) * 0.5f - y) * cellHeight
                );
                
                // 设置样式
                cellImage.color = normalCellColor;
                
                // 存储网格单元格信息
                gridCells[x, y] = new GridCell
                {
                    cellObject = cell,
                    cellImage = cellImage,
                    gridPosition = new Vector2Int(x, y)
                };
                
                Debug.Log($"Created grid cell at ({x},{y}) with position {cellRect.anchoredPosition}");
            }
        }
    }
    
    public void HighlightCell(Vector2Int gridPos)
    {
        if (IsValidGridPosition(gridPos))
        {
            gridCells[gridPos.x, gridPos.y].cellImage.color = highlightCellColor;
        }
    }
    
    public void ResetCellHighlight(Vector2Int gridPos)
    {
        if (IsValidGridPosition(gridPos))
        {
            gridCells[gridPos.x, gridPos.y].cellImage.color = normalCellColor;
        }
    }
    
    public void ResetAllHighlights()
    {
        for (int x = 0; x < Values.gridColumns; x++)
        {
            for (int y = 0; y < Values.gridRows; y++)
            {
                gridCells[x, y].cellImage.color = normalCellColor;
            }
        }
    }
    
    public void DisplayNote(Vector2Int gridPos, NoteType noteType)
    {
        if (!IsValidGridPosition(gridPos)) return;
        
        // 移除现有note
        RemoveNote(gridPos);
        
        // 创建新note显示
        GameObject notePrefab = GetNotePrefab(noteType);
        if (notePrefab == null) return;
        
        GameObject noteDisplay = Instantiate(notePrefab, gridContainer);
        RectTransform noteRect = noteDisplay.GetComponent<RectTransform>();
        
        if (noteRect == null)
            noteRect = noteDisplay.AddComponent<RectTransform>();
        
        // 设置note位置到对应的网格单元格
        RectTransform cellRect = gridCells[gridPos.x, gridPos.y].cellObject.GetComponent<RectTransform>();
        noteRect.anchoredPosition = cellRect.anchoredPosition;
        noteRect.sizeDelta = cellRect.sizeDelta * 0.8f; // 稍微小一点
        
        // 添加颜色区分
        Image noteImage = noteDisplay.GetComponent<Image>();
        if (noteImage == null)
            noteImage = noteDisplay.AddComponent<Image>();
        
        noteImage.color = GetNoteColor(noteType);
        
        displayedNotes[gridPos] = noteDisplay;
        
        Debug.Log($"Displayed {noteType} note at grid ({gridPos.x},{gridPos.y})");
    }
    
    public void RemoveNote(Vector2Int gridPos)
    {
        if (displayedNotes.ContainsKey(gridPos))
        {
            Destroy(displayedNotes[gridPos]);
            displayedNotes.Remove(gridPos);
            Debug.Log($"Removed note at grid ({gridPos.x},{gridPos.y})");
        }
    }
    
    public void ClearAllNotes()
    {
        foreach (var note in displayedNotes.Values)
        {
            if (note != null)
                Destroy(note);
        }
        displayedNotes.Clear();
    }
    
    public bool HasNoteAt(Vector2Int gridPos)
    {
        return displayedNotes.ContainsKey(gridPos);
    }
    
    public Vector2Int ScreenToGridPosition(Vector2 screenPosition)
    {
        // 将屏幕坐标转换为网格坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridContainer, screenPosition, null, out Vector2 localPoint);
        
        // 计算网格位置
        float cellWidth = gridContainer.rect.width / Values.gridColumns;
        float cellHeight = gridContainer.rect.height / Values.gridRows;
        
        int x = Mathf.FloorToInt((localPoint.x + gridContainer.rect.width * 0.5f) / cellWidth);
        int y = Mathf.FloorToInt((gridContainer.rect.height * 0.5f - localPoint.y) / cellHeight);
        
        return new Vector2Int(
            Mathf.Clamp(x, 0, Values.gridColumns - 1),
            Mathf.Clamp(y, 0, Values.gridRows - 1)
        );
    }
    
    private bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < Values.gridColumns && 
               gridPos.y >= 0 && gridPos.y < Values.gridRows;
    }
    
    private GameObject GetNotePrefab(NoteType noteType)
    {
        return noteType switch
        {
            NoteType.Tap => tapNotePrefab,
            NoteType.Drag => dragNotePrefab,
            NoteType.Block => blockNotePrefab,
            _ => tapNotePrefab
        };
    }
    
    private Color GetNoteColor(NoteType noteType)
    {
        return noteType switch
        {
            NoteType.Tap => Color.cyan,
            NoteType.Drag => Color.green,
            NoteType.Block => Color.red,
            _ => Color.white
        };
    }
}