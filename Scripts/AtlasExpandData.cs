using UnityEngine;

namespace NeonTanto.Tools.AtlasPacking
{
    internal struct AtlasExpandData
    {
        public float areaPenalty;
        public Vector2Int delta;
        public RectInt bounds;
        public AtlasPackingContext context;
    }
}