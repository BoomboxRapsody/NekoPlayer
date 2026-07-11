// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Vertices;
using Spine;

namespace NekoPlayer.App.Graphics.Spine.Rendering
{
    // https://github.com/EsotericSoftware/spine-runtimes/blob/4.2/spine-monogame/spine-monogame/src/VertexEffect.cs

    public interface IVertexEffect
    {
        void Begin(Skeleton skeleton);
        void Transform(ref TexturedVertex2D vertex);
        void End();
    }

    public class JitterEffect : IVertexEffect
    {
        public float JitterX { get; set; }
        public float JitterY { get; set; }

        public void Begin(Skeleton skeleton) { }
        public void End() { }

        public void Transform(ref TexturedVertex2D vertex)
        {
            vertex.Position.X += MathUtils.RandomTriangle(-JitterX, JitterY);
            vertex.Position.Y += MathUtils.RandomTriangle(-JitterX, JitterY);
        }
    }

    public class SwirlEffect(float radius) : IVertexEffect
    {
        private float _worldX, _worldY, _angle;

        public float Radius { get; set; } = radius;

        public float Angle
        {
            get => _angle;
            set => _angle = value * MathUtils.DegRad;
        }

        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public IInterpolation Interpolation { get; set; } = IInterpolation.Pow2;

        public void Begin(Skeleton skeleton)
        {
            _worldX = skeleton.X + CenterX;
            _worldY = skeleton.Y + CenterY;
        }

        public void End() { }

        public void Transform(ref TexturedVertex2D vertex)
        {
            var x = vertex.Position.X - _worldX;
            var y = vertex.Position.Y - _worldY;
            var dist = (float)Math.Sqrt((x * x) + (y * y));
            if (dist > Radius) return;

            var theta = Interpolation!.Apply(0, _angle, (Radius - dist) / Radius);
            float cos = MathUtils.Cos(theta), sin = MathUtils.Sin(theta);
            vertex.Position.X = (cos * x) - (sin * y) + _worldX;
            vertex.Position.Y = (sin * x) + (cos * y) + _worldY;
        }
    }
}
