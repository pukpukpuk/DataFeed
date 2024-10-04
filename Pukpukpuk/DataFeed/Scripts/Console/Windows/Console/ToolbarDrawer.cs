using System;
using System.Collections.Generic;
using System.Linq;
using Pukpukpuk.DataFeed.Console.Entries;
using Pukpukpuk.DataFeed.Utils;
using UnityEditor;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Console.Windows.Console
{
    [Serializable]
    public class ToolbarDrawer
    {
        private static readonly float[] ToolbarWidth = { 45f, 105f, 95f };
        private const float MinSearchWidth = 140;
        
        [SerializeField] private List<int> cachedSearchResult;
        [SerializeField] private int cursorIndex;

        [SerializeField] private bool caseSensitive;
        
        private HashSet<LogEntry> cachedSearchResultEntries;
        
        private static GUIStyle ArrowButtonStyle => new("ButtonMid");
        private static GUIStyle ButtonStyle => new("TE toolbarbutton");
        private static GUIStyle DropdownStyle => new ("ToolbarDropDownLeft");
        private static float SearchFieldWidth => ConsoleWindow.Inst.WindowWidth - ToolbarWidth.Sum() + 25;
        private static bool IsSearchDrawn => SearchFieldWidth >= MinSearchWidth;
        
        private static string SearchText
        {
            get => ConsoleWindow.Inst.SearchText;
            set => ConsoleWindow.Inst.SearchText = value;
        }
        
        public void DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            
            DrawClearButton();
            DrawShownLayersPopup();
            DrawShownTagsPopup();
            DrawSearchField();

            GUILayout.EndHorizontal();
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

            var previousSearchText = SearchText;
            SearchText = GUILayout.TextField(SearchText, new GUIStyle("SearchTextField"),
                GUILayout.MinWidth(20), GUILayout.ExpandWidth(true));

            if (!previousSearchText.Equals(SearchText) || IsCachedResultsAreInvalid()) UpdateSearchResultList();
            DrawOtherSearchElements();
        }

        private bool IsCachedResultsAreInvalid()
        {
            if (!IsAnyCachedSearchResult()) return false;
            
            var log = ConsoleWindow.Inst.Log;
            if (cachedSearchResult.Count > log.Count) return true;
            if (cachedSearchResult[^1] >= log.Count) return true;

            return false;
        }
        
        private void UpdateSearchResultList()
        {
            var previousIndexOfSelectedEntry = IsAnyCachedSearchResult() 
                ? cachedSearchResult[cursorIndex]
                : 0;
            cachedSearchResult = GetFoundEntries();
            UpdateCachedLogEntries();

            var minDistance = int.MaxValue;
            for (int i = 0; i < cachedSearchResult.Count; i++)
            {
                var distance = Math.Abs(cachedSearchResult[i] - previousIndexOfSelectedEntry);
                    
                if (distance >= minDistance) break;
                minDistance = distance;
                cursorIndex = i;
            }
                
            UpdateSearchFocus();
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
            if (GUILayout.Button(content, style, GUILayout.Width(26), GUILayout.Height(17)))
            {
                caseSensitive = !caseSensitive;
                UpdateSearchResultList();
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
            ConsoleWindow.Inst.SelectedEntry = ConsoleWindow.Inst.Log[index];
            var scrollPosition = index * ConsoleWindow.LineHeight - (ConsoleWindow.Inst.LogViewerHeight - ConsoleWindow.LineHeight) / 2f;

            ConsoleWindow.Inst.ScrollPosition = Math.Clamp(scrollPosition, 0, ConsoleWindow.Inst.PreviousTotalHeight);
            ConsoleWindow.Inst.Repaint();
        }

        private bool IsAnyCachedSearchResult() => cachedSearchResult is { Count: > 0 };

        private List<int> GetFoundEntries()
        {
            if (string.IsNullOrEmpty(SearchText)) return new List<int>();
            
            var temp = SearchText.Trim();
            var comparisonType = caseSensitive 
                ? StringComparison.CurrentCulture
                : StringComparison.CurrentCultureIgnoreCase;
            
            var result = new List<int>();
            var log = ConsoleWindow.Inst.Log;
            for (int i = 0; i < log.Count; i++)
            {
                var entry = log[i];
                if (!entry.MessageWithoutTags.Contains(temp, comparisonType)) continue;
                result.Add(i);
            }

            return result;
        }

        private void UpdateCachedLogEntries()
        {
            cachedSearchResultEntries = new HashSet<LogEntry>();
            if (!IsAnyCachedSearchResult()) return;

            foreach (var index in cachedSearchResult)
            {
                cachedSearchResultEntries.Add(ConsoleWindow.Inst.Log[index]);
            }
        }

        public bool IsHighlighted(LogEntry entry)
        {
            return cachedSearchResultEntries != null && cachedSearchResultEntries.Contains(entry);
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
}