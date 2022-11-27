using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MetaBrush.Collections;

namespace MetaBrush
{
    public static class OperationStash
    {
        public static readonly LinkedList<PainterOp> operations = new LinkedList<PainterOp>();
        public static LinkedListNode<PainterOp> index { get; private set; }
        public static int maxSteps = 20;
        static LinkedListNode<PainterOp> top = null;

        static ObjectPool<PainterOp> operationPool = new ObjectPool<PainterOp>(
            () => new PainterOp(),
            onDestroy: (op) => op.Dispose(),
            onRelease: (op) => op.Release());

        public static bool operating = false;

        public static string status
        {
            get
            {
                System.Text.StringBuilder status = new System.Text.StringBuilder(OperationStash.operations.Count + 1);
                foreach (var op in OperationStash.operations)
                {
                    if (OperationStash.index.Value == op && OperationStash.top?.Value != op)
                    {
                        status.Append('^');
                    }
                    else if (OperationStash.top?.Value != op)
                    {
                        status.Append('+');
                    }
                }

                return status.ToString();
            }
        }

        public static void Clear()
        {
            foreach (var op in operations)
            {
                operationPool.Release(op);
            }

            operations.Clear();
            index = null;
            top = null;
        }

        public static void BeginOperation()
        {
            operationPool.TryGet(out PainterOp op);
            if (top != null)
            {
                operations.RemoveLast();
                operationPool.Release(top.Value);
                top = null;
            }

            while (operations.Count > 0 && operations.Last != index)
            {
                var lastOp = operations.Last.Value;
                operationPool.Release(lastOp);
                operations.RemoveLast();
            }

            while (operations.Count > maxSteps)
            {
                var firstOp = operations.First.Value;
                operationPool.Release(firstOp);
                operations.RemoveFirst();
            }

            index = operations.AddLast(op);
            operating = true;
        }

        public static void Record(this MetaPainter painter, Tile tile)
        {
            index.Value.Stash(painter, tile);
        }

        public static void Undo(this MetaPainter painter)
        {
            if (operating || index == null)
                return;
            if (index == operations.Last && top == null)
            {
                PainterOp lastOp = index.Value;
                operationPool.TryGet(out PainterOp op);
                foreach (var tile in lastOp.tiles)
                {
                    op.Stash(painter, tile);
                }

                top = operations.AddLast(op);
            }

            index.Value.Pop();
            if (index.Previous != null)
                index = index.Previous;
        }

        public static void Redo(this MetaPainter painter)
        {
            if (operating || index == null)
                return;
            if (index.Next != null && index.Next != top)
                index = index.Next;
            index.Next?.Value?.Pop();
        }

        public static void EndOperation()
        {
            if (index.Value.tiles.Count() == 0)
            {
                operations.RemoveLast();
            }

            operating = false;
        }
    }
}