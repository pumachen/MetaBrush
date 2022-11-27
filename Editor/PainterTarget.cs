using System.Collections;
using System.Collections.Generic;
using MetaBrush.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace MetaBrush
{
    public class PainterTarget
    {
        public readonly DoubleBufferedRenderTexture targetRT;
        public readonly Texture2D src;
        
        public PainterTarget(GraphicsFormat format, Texture2D src)
        {
            this.src = src;
            targetRT = new DoubleBufferedRenderTexture(src.width, src.height, 0, format);
            targetRT.Initialize(src);
        }
    }
}