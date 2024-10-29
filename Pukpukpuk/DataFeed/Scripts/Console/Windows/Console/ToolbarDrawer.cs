using System;
using System.Collections.Generic;
using System.Linq;
using NPOI.SS.Formula.Functions;
using Pukpukpuk.DataFeed.Console.Entries;
using Pukpukpuk.DataFeed.Utils;
using UnityEditor;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Console.Windows.Console
{
#if UNITY_EDITOR
    [Serializable]
    public class ToolbarDrawer
    {
        private static readonly Color ScrollBarMarkColor = new(.8f, .8f, .5f);
        
        private static readonly float[] ToolbarWidth = { 45f, 105f, 95f };
        private const float MinSearchFieldWidth = 140;
        
        // SeenLogEntries indexes of all found entries
        [SerializeField] private List<int> cachedSearchResult;
        // Log indexes of all found entries. Used for calculating distances between entries
        [SerializeField] private List<int> absoluteCachedSearchResult;
        [SerializeField] private int cursorIndex;

        [SerializeField] private bool caseSensitive;
        
        private Dictionary<LogEntry, HighlightData> cachedSearchResultEntries;
        
        private static GUIStyle ArrowButtonStyle => new("ButtonMid");
        private static GUIStyle ButtonStyle => new("TE toolbarbutton");
        private static GUIStyle DropdownStyle => new ("ToolbarDropDownLeft");
        private static float SearchFieldWidth => ConsoleWindow.Inst.WindowWidth - ToolbarWidth.Sum() + 25;
        private static bool IsSearchDrawn => SearchFieldWidth >= MinSearchFieldWidth;
        
        private static string SearchText
        {
            get => ConsoleWindow.Inst.SearchText;
            set => ConsoleWindow.Inst.SearchText = value;
        }

        private static List<ILogEntry> SeenEntries => ConsoleWindow.Inst.SeenLogEntries ?? new List<ILogEntry>();

        public bool SeenEntriesAreNotActual;
        
        public void DrawToolbar()
        {
            if (SeenEntriesAreNotActual && Event.current.type != EventType.Repaint)
            {
                UpdateSearchResultList();
                SeenEntriesAreNotActual = false;
                ConsoleWindow.Inst.Repaint();
            }
            
            GUILayout.BeginHorizontal(new GUIStyle("Toolbar"));
            
            DrawClearButton();
            DrawShownLayersPopup();
            DrawShownTagsPopup();
            DrawSearchField();
            
            GUILayout.EndHorizontal();
        }

        public void DrawScrollBarMarks()
        {
            if (!ConsoleWindow.Inst.ScrollBarIsVisible) return;
            
            var totalEntriesCount = SeenEntries.Count;
            if (totalEntriesCount == 0) return;
            
            var scrollBarRect = ConsoleWindow.Inst.ScrollBarRect;
            scrollBarRect.yMin += 18;
            scrollBarRect.yMax -= 18;

            foreach (var entryIndex in cachedSearchResult)
            {
                var rect = new Rect(0, 0, 10, 1f)
                {
                    x = scrollBarRect.xMin,
                    y = scrollBarRect.y,
                };
                
                float k = (float) entryIndex / (totalEntriesCount - 1);
                rect.y += scrollBarRect.height * k;

                EditorGUI.DrawRect(rect, ScrollBarMarkColor);
            }
        }

        private void DrawClearButton()
        {
            var expandWidth = !IsSearchDrawn; 
            if (!GUILayout.Button("Clear", ButtonStyle, GUILayout.Width(ToolbarWidth[0]), GUILayout.ExpandWidth(expandWidth)))
            {
                return;
            }
            
            ConsoleWindow.Inst.Log.Clear();
            ConsoleWindow.Inst.Tags.Clear();
            ConsoleWindow.Inst.SelectedEntry = null;

            cachedSearchResult = null;
            absoluteCachedSearchResult = null;
            UpdateCachedLogEntries();
        }
        
        private void DrawShownLayersPopup()
        {
            if (!GUILayout.Button("Shown Layers", DropdownStyle, GUILayout.Width(ToolbarWidth[1])))
            {
                return;
            }
            
            var menu = new GenericMenu();

            var layers = ConsoleWindow.Inst.Layers;
            var config = ConsoleWindow.GetConfig();
                
            var countCondition = layers.Count == config.Layers.Count;
            var isEverythingOn = countCondition && layers.Values.All(value => value);
            var isNothingOn = countCondition && layers.Values.All(value => !value);

            menu.AddItem(new GUIContent("Everything"), isEverythingOn,
                () => config.Layers.ForEach(layer => SetLayerState(layer, true)));
            menu.AddItem(new GUIContent("Nothing"), isNothingOn,
                () => config.Layers.ForEach(layer => SetLayerState(layer, false)));

            menu.AddSeparator("");
            foreach (var layer in config.Layers)
            {
                var on = true;
                if (!layers.TryAdd(layer.Name, true)) on = layers[layer.Name];

                menu.AddItem(new GUIContent(layer.Name), on, () => SetLayerState(layer, !layers[layer.Name]));
            }

            menu.ShowAsContext();
            return;

            void SetLayerState(Layer layer, bool state)
            {
                if (layers.TryAdd(layer.Name, state)) return;
                layers[layer.Name] = state;
            }
        }
        
        private void DrawSearchField()
        {
            if (!IsSearchDrawn) return;

            var previousSearchText = SearchText ?? "";
            SearchText = GUILayout.TextField(SearchText, new GUIStyle("SearchTextField"),
                GUILayout.MinWidth(20), GUILayout.ExpandWidth(true));

            if (!previousSearchText.Equals(SearchText) || IsCachedResultsAreInvalid())
            {
                UpdateSearchResultList();
                UpdateSearchFocus();
            }
            
            DrawOtherSearchElements();
        }

        private bool IsCachedResultsAreInvalid()
        {
            if (!IsAnyCachedSearchResult()) return false;
            
            var log = SeenEntries;
            if (cachedSearchResult.Count > log.Count) return true;
            if (cachedSearchResult[^1] >= log.Count) return true;

            return false;
        }
        
        private void UpdateSearchResultList()
        {
            var previousIndexOfSelectedEntry = absoluteCachedSearchResult is { Count: > 0 }
                ? absoluteCachedSearchResult[cursorIndex]
                : 0;
            cachedSearchResult = GetFoundEntries(SeenEntries);

            UpdateCachedLogEntries();
            UpdateAbsoluteSearchResult();
            
            var minDistance = int.MaxValue;
            for (int i = 0; i < cachedSearchResult.Count; i++)
            {
                var distance = Math.Abs(absoluteCachedSearchResult[i] - previousIndexOfSelectedEntry);
                    
                if (distance >= minDistance) break;
                minDistance = distance;
                cursorIndex = i;
            }
            return;

            Dictionary<LogEntry, int> GetAbsoluteIndexes()
            {
                var log = ConsoleWindow.Inst.Log;
                var result = new Dictionary<LogEntry, int>();
                for (int i = 0; i < log.Count; i++)
                {
                    result.Add(log[i], i);
                }

                return result;
            }
            
            void UpdateAbsoluteSearchResult()
            {
                absoluteCachedSearchResult = new List<int>();
                var absoluteIndexes = GetAbsoluteIndexes();
                foreach (var entry in cachedSearchResultEntries.Keys)
                {
                    absoluteCachedSearchResult.Add(absoluteIndexes[entry]);
                }
            }
        }
        
        private void DrawOtherSearchElements()
        {
            var foundCount = IsAnyCachedSearchResult() ? cachedSearchResult.Count : 0;
            var currentlyFocusedEntryNum = foundCount == 0 ? 0 : cursorIndex + 1;
            
            var text = foundCount == 0 ? "0 results" : $"{currentlyFocusedEntryNum}/{foundCount}";
            var content = new GUIContent(text, "Count of found occurrences");
            GUILayout.Label(content, GUILayout.ExpandWidth(false));
            
            DrawCaseButton();
            DrawArrows(foundCount);
        }

        private void DrawCaseButton()
        {
            var isButton = caseSensitive;
            var style = isButton ? new GUIStyle("Button") : new GUIStyle("Label");
            style.richText = true;
            style.alignment = TextAnchor.MiddleCenter;

            var content = new GUIContent("<color=white><b>Cc</b></color>", "Match Case");
            if (GUILayout.Button(content, style, GUILayout.Width(27), GUILayout.Height(17)))
            {
                caseSensitive = !caseSensitive;
                UpdateSearchResultList();
                UpdateSearchFocus();
            }
            
            if (isButton) GUILayout.Space(1); 
        }

        private void DrawArrows(int foundCount)
        {
            if (foundCount == 0) return;
            
            var direction = 0;
            if (DrawArrowButton(true)) direction = -1;
            GUILayout.Space(3);
            if (DrawArrowButton(false)) direction = 1;
            GUILayout.Space(1);
            
            if (direction == 0) return;
            
            cursorIndex = (cursorIndex + direction + foundCount) % foundCount;
            UpdateSearchFocus();
            
            return;

            bool DrawArrowButton(bool up)
            {
                var content = up 
                    ? new GUIContent("\u2191", "Previous Occurrence") 
                    : new GUIContent("\u2193", "Next Occurrence");
                return GUILayout.Button(content, ArrowButtonStyle, GUILayout.Width(16), GUILayout.Height(17));
            }
        }
        
        private void UpdateSearchFocus()
        {
            if (!IsAnyCachedSearchResult()) return;

            var index = cachedSearchResult[cursorIndex];
            ConsoleWindow.Inst.SelectedEntry = SeenEntries[index] as LogEntry;

            var logViewerHeight = ConsoleWindow.Inst.LogViewerHeight;
            var lineHeight = ConsoleWindow.LineHeight;
            var scrollPosition = index * lineHeight - (logViewerHeight - lineHeight) / 2f;

            ConsoleWindow.Inst.ScrollPosition = Math.Clamp(scrollPosition, 0, ConsoleWindow.Inst.PreviousTotalHeight);
            ConsoleWindow.Inst.Repaint();
        }

        private bool IsAnyCachedSearchResult() => cachedSearchResult is { Count: > 0 };

        private List<int> GetFoundEntries<T>(List<T> log) where T : ILogEntry
        {
            if (string.IsNullOrEmpty(SearchText)) return new List<int>();
            
            var result = new List<int>();
            for (int i = 0; i < log.Count; i++)
            {
                if (log[i] is not LogEntry entry) continue;
                if (!IsTextMatchSearch(entry.MessageWithoutTags, out _)) continue;
                result.Add(i);
            }

            return result;
        }

        private bool IsTextMatchSearch(string text, out int index)
        {
            var comparisonType = caseSensitive 
                ? StringComparison.CurrentCulture
                : StringComparison.CurrentCultureIgnoreCase;

            index = text.IndexOf(SearchText, comparisonType);
            return index != -1;
        }

        private void UpdateCachedLogEntries()
        {
            cachedSearchResultEntries = new Dictionary<LogEntry, HighlightData>();
            if (!IsAnyCachedSearchResult()) return;

            foreach (var entryIndex in cachedSearchResult)
            {
                if (SeenEntries[entryIndex] is not LogEntry entry) continue;
                IsTextMatchSearch(entry.MessageWithoutTags, out var index);
                
                var highlightData = new HighlightData(index, SearchText.Length);
                cachedSearchResultEntries.Add(entry, highlightData);
            }
        }

        public HighlightData GetHighlightData(LogEntry entry)
        {
            if (cachedSearchResultEntries == null) UpdateCachedLogEntries();
            return cachedSearchResultEntries?.GetValueOrDefault(entry);
        }
        
        private void DrawShownTagsPopup()
        {
            if (!GUILayout.Button("Shown Tags", DropdownStyle, GUILayout.Width(ToolbarWidth[2])))
            {
                return;
            }
            
            var tags = ConsoleWindow.Inst.Tags;
            var menu = new GenericMenu();
            foreach (var pair in tags)
                menu.AddItem(new GUIContent(pair.Key), pair.Value, () => tags[pair.Key] = !pair.Value);
            menu.ShowAsContext();
        }
    }
#endif
}