using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pukpukpuk.DataFeed.Console.Windows;
using Pukpukpuk.DataFeed.Utils;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Console.Entries
{
#if UNITY_EDITOR
    [Serializable]
    public class LogEntry : ILogEntry
    {
        public string MessageWithoutTags;
        public Layer Layer;
        public LogMessageType MessageType;
        public string LayerText;
        public string Tag;

        public string Stack = "Stack";
        [SerializeField] private float _time;

        public ConsoleWindow ConsoleWindow;

        [SerializeField] private string StackWithHyperlinks_cached;

        [SerializeField] public string TimeText;

        private string _message;

        public LogEntry(string message, Layer layer, float time, ConsoleWindow consoleWindow)
        {
            Message = message;

            Layer = layer;
            Time = time;

            ConsoleWindow = consoleWindow;
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                MessageWithoutTags = StringUtils.RemoveTags(_message);
            }
        }

        public float Time
        {
            get => _time;
            set
            {
                _time = value;
                TimeText = TimeSpan.FromSeconds(_time).ToString(@"h\:mm\:ss\.ff");
            }
        }

        public void Draw(GUIStyle style)
        {
            DrawMessageLine(ColorBackground(style));
        }

        private GUIStyle ColorBackground(GUIStyle style)
        {
            var styleCopy = new GUIStyle(style);
            if (MessageType is LogMessageType.Warning or LogMessageType.Error)
            {
                var color = styleCopy.normal.background.GetPixel(0, 0);
                color = Color.Lerp(color, MessageType.GetColor(), .1f);
                styleCopy.normal.background = TextureUtils.CreateTexture(color);
            }

            return styleCopy;
        }

        private void DrawMessageLine(GUIStyle style)
        {
            GUILayout.BeginHorizontal(style);

            var layerText = LayerText;
            var color = Layer.GetColor();

            if (Layer.Name == "Undefined" && MessageType is LogMessageType.Warning or LogMessageType.Error)
                color = MessageType.GetColor();

            ConsoleWindow.DrawLabel(layerText, ConsoleWindow.LayerColumnWidth, color, TextAnchor.MiddleRight);
            ConsoleWindow.DrawLabel(Message, ConsoleWindow.MessageColumnWidth);
            ConsoleWindow.DrawLabel(TimeText, ConsoleWindow.TimeColumnWidth);
            GUILayout.EndHorizontal();
        }

        public string GetStackWithHyperlinks()
        {
            if (!string.IsNullOrEmpty(StackWithHyperlinks_cached)) return StackWithHyperlinks_cached;

            var textWithHyperlinks = new StringBuilder();
            var lines = GetLinesFromStack(Stack);

            foreach (var t in lines)
            {
                var textBeforeFilePath = " (at ";
                var filePathIndex = t.IndexOf(textBeforeFilePath, StringComparison.Ordinal);
                if (filePathIndex > 0)
                {
                    filePathIndex += textBeforeFilePath.Length;
                    if (t[filePathIndex] !=
                        '<') // sometimes no url is given, just an id between <>, we can't do an hyperlink
                    {
                        var filePathPart = t.Substring(filePathIndex);
                        var lineIndex =
                            filePathPart.LastIndexOf(":",
                                StringComparison.Ordinal); // LastIndex because the url can contain ':' ex:"C:"
                        if (lineIndex > 0)
                        {
                            var endLineIndex =
                                filePathPart.LastIndexOf(")",
                                    StringComparison
                                        .Ordinal); // LastIndex because files or folder in the url can contain ')'
                            if (endLineIndex > 0)
                            {
                                var lineString =
                                    filePathPart.Substring(lineIndex + 1, endLineIndex - (lineIndex + 1));
                                var filePath = filePathPart.Substring(0, lineIndex);

                                textWithHyperlinks.Append(
                                    $"{t.Substring(0, filePathIndex)}<color=#4c7eff><a href=\"{filePath}\" line=\"{lineString}\">{filePath}:{lineString}</a></color>)\n");

                                continue; // continue to evade the default case
                            }
                        }
                    }
                }

                // default case if no hyperlink : we just write the line
                textWithHyperlinks.Append(t + "\n");
            }

            // Remove the last \n
            if (textWithHyperlinks.Length > 1) // textWithHyperlinks always ends with \n if it is not empty
                textWithHyperlinks.Remove(textWithHyperlinks.Length - 2, 2);

            return StackWithHyperlinks_cached = textWithHyperlinks.ToString();
        }

        private IEnumerable<string> GetLinesFromStack(string stack)
        {
            return stack.Split("\n")
                .Select(line => line.Trim())
                .SkipWhile(line => line.Contains("Log") || line.Contains("StackTrace ("));
        }
    }
#endif
}