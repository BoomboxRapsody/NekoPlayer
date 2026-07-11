// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NekoPlayer.App.Extensions;
using osu.Framework.Graphics.Textures;
using Spine;

namespace NekoPlayer.App.Graphics.Spine
{
    /// <summary>
    /// An osu!framework compatible <see cref="TextureLoader"/> for Spine.
    /// <para>You can pass a custom <see cref="TextureStore"/> to change the default behaviour of the loader. e.g: filtering mode</para>
    /// </summary>
    // https://github.com/EsotericSoftware/spine-runtimes/blob/4.2/spine-monogame/spine-monogame/src/XnaTextureLoader.cs
    public class SpineTextureLoader : TextureLoader
    {
        private TextureStore _textureStore;
        private string[] _textureLayerSuffixes;

        public SpineTextureLoader(TextureStore textureStore, bool loadMultipleTextureLayers = false, string[] textureSuffixes = null)
        {
            _textureStore = textureStore;
            if (loadMultipleTextureLayers)
                _textureLayerSuffixes = textureSuffixes;
        }

        public void Load(AtlasPage page, string path)
        {
            // I don't really know if it's the correct parameters?
            var wrapS = page.uWrap.ToWrapMode();
            var wrapT = page.vWrap.ToWrapMode();

            var texture = _textureStore.Get(path, wrapS, wrapT)!;
            page.width = texture.Width;
            page.height = texture.Height;

            if (_textureLayerSuffixes == null)
                page.rendererObject = texture;
            else
            {
                var layersArray = new Texture[_textureLayerSuffixes.Length];
                layersArray[0] = texture;
                for (var layer = 1; layer < _textureLayerSuffixes.Length; layer++)
                {
                    var layerPath = GetLayerName(path, _textureLayerSuffixes[0], _textureLayerSuffixes[layer]);
                    layersArray[layer] = _textureStore.Get(layerPath, wrapS, wrapT)!;
                }

                page.rendererObject = layersArray;
            }
        }

        public void Unload(object texture)
        {
            ((Texture)texture).Dispose();
        }

        private static string GetLayerName(string firstLayerPath, string firstLayerSuffix, string replacementSuffix)
        {

            var suffixLocation = firstLayerPath.LastIndexOf(firstLayerSuffix + ".", StringComparison.Ordinal);
            if (suffixLocation == -1)
            {
                throw new Exception(string.Concat("Error composing texture layer name: first texture layer name '", firstLayerPath,
                    "' does not contain suffix to be replaced: '", firstLayerSuffix, "'"));
            }
            return firstLayerPath.Remove(suffixLocation, firstLayerSuffix.Length).Insert(suffixLocation, replacementSuffix);
        }
    }
}
