using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeonTanto.Tools.AtlasPacking
{
    public static class Extensions
    {
        internal static bool IsIntersect(this RectInt self, RectInt other)
        {
            var separatedByX = self.xMax <= other.xMin || other.xMax <= self.xMin;
            var separatedByY = self.yMax <= other.yMin || other.yMax <= self.yMin;

            return !separatedByX && !separatedByY;
        }

        internal static bool IsContain(this RectInt self, Vector2Int point, bool includeLeftBottom = false)
        {
            var isInRightBottomPart = point.x >= self.xMin && point.y >= self.yMin;
            var isInLeftTopPart = includeLeftBottom
                ? point.x <= self.xMax && point.y <= self.yMax
                : point.x < self.xMax && point.y < self.yMax;
            
            return isInRightBottomPart && isInLeftTopPart;
        }

        internal static void RemoveAll<T>(this LinkedList<T> list, Func<LinkedListNode<T>, bool> predicate)
        {
            var current = list.First;
            while (current != null)
            {
                var old = current;
                current = current.Next;

                if (predicate(old))
                {
                    list.Remove(old);
                }
            }
        }

        internal static float GetPackRatio(this AtlasData atlas)
        {
            if (atlas.size.x == 0 || atlas.size.y == 0) return 0;

            var atlasArea = (float)atlas.size.x * atlas.size.y;
            var rectsArea = atlas.rects.Count == 0 ? 0f : atlas.rects.Sum(rect => rect.bounds.width * rect.bounds.height);

            return rectsArea / atlasArea;
        }

        internal static float GetAverageRatio(this AtlasPackData packData)
        {
            if (packData == null) return 0;
            if (packData.packedAtlases.Count == 0) return 0;

            var sumRatioPerPixel = packData.packedAtlases.Sum(data => data.GetPackRatio() * data.size.x * data.size.y);
            var sumArea = packData.packedAtlases.Sum(data => data.size.x * data.size.y);

            return sumRatioPerPixel / sumArea;
        }

        public static AtlasPackData PackRects(this AtlasPacker packer, AtlasRect[] atlasRects, RectOrder order = RectOrder.HeightThenWidth)
        {
            packer.Reset();

            switch (order)
            {
                case RectOrder.HeightThenWidth:
                    atlasRects = atlasRects
                        .OrderByDescending(rect => rect.bounds.height)
                        .ThenByDescending(rect => rect.bounds.width)
                        .ToArray();
                    break;
                case RectOrder.WidthThenHeight:
                    atlasRects = atlasRects
                        .OrderByDescending(rect => rect.bounds.width)
                        .ThenByDescending(rect => rect.bounds.height)
                        .ToArray();
                    break;
                case RectOrder.AreaThenHeight:
                    atlasRects = atlasRects
                        .OrderByDescending(rect => rect.bounds.height * rect.bounds.width)
                        .ThenByDescending(rect => rect.bounds.height)
                        .ToArray();
                    break;
                case RectOrder.AreaThenWidth:
                    atlasRects = atlasRects
                        .OrderByDescending(rect => rect.bounds.height * rect.bounds.width)
                        .ThenByDescending(rect => rect.bounds.width)
                        .ToArray();
                    break;
            }

            packer.AddRects(atlasRects);
            var result = packer.PackData;

            packer.Reset();

            return result;
        }

        public static AtlasPackData PackRectsWithBestOrder(this AtlasPacker packer, AtlasRect[] atlasRects, out RectOrder selectedOrder)
        {
            var orders = (RectOrder[])Enum.GetValues(typeof(RectOrder));

            selectedOrder = default;
            AtlasPackData result = null;
            float bestRatio = 0;

            foreach (var order in orders)
            {
                var candidate = packer.PackRects(atlasRects, order);
                var candidateRatio = candidate.GetAverageRatio();

                if (result == null || candidateRatio > bestRatio)
                {
                    result = candidate;
                    selectedOrder = order;
                    bestRatio = candidateRatio;
                }
            }

            return result;
        }
    }
}