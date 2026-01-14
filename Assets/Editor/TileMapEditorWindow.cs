using UnityEngine;
using UnityEditor;
using Game.Systems.BattleMap;

public class TileMapEditorWindow : EditorWindow
{
    private TilemapData tilemap;

    private int selectedTileId = 0;
    private int selectedSpriteId = 0;

    private Vector2 scrollPos;

    private const int CELL_SIZE = 32;

    [MenuItem("Tools/Tilemap Editor")]
    public static void Open()
    {
        GetWindow<TileMapEditorWindow>("Tilemap Editor");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        tilemap = (TilemapData)EditorGUILayout.ObjectField("Tilemap Data", tilemap, typeof(TilemapData), false);

        if (tilemap == null)
        {
            EditorGUILayout.HelpBox("Assign a TilemapData asset.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("StartBattle Cells"))
        {
            Undo.RecordObject(tilemap, "Init Cells");
            tilemap.InitCells();
            EditorUtility.SetDirty(tilemap);
        }

        DrawPaletteSection();
        DrawGridSection();
    }

    // ----------------------------------------------------
    // PALETTE: Select TileId + spriteId
    // ----------------------------------------------------

    private void DrawPaletteSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Palette", EditorStyles.boldLabel);

        selectedTileId = EditorGUILayout.IntField("Tile ID", selectedTileId);

        EditorGUILayout.LabelField("Sprite Selection:");

        if (tilemap.sprites == null || tilemap.sprites.Count == 0)
        {
            EditorGUILayout.HelpBox("No sprites assigned in the TilemapData.", MessageType.Warning);
            return;
        }

        // show sprite list
        selectedSpriteId = GUILayout.SelectionGrid(
            selectedSpriteId,
            GetSpritePreviews(),
            8,
            GUILayout.Height(80)
        );

        if (selectedSpriteId < 0) selectedSpriteId = 0;
        if (selectedSpriteId >= tilemap.sprites.Count) selectedSpriteId = tilemap.sprites.Count - 1;
    }

    private Texture2D[] GetSpritePreviews()
    {
        Texture2D[] previews = new Texture2D[tilemap.sprites.Count];

        for (int i = 0; i < tilemap.sprites.Count; i++)
        {
            previews[i] = AssetPreview.GetAssetPreview(tilemap.sprites[i]);
        }

        return previews;
    }

    // ----------------------------------------------------
    // GRID PAINTER
    // ----------------------------------------------------

    private void DrawGridSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tilemap Grid", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        Rect gridRect = GUILayoutUtility.GetRect(
            tilemap.width * CELL_SIZE,
            tilemap.height * CELL_SIZE,
            GUILayout.ExpandWidth(false),
            GUILayout.ExpandHeight(false)
        );

        Handles.BeginGUI();

        for (int y = 0; y < tilemap.height; y++)
        {
            for (int x = 0; x < tilemap.width; x++)
            {
                int index = tilemap.Index(x, y);
                var cell = tilemap.cells[index];

                Rect cellRect = new Rect(
                    gridRect.x + x * CELL_SIZE,
                    gridRect.y + y * CELL_SIZE,
                    CELL_SIZE,
                    CELL_SIZE
                );

                // draw sprite
                if (cell.spriteId >= 0 &&
                    cell.spriteId < tilemap.sprites.Count &&
                    tilemap.sprites[cell.spriteId] != null)
                {
                    Sprite sp = tilemap.sprites[cell.spriteId];
                    Texture2D tex = AssetPreview.GetAssetPreview(sp);
                    GUI.DrawTexture(cellRect, tex, ScaleMode.ScaleToFit);
                }

                // draw border
                Handles.color = Color.gray;
                Handles.DrawLine(
                    new Vector2(cellRect.x, cellRect.y),
                    new Vector2(cellRect.x + CELL_SIZE, cellRect.y));
                Handles.DrawLine(
                    new Vector2(cellRect.x, cellRect.y),
                    new Vector2(cellRect.x, cellRect.y + CELL_SIZE));
            }
        }

        Handles.EndGUI();

        ProcessMouse(gridRect);

        EditorGUILayout.EndScrollView();
    }

    // ----------------------------------------------------
    // MOUSE PAINTING
    // ----------------------------------------------------

    private void ProcessMouse(Rect gridRect)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
        {
            if (!gridRect.Contains(e.mousePosition)) return;

            int x = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / CELL_SIZE);
            int y = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / CELL_SIZE);

            if (x < 0 || x >= tilemap.width) return;
            if (y < 0 || y >= tilemap.height) return;

            Undo.RecordObject(tilemap, "Paint Tile");

            tilemap.SetCell(x, y, new TilemapData.TileCellData
            {
                tileId = selectedTileId,
                spriteId = selectedSpriteId
            });

            EditorUtility.SetDirty(tilemap);

            Repaint();
        }
    }
}
