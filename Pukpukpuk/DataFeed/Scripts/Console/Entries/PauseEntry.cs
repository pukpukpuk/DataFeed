using System.Collections.Generic;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Console.Entries
{
#if UNITY_EDITOR
    public class PauseEntry : ILogEntry
    {
        public const float MinElapsedTimeBetweenEntries = 2f;
        private readonly ConsoleWindow _consoleWindow;
        private readonly List<ILogEntry> _entries;

        private readonly int _index;

        private float elapsedTime;

        public PauseEntry(int index, List<ILogEntry> entries, ConsoleWindow consoleWindow)
        {
            _index = index;
            _entries = entries;

            _consoleWindow = consoleWindow;
        }

        public void Draw(GUIStyle style)
        {
            _consoleWindow.DrawCenteredLabel($" --- Time Between: {elapsedTime} seconds --- ", style);
        }

        public bool isMayBeDrawn()
        {
            if (_index >= _entries.Count - 1) return false;
            if (_entries[_index] is not LogEntry) return false;
            if (_entries[_index + 1] is not LogEntry) return false;

            var first = (LogEntry)_entries[_index];
            var second = (LogEntry)_entries[_index + 1];
            
            elapsedTime = GetElapsedTime(first, second);
            return elapsedTime >= MinElapsedTimeBetweenEntries;
        }

        public static float GetElapsedTime(LogEntry first, LogEntry second)
        {
            return Mathf.Floor((second.Time - first.Time) * 100f) / 100f;
        }
    }
#endif
}