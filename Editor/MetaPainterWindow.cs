using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MetaBrush.Collections;
using UnityEngine;
using UnityEditor;
using MetaBrush.Utils;

namespace MetaBrush
{
    public class MetaPainterWindow : EditorWindow
    {
        private MetaPainter painter;

        bool brushExpanded = true;
        bool paletteExpanded = true;
        bool historyExpanded = true;
        bool tileStatusExpanded = true;
        bool tileListExpanded = false;
        int brushIdx = 0;
        Vector2 scrollPos = Vector2.zero;
        
        Vector3 prevPos;
        Vector3 prevNorm;
        float prevPressure;
        bool isPrevHover = false;

        private RandomAccessCollection<Brush> m_brushes;
        IRandomAccessEnumerable<int, Brush> brushes
        {
            get
            {
                if (m_brushes == null)
                {
                    m_brushes = new RandomAccessCollection<Brush>(painter.brushes.ToArray());
                }
                return m_brushes;
            }
        }

        bool isDrawing
        {
            get
            {
                Event e = Event.current;
                return e.isMouse && e.button == 0 && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag);
            }
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            painter.OnEnable();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            painter.OnDisable();
        }
        
        void OnGUI()
        {
            Shortcut();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Bold,
            };
            brushExpanded = EditorGUILayout.Foldout(brushExpanded, "======Brush======", foldoutStyle);
            if (brushExpanded)
            {
                BrushToolbarLayout();
            }

            paletteExpanded = EditorGUILayout.Foldout(paletteExpanded, "======Palette======", foldoutStyle);
            if (paletteExpanded)
            {
                painter.OnPaletteGUI();
            }

            historyExpanded = EditorGUILayout.Foldout(historyExpanded, "======History======", foldoutStyle);
            if (historyExpanded)
            {
                UndoRedoLayout();
            }

            tileStatusExpanded = EditorGUILayout.Foldout(tileStatusExpanded, "======Tile Status======", foldoutStyle);
            if (tileStatusExpanded)
            {
                TileStatusLayout();
            }

