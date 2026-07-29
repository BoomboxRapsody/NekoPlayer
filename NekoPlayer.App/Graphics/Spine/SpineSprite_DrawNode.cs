// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NekoPlayer.App.Extensions;
using NekoPlayer.App.Graphics.Spine.Rendering;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using Spine;

namespace NekoPlayer.App.Graphics.Spine
{
    public partial class SpineSprite
    {
        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// The "SkeletonRenderer", bound to a <see cref="SpineSprite"/>.
        /// <para>Ported from: https://github.com/EsotericSoftware/spine-runtimes/blob/4.2/spine-monogame/spine-monogame/src/SkeletonRenderer.cs</para>
        /// </summary>
        public class SpineSprite_DrawNode : SpriteDrawNode
        {
            protected new SpineSprite Source => (SpineSprite)base.Source;

            // We don't really need to expose clipping, but if we need to, we should move
            // this clipper into the SpineSprite itself and grab it here through ApplyState
            private SkeletonClipping _clipper = new();
            private SpineMeshBatcher _batcher;

            private float[] _vertices = new float[8];
            private int[] _quadTriangles = [0, 1, 2, 2, 3, 0];

            private BlendingParameters _defaultBlendState;
            private BlendingParameters? _blendStateMultiply;

            // TODO: Effects (should be shaders basically?)
            public IVertexEffect VertexEffect { get; set; } = null;

            private bool _premultipliedAlpha;
            private float _zSpacing;
            private float _z;

            private Skeleton _skeleton;

            public SpineSprite_DrawNode(SpineSprite source) : base(source)
            {
                Bone.yDown = true;
            }

            public override void ApplyState()
            {
                base.ApplyState();

                // By the time Invalidate is called the skeleton should be populated already in the source sprite
                _skeleton = Source.Skeleton;

                _z = Source.Depth;
                _zSpacing = Source.ZSpacing;

                _premultipliedAlpha = Source.PremultipliedAlpha;
            }

            protected override void Draw(IRenderer renderer)
            {
                // Before drawing we set up the blending and alpha
                _defaultBlendState = SpineBlendModes.GetDefault(_premultipliedAlpha);
                _blendStateMultiply ??= new BlendingParameters
                {
                    Source = BlendingType.DstColor,
                    Destination = BlendingType.Zero,
                    RGBEquation = BlendingEquation.Max,
                };

                _batcher ??= new SpineMeshBatcher(renderer);

                renderer.PushLocalMatrix(DrawInfo.Matrix);

                Source.Blending = _defaultBlendState;
                renderer.SetBlend(_defaultBlendState);

                // Will blit the animation
                base.Draw(renderer);

                EndDraw(renderer);
            }

            // Should be called inside the shader bind (base.Draw)
            protected virtual void EndDraw(IRenderer renderer)
            {
                _batcher?.Draw();

                // Should make a pass on effects and draw the batch?
                renderer.PopLocalMatrix();
                _batcher?.AfterLastDrawPass();
            }

