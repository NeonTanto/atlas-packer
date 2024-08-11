using System.Collections.Generic;
using UnityEngine;

namespace NeonTanto.Tools.AtlasPacking
{
    public struct AtlasData
    {
        public Vector2Int size;
        public IReadOnlyList<AtlasRect> rects;
    }
}