            EditorGUILayout.EndScrollView();
        }

        void OnSceneGUI(SceneView sceneView)
        {
            Event evnt = Event.current;
            if (!Shortcut())
            {
                return;
            }

            Vector2 mousePos = evnt.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

            if (evnt.isMouse && evnt.button == 0)
            {
                if (evnt.type == EventType.MouseDown || OperationStash.index == null)
                    OperationStash.BeginOperation();
                if (evnt.type == EventType.MouseUp)
                    OperationStash.EndOperation();
            }

            var brush = brushes[brushIdx];
            RaycastHit hit;
            Brush.DeltaDrag deltaDrag;
            if (Raycast(ray, out hit))
            {
                Vector3 curPos = hit.point;
                Vector3 curNorm = hit.normal;
                float curPressure = evnt.pressure;
                if (!isPrevHover)
                {
                    prevPos = curPos;
                    prevPressure = curPressure;
                    deltaDrag = new Brush.DeltaDrag(
                        curPos,
                        curNorm,
                        curPressure,
                        prevPos,
                        prevNorm,
                        prevPressure);
                }
                else
                {
                    deltaDrag = new Brush.DeltaDrag(
                        curPos,
                        curNorm,
                        curPressure,
                        prevPos,
                        prevNorm,
                        prevPressure);
                    prevPos = curPos;
                    prevPressure = curPressure;
                }

                isPrevHover = true;

                foreach (var collider in Physics.OverlapSphere(curPos, Brush.radius).Where(c => painter.TileFilter(c.gameObject)))
                {
                    var tile = Tile.CreateTile(collider.gameObject);
                    if (isDrawing)
                    {
                        brush.UpdateMask(deltaDrag, tile, false);
                        painter.Record(tile);
                        painter.UpdateTile(tile);
                    }
                    else
                    {
                        brush.UpdateMask(deltaDrag, tile, true);
                    }

                    DrawPreview(tile);
                }
            }
            else
            {
                isPrevHover = false;
            }

            HandleUtility.Repaint();
        }

        void UndoRedoLayout()
        {
            GUILayout.TextArea(OperationStash.status);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Undo"))
            {
                painter.Undo();
            }
            if(GUILayout.Button("Redo"))
            {
                painter.Redo();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        void BrushToolbarLayout()
        {
            EditorGUILayout.BeginVertical();
            brushIdx = GUILayout.Toolbar(brushIdx, painter.brushes.Select(brush => brush.title).ToArray());
            brushes[brushIdx].BrushGUILayout(position.width);
            brushOutline = EditorGUILayout.Toggle("Brush Outline", brushOutline);
            EditorGUILayout.EndVertical();
        }
        
        protected virtual void TileStatusLayout()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                Selection.objects = painter.loadedTiles
                    .Select(tile => tile.gameObject)
                    .ToArray();
            }

            if (GUILayout.Button("Select Modified"))
            {
                Selection.objects = painter.loadedTiles
                    .Where(tile => tile.isDirty)
                    .Select(tile => tile.gameObject)
                    .ToArray();
            }

            if (GUILayout.Button("Deselect All"))
            {
                Selection.objects = new Object[] { };
            }

            EditorGUILayout.EndHorizontal();
            GameObject[] selectedGameObjects = Selection.gameObjects;
            tileListExpanded = EditorGUILayout.Foldout(tileListExpanded, "Tiles");
            if (tileListExpanded)
            {
                Color contentColor = GUI.contentColor;
                Color modifiedColor = new Color(1, 0.5f, 0);
                Color savedColor = Color.green;
                foreach (var tile in painter.loadedTiles)
                {
                    bool selected = selectedGameObjects.Contains(tile.gameObject);
                    GUI.contentColor = tile.isDirty ? modifiedColor : savedColor;
                    bool toggle = EditorGUILayout.ToggleLeft($"{tile.gameObject.name}", selected);
                    if (toggle != selected)
                    {
                        if (toggle)
                            SelectionUtility.Add(tile.gameObject);
                        else
                            SelectionUtility.Remove(tile.gameObject);
                    }
                }

                GUI.contentColor = contentColor;
            }

            if (GUILayout.Button("Save Selected"))
            {
                foreach (var tile in painter.loadedTiles.Where(t => selectedGameObjects.Contains(t.gameObject)))
                {
                    painter.SaveTile(tile);
                }
            }
        }

        bool Raycast(Ray ray, out RaycastHit hit)
        {
            var hits = Physics.RaycastAll(ray)
                .Where(h => painter.TileFilter(h.collider.gameObject))
                .OrderBy(h => h.distance);
            if (hits.Count() != 0)
            {
                hit = hits.First();
                return true;
            }
            else
            {
                hit = default(RaycastHit);
                return false;
            }
        }

        public virtual bool Shortcut()
        {
            Event evnt = Event.current;
            if (!evnt.alt)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }

            switch (evnt.keyCode)
            {
                case KeyCode.LeftBracket:
                {
                    if (evnt.shift && evnt.type == EventType.KeyDown)
                    {
                        Brush.strength = Mathf.Clamp01(Brush.strength - 0.05f);
                    }
                    else
                    {
                        Brush.radius *= 0.95f;
                    }

                    break;
                }
                case KeyCode.RightBracket:
                {
                    if (evnt.shift && evnt.type == EventType.KeyDown)
                    {
                        Brush.strength = Mathf.Clamp01(Brush.strength + 0.05f);
                    }
                    else
                    {
                        Brush.radius *= 1.05f;
                    }

                    break;
                }
                case KeyCode.Z:
                {
                    if (evnt.rawType == EventType.KeyDown)
                    {
                        painter.Undo();
                    }

                    break;
                }
                case KeyCode.Y:
                {
                    if (evnt.rawType == EventType.KeyDown)
                    {
                        painter.Redo();
                    }

                    break;
                }
            }

            return !evnt.alt;
        }

        static Material previewMat
        {
            get
            {
                if (m_previewMat == null)
                    m_previewMat = new Material(Shader.Find("Hidden/OTP/BrushPreview"));
                return m_previewMat;
            }
        }
        static Material m_previewMat = null;
        bool brushOutline = false;
        void DrawPreview(Tile tile)
        {
            previewMat.SetTexture("_BrushMask", tile.brushMask);
            previewMat.SetPass(0);
            Graphics.DrawMeshNow(tile.mesh, tile.localToWorld, 0);
            if(brushOutline)
            {
                previewMat.SetPass(1);
                Graphics.DrawMeshNow(tile.mesh, tile.localToWorld, 0);
            }
        }
    }
}