            protected override void Blit(IRenderer renderer)
            {
                if (_skeleton == null) return;

                VertexEffect?.Begin(_skeleton);

                var drawOrder = _skeleton.DrawOrder;
                var drawOrderItems = _skeleton.DrawOrder.Items;

                var skeletonColor = new Vector4(_skeleton.R, _skeleton.G, _skeleton.B, _skeleton.A);

                for (var i = 0; i < drawOrder.Count; i++)
                {
                    var slot = drawOrderItems[i];
                    if (!slot.Bone.Active)
                    {
                        _clipper.ClipEnd(slot);
                        continue;
                    }

                    var attachment = slot.Attachment;
                    var attachmentZOffset = (_z + _zSpacing) * i;

                    Vector4 attachmentColor;

                    AtlasRegion region;

                    int verticesCount;
                    var vertices = _vertices;

                    int indicesCount;
                    int[] indices;
                    float[] uvs;

                    switch (attachment)
                    {
                        case RegionAttachment regionAttachment:
                        {
                            attachmentColor = new Vector4(regionAttachment.R, regionAttachment.G, regionAttachment.B, regionAttachment.A);

                            regionAttachment.ComputeWorldVertices(slot, vertices, 0);

                            verticesCount = 4;

                            indicesCount = 6;
                            indices = _quadTriangles;

                            uvs = regionAttachment.UVs;

                            region = (AtlasRegion)regionAttachment.Region;
                            break;
                        }
                        case MeshAttachment mesh:
                        {
                            attachmentColor = new Vector4(mesh.R, mesh.G, mesh.B, mesh.A);

                            var vertexCount = mesh.WorldVerticesLength;
                            if (vertices.Length < vertexCount) vertices = new float[vertexCount];
                            verticesCount = vertexCount >> 1;

                            mesh.ComputeWorldVertices(slot, vertices);

                            indicesCount = mesh.Triangles.Length;
                            indices = mesh.Triangles;

                            uvs = mesh.UVs;

                            region = (AtlasRegion)mesh.Region;
                            break;
                        }
                        case ClippingAttachment clip:
                            _clipper.ClipStart(slot, clip);
                            continue;
                        default:
                            _clipper.ClipEnd(slot);
                            continue;
                    }

                    var textureObject = region.page.rendererObject;

                    var blend = slot.Data.BlendMode switch
                    {
                        BlendMode.Additive => BlendingParameters.Additive,
                        BlendMode.Multiply => _blendStateMultiply!.Value,
                        _ => _defaultBlendState
                    };

                    // ?
                    if (Source.Blending != blend)
                    {
                        Source.Blending = blend;
                        renderer.SetBlend(blend);
                        // EndDraw(renderer);
                    }

                    var a = (skeletonColor.W * slot.A * attachmentColor.W) * Source.Alpha;
                    Colour4 color;
                    if (_premultipliedAlpha)
                        color = new Colour4(
                            skeletonColor.X * slot.R * attachmentColor.X * a,
                            skeletonColor.Y * slot.G * attachmentColor.Y * a,
                            skeletonColor.Z * slot.B * attachmentColor.Z * a,
                            a
                        );
                    else
                        color = new Colour4(
                            skeletonColor.X * slot.R * attachmentColor.X,
                            skeletonColor.Y * slot.G * attachmentColor.Y,
                            skeletonColor.Z * slot.B * attachmentColor.Z,
                            a
                        );

                    /*
                    Colour4 darkColor = Colour4.Transparent;
                    var darkAlpha = _premultipliedAlpha ? 255 : 0;
                    if (slot.HasSecondColor)
                        darkColor = new Colour4(slot.R2 * a, slot.G2 * a, slot.B2 * a, darkAlpha);*/

                    if (_clipper.IsClipping)
                    {
                        _clipper.ClipTriangles(vertices, verticesCount, indices, indicesCount, uvs);

                        vertices = _clipper.ClippedVertices.Items;
                        verticesCount = _clipper.ClippedVertices.Count >> 1;

                        indices = _clipper.ClippedTriangles.Items;
                        indicesCount = _clipper.ClippedTriangles.Count >> 1;

                        uvs = _clipper.ClippedUVs.Items;
                    }

                    if (verticesCount == 0 || indicesCount == 0)
                    {
                        _clipper.ClipEnd(slot);
                        continue;
                    }

                    var item = _batcher!.NextItem(verticesCount, indicesCount);
                    if (textureObject is Texture texture)
                        item.Texture = texture;
                    else
                    {
                        item.TextureLayers = (Texture[])textureObject;
                        item.Texture = item.TextureLayers[0];
                    }

                    for (var ii = 0; ii < indicesCount; ii++)
                    {
                        item.Indices[ii] = indices[ii];
                    }

                    var itemVertices = item.Vertices;
                    for (int ii = 0, v = 0, nn = verticesCount << 1; v < nn; ii++, v += 2)
                    {
                        var vertex = new TexturedVertex2D(renderer)
                        {
                            Position = new Vector2(vertices[v], vertices[v + 1]),
                            Colour = color.ToPremultiplied(),
                            //DarkColour = darkColor,
                            TexturePosition = new Vector2(uvs[v], uvs[v + 1]),
                            TextureRect = new Vector4(0, 0, 1, 1),
                            BlendRange = Vector2.One
                        };
                        itemVertices[ii] = vertex;
                        VertexEffect?.Transform(ref vertex);
                    }

                    _clipper.ClipEnd(slot);
                }

                _clipper.ClipEnd();
                VertexEffect?.End();
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);
                _batcher?.Dispose();
            }
        }
    }
}
