using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using MetaBrush.Collections;
using MetaBrush.Utils;
using UnityEngine.Experimental.Rendering;

namespace MetaBrush
{
    public abstract class Tile
    {
        public virtual GameObject gameObject { get; protected set; }
        public virtual Matrix4x4 localToWorld { get => gameObject.transform.localToWorldMatrix; }
        public virtual Mesh mesh { get; protected set; }

        public bool isDirty { get; protected set; }

        protected Tile(GameObject gameObject)
        {
            this.gameObject = gameObject;
        }

        public RenderTexture brushMask = new RenderTexture(512, 512, 32, RenderTextureFormat.R8)
        { 
            name = "Brush Mask", 
            wrapMode = TextureWrapMode.Clamp
        };

        public static Tile CreateTile(GameObject gameObject)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                return new MeshTile(gameObject, meshFilter.sharedMesh);
            }

            Terrain terrain = gameObject.GetComponent<Terrain>();
            if (terrain != null)
            {
                return new TerrainTile(gameObject, terrain.terrainData);
            }

            return null;
        }

        public void SetDirty(bool isDirty = true)
        {
            this.isDirty = isDirty;
        }
    }
}