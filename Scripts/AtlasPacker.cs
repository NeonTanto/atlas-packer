using System.Collections.Generic;
using Unity.Plastic.Antlr3.Runtime.Misc;
using UnityEngine;

namespace NeonTanto.Tools.AtlasPacking
{
    public class AtlasPacker
    {
        private readonly List<AtlasPackingContext> contexts;
        private List<AtlasData> packedAtlases;
        private List<List<AtlasRect>> packedAtlasRects;
        private List<AtlasRect> notPackedRects;
        private readonly Vector2Int padding;
        private readonly Vector2Int maxSize;

        public AtlasPackData PackData { get; private set; }

        public AtlasPacker(uint padding = 2, uint maxSize = 4096)
        {
            this.padding = Vector2Int.one * (int)padding;
            this.maxSize = Vector2Int.one * (int)maxSize + this.padding;

            contexts = new List<AtlasPackingContext>();
            packedAtlases = new List<AtlasData>();
            packedAtlasRects = new ListStack<List<AtlasRect>>();
            notPackedRects = new List<AtlasRect>();

            PackData = new AtlasPackData(packedAtlases.AsReadOnly(), notPackedRects.AsReadOnly());
        }

        public void AddRects(AtlasRect[] identifiedRects)
        {
            foreach (var identifiedRect in identifiedRects)
            {
                AddRect(identifiedRect.bounds.size, identifiedRect.id);
            }
        }
        
        public void AddRect(Vector2Int size, int id)
        {
            if (size.x <= 0 || size.y <= 0)
            {
                Debug.LogError($"Invalid size for packing: [{id} : {size}]");
                notPackedRects.Add(new AtlasRect {bounds = new RectInt(Vector2Int.zero, size), id = id});
                return;
            }

            size += padding;
            if (size.x > maxSize.x || size.y > maxSize.y)
            {
                size -= padding;
                Debug.LogError($"Invalid size for packing: [{id} : {size}]");
                notPackedRects.Add(new AtlasRect {bounds = new RectInt(Vector2Int.zero, size), id = id});
                return;
            }

            if (contexts.Count == 0) AddNewContext(size);

            var expandData = new AtlasExpandData {areaPenalty = float.MaxValue};

            for (var i = 0; i < contexts.Count; i++)
            {
                var context = contexts[i];

                if (context.TryGetExpandData(size, out var candidateExpandData))
                {
                    if (candidateExpandData.delta == Vector2Int.zero)
                    {
                        context.Insert(candidateExpandData.bounds, id);
                        InsertIntoPackedAtlases(candidateExpandData, id);
                        break;
                    }

                    if (expandData.areaPenalty > candidateExpandData.areaPenalty)
                    {
                        expandData = candidateExpandData;
                    }
                }

                if (i == contexts.Count - 1)
                {
                    if (expandData.context != null)
                    {
                        expandData.context.Expand(expandData.delta);
                        expandData.context.Insert(expandData.bounds, id);
                        InsertIntoPackedAtlases(expandData, id);
                        break;
                    }

                    if (context.IsEmpty)
                    {
                        size -= padding;
                        Debug.LogError($"Cant pack rect: [{id} : {size}");
                        notPackedRects.Add(new AtlasRect {bounds = new RectInt(Vector2Int.zero, size), id = id});
                        return;
                    }

                    AddNewContext(size);
                }
            }
        }

        public void Reset()
        {
            contexts.Clear();
            packedAtlases = new List<AtlasData>();
            notPackedRects = new List<AtlasRect>();
            packedAtlasRects = new List<List<AtlasRect>>();
            PackData = new AtlasPackData(packedAtlases.AsReadOnly(), notPackedRects.AsReadOnly());
        }

        private void InsertIntoPackedAtlases(AtlasExpandData data, int id)
        {
            if (data.context == null) return;
            var idx = contexts.IndexOf(data.context);

            var atlas = packedAtlases[idx];
            var atlasRects = packedAtlasRects[idx];

            atlas.size += data.delta;
            atlasRects.Add(new AtlasRect
            {
                id = id,
                bounds = new RectInt(data.bounds.position, data.bounds.size - padding)
            });

            packedAtlases[idx] = atlas;
        }

        private void AddNewContext(Vector2Int size)
        {
            contexts.Add(new AtlasPackingContext(size, maxSize));
            var atlasRects = new List<AtlasRect>();
            packedAtlasRects.Add(atlasRects);
            packedAtlases.Add(new AtlasData {rects = atlasRects.AsReadOnly(), size = size - padding});
        }
    }
}