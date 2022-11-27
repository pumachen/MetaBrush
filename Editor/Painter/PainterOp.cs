using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MetaBrush
{
    public class PainterOp : IDisposable
    {
        Dictionary<Tile, (PainterTarget, RenderTexture)[]> tileStashes =
            new Dictionary<Tile, (PainterTarget, RenderTexture)[]>();

        public IEnumerable<Tile> tiles => tileStashes.Keys;

        public void Stash(MetaPainter painter, Tile tile)
        {
            if (tileStashes.ContainsKey(tile))
            {
                return;
            }

            (PainterTarget, RenderTexture)[] stash = painter.GetPainterTargets(tile).Select((target) =>
            {
                RenderTexture frontBuffer = target.targetRT.frontBuffer;
                RenderTexture stashRT = RenderTexture.GetTemporary(frontBuffer.width, frontBuffer.height,
                    frontBuffer.depth, frontBuffer.format);
                Graphics.Blit(frontBuffer, stashRT);
                return (target, stashRT);
            }).ToArray();
            tileStashes.Add(tile, stash);
        }

        public void Pop()
        {
            foreach (var tileStash in tileStashes)
            {
                foreach (var (target, stash) in tileStash.Value)
                {
                    Graphics.Blit(stash, target.targetRT.frontBuffer);
                }
            }
        }

        public void Release()
        {
            foreach (var tileStash in tileStashes)
            {
                foreach (var (_, stash) in tileStash.Value)
                {
                    RenderTexture.ReleaseTemporary(stash);
                }
            }

            tileStashes.Clear();
        }

        public void Dispose()
        {
            Release();
        }
    }
}