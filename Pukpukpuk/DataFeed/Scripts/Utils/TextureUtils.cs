using UnityEngine;

namespace Pukpukpuk.DataFeed.Utils
{
    public static class TextureUtils
    {
        public static GUIStyle CreateBackground(Color color)
        {
            var result = new GUIStyle();
            result.normal.background = CreateTexture(color);
            return result;
        }

        public static Texture2D CreateTexture(Color col, int width = 1, int height = 1)
        {
            var pix = new Color[width * height];

            for (var i = 0; i < pix.Length; i++)
                pix[i] = col;

            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }
    }
}