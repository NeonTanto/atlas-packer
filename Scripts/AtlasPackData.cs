using System.Collections.Generic;

namespace NeonTanto.Tools.AtlasPacking
{
    public class AtlasPackData
    {
        public readonly IReadOnlyList<AtlasData> packedAtlases;
        public readonly IReadOnlyList<AtlasRect> notPackedRects;

        internal AtlasPackData(IReadOnlyList<AtlasData> packedAtlases, IReadOnlyList<AtlasRect> notPackedRects)
        {
            this.packedAtlases = packedAtlases;
            this.notPackedRects = notPackedRects;
        }
    }
}