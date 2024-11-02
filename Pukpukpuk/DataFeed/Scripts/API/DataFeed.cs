using Pukpukpuk.DataFeed.Console;
using Pukpukpuk.DataFeed.Input;
using Pukpukpuk.DataFeed.Shared;
using Pukpukpuk.DataFeed.Utils;

namespace Pukpukpuk.DataFeed.API
{
    public static class DataFeed
    {
        private const string White = "#ffffff";
        
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
#if UNITY_EDITOR
            var text = ColorUtils.ColorText(textColor, message?.ToString());
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
#if UNITY_EDITOR
            var text = ColorUtils.ColorText(textColor, message?.ToString());
            ConsoleWindow.LogToConsole(text, "Undefined", logMessageType);
#endif
        }

        public static void ExecuteStartupCommands()
        {
#if UNITY_EDITOR
            InputWindow.Instance?.ExecuteStartUpCommands();
#endif
        }
    }
}