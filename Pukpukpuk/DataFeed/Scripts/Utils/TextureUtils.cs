using System.Collections.Generic;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Utils
{
    public static class TextureUtils
    {
        private static readonly Dictionary<Color, Texture2D> _cachedTextures = new();
        
        public static GUIStyle CreateBackground(Color color)
        {
            var result = new GUIStyle();
            result.normal.background = CreateTexture(color);
            return result;
        }

        public static Texture2D CreateTexture(Color color)
        {
            if (_cachedTextures.TryGetValue(color, out var texture)) return texture;

            var result = new Texture2D(1, 1);
            result.SetPixels(new [] {color});
            result.Apply();
            
            _cachedTextures[color] = result;
            return result;
        }
    }
}