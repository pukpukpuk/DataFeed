using Pukpukpuk.DataFeed.Console.Windows;
using Pukpukpuk.DataFeed.Console.Windows.Console;
using Pukpukpuk.DataFeed.Utils;

namespace Pukpukpuk.DataFeed.Console
{
    public static class DebugUtils
    {
        public const string Gray = "#ababab";
        public const string White = "#ffffff";

        public const string Red = "#cc6666";
        public const string Yellow = "#fceba8";

        public const string PlusSign = "<color=#6ec077>+</color>";
        public const string MinusSign = "<color=#cc6666>-</color>";

        /// <summary>
        /// Log message to DataFeed console
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="layerName">Layer name</param>
        /// <param name="logMessageType">Type of message</param>
        /// <param name="prefix">Prefix that will be shown instead of layer name</param>
        /// <param name="tag">Message tag. Used for hiding specific entries</param>
        public static void LogLayer(object message, string layerName, LogMessageType logMessageType,
            string prefix = null, string tag = null)
        {
            LogLayer(message, layerName, logMessageType.GetHex(), logMessageType, prefix, tag);
        }

        /// <summary>
        /// Log message to DataFeed console
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="layerName">Layer name</param>
        /// <param name="textColor">Color of the text</param>
        /// <param name="logMessageType">Type of message</param>
        /// <param name="prefix">Prefix that will be shown instead of layer name</param>
        /// <param name="tag">Message tag. Used for hiding specific entries</param>
        public static void LogLayer(object message, string layerName, string textColor = White,
            LogMessageType logMessageType = LogMessageType.Info, string prefix = null, string tag = null)
        {
            var text = ColorUtils.ColorText(textColor, message?.ToString());
#if UNITY_EDITOR
            ConsoleWindow.LogToConsole(text, layerName, logMessageType, tag, prefix);
#endif
        }

        /// <summary>
        /// Log message to DataFeed console
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="logMessageType">Type of message</param>
        public static void Log(object message, LogMessageType logMessageType)
        {
            Log(message, logMessageType.GetHex(), logMessageType);
        }

        /// <summary>
        /// Log message to DataFeed console
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="textColor">Color of the text</param>
        /// <param name="logMessageType">Type of message</param>
        public static void Log(object message, string textColor = White,
            LogMessageType logMessageType = LogMessageType.Info)
        {
            var text = ColorUtils.ColorText(textColor, message?.ToString());
#if UNITY_EDITOR
            ConsoleWindow.LogToConsole(text, "Undefined", logMessageType);
#endif
        }
    }

    public enum LogMessageType
    {
        Unimportant,
        Info,
        Warning,
        Error
    }
}