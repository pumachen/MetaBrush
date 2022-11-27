using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MetaBrush.Collections;
using MetaBrush.Utils;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MetaBrush
{
    public abstract class MetaPainter
    {
        protected abstract Shader shader { get; }
        protected Material material
        {
            get
            {
                if (m_material == null)
                {
                    m_material = new Material(shader);
                }
                return m_material;
            }
        }
        private Material m_material;

        protected Dictionary<GameObject, Tile> m_loadedTiles = new Dictionary<GameObject, Tile>();

        public IEnumerable<Tile> loadedTiles => m_loadedTiles.Values;

        public abstract IEnumerable<PainterTarget> GetPainterTargets(Tile tile);

        public virtual IEnumerable<Brush> brushes
        {
            get
            {
                yield return new AlphaBrush();
                yield return new RampedSDFBrush();
            }
        }

        public virtual void SaveTile(Tile tile)
        {
            RenderTexture prevActive = RenderTexture.active;
            foreach (var target in GetPainterTargets(tile))
            {
                RenderTexture.active = target.targetRT;
                Texture2D src = target.src;
                Texture2D buffer = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false, true);
                string path = AssetDatabase.GetAssetPath(src);
                buffer.ReadPixels(new Rect(0, 0, buffer.width, buffer.height), 0, 0);
                byte[] rawTexture = null;
                string format = Path.GetExtension(path).ToLower();
                switch (format)
                {
                    case ".tga":
                        rawTexture = buffer.EncodeToTGA();
                        break;
                    case ".png":
                        rawTexture = buffer.EncodeToPNG();
                        break;
                    case ".jpg":
                        rawTexture = buffer.EncodeToJPG();
                        break;
                    case ".exr":
                        rawTexture = buffer.EncodeToEXR();
                        break;
                }

                if (rawTexture != null)
                    File.WriteAllBytes(path, rawTexture);
                Object.DestroyImmediate(buffer);
            }

            RenderTexture.active = prevActive;
            tile.SetDirty(false);
            AssetDatabase.Refresh();
        }

        public virtual void OnEnable()
        {
            OperationStash.Clear();
        }

        public virtual void OnDisable()
        {
            var dirtyTiles = m_loadedTiles.Values.Where(tile => tile.isDirty);
            if (dirtyTiles.Count() > 0 && EditorUtility.DisplayDialog("Save Tiles", "Save Tiles?", "Yes", "Cancel"))
            {
                foreach (var tile in dirtyTiles)
                {
                    SaveTile(tile);
                }
            }
            OperationStash.Clear();
        }

        public abstract void OnPaletteGUI();

        public abstract bool TileFilter(GameObject gameObject);
        public abstract void UpdateTile(Tile tile);
    }
}