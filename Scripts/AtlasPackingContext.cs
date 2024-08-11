using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeonTanto.Tools.AtlasPacking
{
    internal class AtlasPackingContext
    {
        private AtlasData atlas;
        private readonly List<AtlasRect> packedRects;
        private readonly LinkedList<Vector2Int> points;
        private readonly LinkedList<int> xLines;
        private readonly LinkedList<int> yLines;
        private readonly Vector2Int maxSize;

        public bool IsEmpty => packedRects.Count == 0;

        public AtlasPackingContext(Vector2Int initialSize, Vector2Int maxSize)
        {
            packedRects = new List<AtlasRect>();

            atlas = new AtlasData
            {
                rects = packedRects,
                size = initialSize
            };

            points = new LinkedList<Vector2Int>();
            xLines = new LinkedList<int>();
            yLines = new LinkedList<int>();

            xLines.AddFirst(0);
            yLines.AddFirst(0);

            points.AddLast(Vector2Int.zero);

            this.maxSize = maxSize;
        }

        public bool TryGetExpandData(Vector2Int size, out AtlasExpandData expandData)
        {
            expandData = new AtlasExpandData{ areaPenalty = float.MaxValue };

            foreach (var point in points)
            {
                var candidate = new RectInt(point, size);

                if (!IsIntersectedWithPacked(candidate) && TryGetExpandData(candidate, out var candidateExpand))
                {
                    if (candidateExpand.delta == Vector2Int.zero)
                    {
                        expandData = candidateExpand;
                        return true;
                    }

                    if (expandData.areaPenalty > candidateExpand.areaPenalty)
                    {
                        expandData = candidateExpand;
                    }
                }
            }

            return expandData.context != null;
        }

        public void Insert(RectInt rect, int id)
        {
            packedRects.Add(new AtlasRect
            {
                id = id,
                bounds = rect
            });

            points.RemoveAll(node => rect.Contains(node.Value));

            ProcessNewXLine(rect.xMax);
            ProcessNewYLine(rect.yMax);
        }

        public void Expand(Vector2Int delta)
        {
            var oldSize = atlas.size;
            atlas.size += delta;
            var newSize = atlas.size;

            if (delta.x != 0)
            {
                if (packedRects.All(rect => rect.bounds.xMax != oldSize.x))
                {
                    xLines.RemoveAll(node => node.Value == oldSize.x);
                    points.RemoveAll(node => node.Value.x == oldSize.x);
                }

                ProcessNewXLine(newSize.x);
            }

            if (delta.y != 0)
            {
                if (packedRects.All(rect => rect.bounds.yMax != oldSize.y))
                {
                    xLines.RemoveAll(node => node.Value == oldSize.y);
                    points.RemoveAll(node => node.Value.y == oldSize.y);
                }

                ProcessNewYLine(newSize.y);
            }
        }

        private bool TryGetExpandData(RectInt rect, out AtlasExpandData expandData)
        {
            expandData = default;
            if (rect.xMax > maxSize.x || rect.yMax > maxSize.x) return false;

            var size = atlas.size;

            var delta = new Vector2Int
            {
                x = Mathf.Max(0, rect.xMax - size.x),
                y = Mathf.Max(0, rect.yMax - size.y)
            };

            var multiplierX = atlas.size.x / atlas.size.y;
            var multiplierY = atlas.size.y / atlas.size.x;

            var areaPenalty = delta.x * size.y * multiplierX + delta.y * size.x * multiplierY;

            expandData = new AtlasExpandData
            {
                areaPenalty = areaPenalty,
                delta = delta,
                context = this,
                bounds = rect
            };

            return true;
        }

        private void ProcessNewXLine(int x)
        {
            var current = xLines.First;
            while (current != null)
            {
                if (x == current.Value) return;
                if (x < current.Value)
                {
                    xLines.AddBefore(current, x);
                    foreach (var y in yLines)
                    {
                        ProcessNewPoint(new Vector2Int(x, y));
                    }
                    return;
                }

                current = current.Next;
            }

            xLines.AddLast(x);
            foreach (var y in yLines)
            {
                ProcessNewPoint(new Vector2Int(x, y));
            }
        }

        private void ProcessNewYLine(int y)
        {
            var current = yLines.First;
            while (current != null)
            {
                if (y == current.Value) return;
                if (y < current.Value)
                {
                    yLines.AddBefore(current, y);
                    foreach (var x in xLines)
                    {
                        ProcessNewPoint(new Vector2Int(x, y));
                    }
                    return;
                }

                current = current.Next;
            }

            yLines.AddLast(y);
            foreach (var x in xLines)
            {
                ProcessNewPoint(new Vector2Int(x, y));
            }
        }

        private void ProcessNewPoint(Vector2Int point)
        {
            if (IsContainedByPacked(point)) return;

            var current = points.First;
            while (current != null)
            {
                var currentValue = current.Value;
                if (currentValue == point) return;
                if (point.x < currentValue.x || (point.x == currentValue.x && point.y < currentValue.y))
                {
                    points.AddBefore(current, point);
                    return;
                }

                current = current.Next;
            }

            points.AddLast(point);
        }

        private bool IsIntersectedWithPacked(RectInt rect)
        {
            return packedRects.Any(packedRect => packedRect.bounds.IsIntersect(rect));
        }

        private bool IsContainedByPacked(Vector2Int point)
        {
            return packedRects.Any(packedRect => packedRect.bounds.IsContain(point));
        }
    }
}