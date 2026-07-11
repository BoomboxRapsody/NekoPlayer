// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Textures;

namespace NekoPlayer.App.Graphics.Spine.Rendering
{
    /// <summary>
    /// Draws batched meshses, used in <see cref="SpineSprite.SpineSprite_DrawNode"/>.
    /// <para>Ported over from: https://github.com/EsotericSoftware/spine-runtimes/blob/4.2/spine-monogame/spine-monogame/src/MeshBatcher.cs</para>
    /// </summary>
    public class SpineMeshBatcher : IDisposable
    {
        private readonly List<MeshItem> _items;
        private readonly Queue<MeshItem> _freeItems;

        private IVertexBatch<TexturedVertex2D> _vertexBatch;
        private TexturedVertex2D[] _vertexArray = [];
        private short[] _indices = [];

        private readonly IRenderer _renderer;

        public SpineMeshBatcher(IRenderer renderer)
        {
            _renderer = renderer;
            _items = new List<MeshItem>(1024);
            _freeItems = new Queue<MeshItem>(1024);
            EnsureCapacity(1024, 2048);
        }

        /// <summary>
        /// Looks or creates a <see cref="MeshItem"/> for its cosumption.
        /// </summary>
        /// <param name="vertexCount">The amount of vertices the item should hold.</param>
        /// <param name="indexCount">The amount of indices (triangles) the item should hold.</param>
        /// <returns>A pooled <see cref="MeshItem"/> or a newly created one.</returns>
        public MeshItem NextItem(int vertexCount, int indexCount)
        {
            var item = _freeItems.Count > 0 ? _freeItems.Dequeue() : new MeshItem();
            item.EnsureCapacity(vertexCount, indexCount);
            _items.Add(item);
            return item;
        }

        private void EnsureCapacity(int vertexCount, int triangleCount)
        {
            if (_vertexArray.Length < vertexCount) _vertexArray = new TexturedVertex2D[vertexCount];
            if (_indices.Length < triangleCount) _indices = new short[triangleCount];

            // The reason why we do this is because if we vertex batch is too small, some triangles won't be drawn into the screen
            // thus we have to recreate the batch to fit the maximum size possible so we can draw without artifacts
            // It usually takes around 2~3 calls to reach the maximum size, but could throw at some point.
            if (_vertexBatch?.Size < triangleCount)
            {
                _vertexBatch?.Dispose();
                _vertexBatch = null;
            }

            _vertexBatch ??= _renderer.CreateLinearBatch<TexturedVertex2D>(triangleCount, vertexCount, PrimitiveTopology.Triangles);
        }

        public void Draw()
        {
            if (_items.Count == 0) return;

            var itemCount = _items.Count;
            var vertexCount = 0;
            var indicesCount = 0;
            for (var i = 0; i < itemCount; i++)
            {
                var item = _items[i];
                vertexCount += item.VertexCount;
                indicesCount += item.IndexCount;
            }
            EnsureCapacity(vertexCount, indicesCount);

            vertexCount = 0;
            indicesCount = 0;
            Texture lastTexture = null!;
            for (var i = 0; i < itemCount; i++)
            {
                var item = _items[i];
                var itemVertexCount = item.VertexCount;

                if (item.Texture != lastTexture || vertexCount + itemVertexCount > short.MaxValue)
                {
                    FlushVertex(vertexCount, indicesCount, lastTexture);
                    vertexCount = 0;
                    indicesCount = 0;
                    lastTexture = item.Texture!;

                    if (item.TextureLayers != null)
                    {
                        throw new NotImplementedException();
                        for (var layer = 1; layer < item.TextureLayers.Length; layer++)
                        {
                            // _textureLayers[layer] = item.Texture;
                        }
                    }
                }

                var itemIndices = item.Indices;
                var itemIndicesCount = item.IndexCount;
                for (int ii = 0, t = indicesCount; ii < itemIndicesCount; ii++, t++)
                    _indices[t] = (short)(itemIndices[ii] + vertexCount);

                indicesCount += itemIndicesCount;

                Array.Copy(item.Vertices, 0, _vertexArray, vertexCount, itemVertexCount);
                vertexCount += itemVertexCount;
            }

            FlushVertex(vertexCount, indicesCount, lastTexture);
        }

        public void AfterLastDrawPass()
        {
            var itemCount = _items.Count;
            for (var i = 0; i < itemCount; i++)
            {
                var item = _items[i];
                item.Texture = null;
                _freeItems.Enqueue(item);
            }
            _items.Clear();
        }

        private void FlushVertex(int vertexCount, int indicesCount, Texture texture)
        {
            // renderer.BindTexture binds the current renderer to the working texture
            if (vertexCount == 0 || !_renderer.BindTexture(texture)) return;

            for (var i = 0; i < indicesCount; i += 1) // apply spine 4.1 fix
            {
                var index0 = _indices[i];

                var v0 = _vertexArray[index0];

                _vertexBatch!.AddAction(v0);
            }

            _vertexBatch!.Draw();
        }

        public void Dispose()
        {
            _vertexBatch?.Dispose();
        }

        public class MeshItem
        {
            public Texture Texture;
            public Texture[] TextureLayers = null;

            // EnsureCapacity will be ran shortly after being created so it sets the size properly
            public TexturedVertex2D[] Vertices = [];
            public int[] Indices = [];

            public int VertexCount;
            public int IndexCount;

            public void EnsureCapacity(int vertexCount, int indexCount)
            {
                if (Vertices.Length < vertexCount) Vertices = new TexturedVertex2D[vertexCount];
                if (Indices.Length < indexCount) Indices = new int[indexCount];
                VertexCount = vertexCount;
                IndexCount = indexCount;
            }
        }
    }
}
