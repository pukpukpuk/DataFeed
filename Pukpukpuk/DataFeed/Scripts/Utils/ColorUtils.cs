using System;
using Pukpukpuk.DataFeed.Console;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Utils
{
    public static class ColorUtils
    {
        public static string GetHex(this LogMessageType logMessageType)
        {
            return logMessageType switch
            {
                LogMessageType.Unimportant => DebugUtils.Gray,
                LogMessageType.Info => DebugUtils.White,
                LogMessageType.Warning => DebugUtils.Yellow,
                LogMessageType.Error => DebugUtils.Red,
                _ => throw new ArgumentOutOfRangeException(nameof(logMessageType), logMessageType, null)
            };
        }

        public static Color GetColor(this LogMessageType logMessageType)
        {
            ColorUtility.TryParseHtmlString(logMessageType.GetHex(), out var color);
            return color;
        }

        public static string ColorText(string hex, string text)
        {
            return $"<color={hex}>{text}</color>";
        }
    }
}