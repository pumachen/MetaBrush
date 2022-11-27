using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MetaBrush
{
    public class MeshTile : Tile
    {
        public MeshTile(GameObject gameObject, Mesh mesh) : base(gameObject)
        {
            this.mesh = mesh;
        }
    }
}