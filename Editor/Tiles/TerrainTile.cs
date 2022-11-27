using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MetaBrush
{
    public class TerrainTile : Tile
    {
        private TerrainData terrainData;

        public override Matrix4x4 localToWorld
        {
            get
            {
                Vector3 size = terrainData.size;
                return Matrix4x4.TRS(gameObject.transform.position, Quaternion.identity, size);
            }
        }

        public TerrainTile(GameObject gameObject, TerrainData terrainData) : base(gameObject)
        {
            this.terrainData = terrainData;
        }
    }
}