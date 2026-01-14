using UnityEngine;
using UnityEditor;
using Game.Systems.BattleMap;

[CustomEditor(typeof(TilemapData))]
public class TileMapDataEditor : Editor
{
    TilemapData data;
    int selectedSprite = 0;

    const int CELL_SIZE = 32;

    private void OnEnable()
    {
        data = (TilemapData)target;

        if (data.cells == null || data.cells.Length != data.height)
        {
            InitializeCells();
        }
    }

    void InitializeCells()
    {
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Resize Grid"))
        {
            InitializeCells();
        }

        EditorGUILayout.Space();

        DrawSpritePalette();
        EditorGUILayout.Space();
        DrawTileGrid();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }
    }

    void DrawSpritePalette()
    {
        GUILayout.Label("Sprite Palette", EditorStyles.boldLabel);

        if (data.sprites == null) return;

        int columns = 8;
        int rows = Mathf.CeilToInt(data.sprites.Count / (float)columns);

        for (int y = 0; y < rows; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < columns; x++)
            {
                int index = y * columns + x;
                if (index >= data.sprites.Count) break;

                GUIStyle style = new GUIStyle(GUI.skin.button);
                style.fixedWidth = CELL_SIZE;
                style.fixedHeight = CELL_SIZE;

                if (GUILayout.Button(data.sprites[index].texture, style))
                {
                    selectedSprite = index;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    void DrawTileGrid()
    {
       
    }
}
