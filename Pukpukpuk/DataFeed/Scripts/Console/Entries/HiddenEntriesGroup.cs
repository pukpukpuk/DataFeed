using System;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Console.Entries
{
#if UNITY_EDITOR
    [Serializable]
    public class HiddenEntriesGroup : ILogEntry
    {
        [SerializeField] public LogEntry FirstEntry;
        [SerializeField] public int Count = -1;
        [SerializeField] private ConsoleWindow _consoleWindow;

        [SerializeField] public bool Expanded;

        public HiddenEntriesGroup(LogEntry firstEntry, ConsoleWindow consoleWindow)
        {
            FirstEntry = firstEntry;
            _consoleWindow = consoleWindow;
        }

        public void Draw(GUIStyle style)
        {
            var arrowSymbol = Expanded ? "\u25b2" : "\u25bc";
            GUILayout.BeginHorizontal(style);

            var buttonStyle = new GUIStyle(_consoleWindow.TextStyle);
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            var clicked = GUILayout.Button($"...Hidden {Count} Entries... {arrowSymbol}", buttonStyle,
                GUILayout.Width(_consoleWindow.WindowWidth));
            GUILayout.EndHorizontal();

            if (clicked) Expanded = !Expanded;
        }
    }

    [Serializable]
    public class HiddenEntriesGroupEnd : ILogEntry
    {
        [SerializeField] private ConsoleWindow _consoleWindow;

        public HiddenEntriesGroupEnd(ConsoleWindow consoleWindow)
        {
            _consoleWindow = consoleWindow;
        }

        public void Draw(GUIStyle style)
        {
            _consoleWindow.DrawCenteredLabel("End of Hidden Group", style);
        }
    }
#endif
}