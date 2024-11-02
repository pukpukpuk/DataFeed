using System;
using Pukpukpuk.DataFeed.Shared;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Utils
{
    public static class ColorUtils
    {
        private const string Gray = "#ababab";
        private const string White = "#ffffff";

        private const string Red = "#cc6666";
        private const string Yellow = "#fceba8";
        
        public static Color GetColor(this LogMessageType logMessageType)
        {
            ColorUtility.TryParseHtmlString(logMessageType.GetHex(), out var color);
            return color;
        }

        public static string GetHex(this LogMessageType logMessageType) => GetHex(logMessageType.ToString());

        private static string GetHex(string logMessageType)
        {
            return logMessageType switch
            {
                "Unimportant" => Gray,
                "Info" => White,
                "Warning" => Yellow,
                "Error" => Red,
                _ => throw new ArgumentOutOfRangeException(nameof(logMessageType), logMessageType, null)
            };
        }
        
        public static string ColorText(string hex, string text)
        {
            return $"<color={hex}>{text}</color>";
        }
    }
}