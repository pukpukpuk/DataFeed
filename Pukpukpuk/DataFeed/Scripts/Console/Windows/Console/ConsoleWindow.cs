using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pukpukpuk.DataFeed.Console.Entries;
using Pukpukpuk.DataFeed.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Serialization;

namespace Pukpukpuk.DataFeed.Console.Windows.Console
{
#if UNITY_EDITOR
    [Serializable]
    public class ConsoleWindow : EditorWindow, IHasCustomMenu
    {
        public static readonly Color HighlightedTextColor = new(.8f, .8f, .5f, .3f);
        
        private const int MaxEntriesCount = 5000;

        public const float TimeColumnWidth = 78f;
        private const float ScrollbarWidth = 30f;
        public const float LineHeight = 22;
        public static ConsoleWindow Inst;
        
        public float LayerColumnWidth = 100f;
        public GUIStyle TextStyle;

        public List<LogEntry> Log = new();
        [SerializeField] public SerializableDictionary<string, bool> Layers = new();
        [SerializeField] public SerializableDictionary<string, bool> Tags = new();
        
        [SerializeField] private SplitterData SplitterData;
        [SerializeField] public float ScrollPosition;
        private float previousScrollPosition;
        private float stackViewerScroll;
        public float LogViewerHeight;
        
        [SerializeField] private ConsoleConfig _config;
        [SerializeField] private List<HiddenEntriesGroup> previousHiddenGroups = new();
        [SerializeField] public bool IsTimeBetweenEnabled;
        [SerializeField] public bool IsHiddenGroupsEnabled;

        [SerializeField] public string SearchText;
        [SerializeField] public ToolbarDrawer ToolbarDrawer;
        
        [NonSerialized] public LogEntry SelectedEntry;
        
        private float autoUpdateCooldown;
        private bool needRepaint;
        public float PreviousTotalHeight;

        public bool ScrollBarIsVisible { get; private set; }
        public List<ILogEntry> SeenLogEntries { get; private set; }
        
        private GUIStyle EvenStyle;
        private GUIStyle OddStyle;
        private GUIStyle StyleOfSelected;
        private GUIStyle StackStyle;
        
        public float WindowWidth => position.width - ScrollbarWidth;
        public float MessageColumnWidth => WindowWidth - LayerColumnWidth - TimeColumnWidth;

        public Rect ScrollBarRect;
        
        private void OnGUI()
        {
            RemoveRedundantEntries();
            
            ToolbarDrawer ??= new ToolbarDrawer();
            ToolbarDrawer.DrawToolbar();

            SplitterData ??= new SplitterData(this, false) { YOffset = LineHeight };

            Splitter.BeginFirstPart(SplitterData, out LogViewerHeight);
            DrawTable(LogViewerHeight);
            Splitter.BeginSecondPart(SplitterData);
            DrawStackInfo();
            Splitter.End();

            ToolbarDrawer.DrawScrollBarMarks();
        }

        private void DrawStackInfo()
        {
            if (SelectedEntry == null) return;

            stackViewerScroll = EditorGUILayout.BeginScrollView(new Vector2(0, stackViewerScroll)).y;

            var text = SelectedEntry.Message + "\n" + SelectedEntry.GetStackWithHyperlinks();
            var height = StackStyle.CalcHeight(new GUIContent(text), position.width - 12);

            EditorGUILayout.SelectableLabel(text, StackStyle,
                GUILayout.ExpandHeight(true), GUILayout.MinHeight(height));
            EditorGUILayout.EndScrollView();
        }

        private void UpdateTitleText()
        {
            titleContent.text = "DataFeed Console";
            if (Log.Any()) titleContent.text += $" ({Log.Count})";
        }

        private void RemoveRedundantEntries()
        {
            while (Log.Count > MaxEntriesCount) Log.RemoveAt(0);
            UpdateTitleText();
        }

        public static ConsoleConfig GetConfig()
        {
            var instanceIsNull = Inst == null;
            if (!instanceIsNull && Inst._config != null) return Inst._config;

            var loadedConfig = Resources.Load<ConsoleConfig>("DataFeed/Config");
            if (!instanceIsNull) Inst._config = loadedConfig;
            return loadedConfig;
        }

        public static void LogToConsole(
            string message, 
            string layerName,
            LogMessageType messageType = LogMessageType.Info,
            string tag = null, 
            string customPrefix = null, 
            string customStack = null)
        {
            if (Inst == null) return;

            var time = Time.realtimeSinceStartup;
            var layerText = customPrefix ?? layerName;
            var stack = customStack ?? StackTraceUtility.ExtractStackTrace();
            var layer = GetConfig().Layers.Find(layer => layer.Name == layerName) ?? Layer.Undefined;

            if (tag != null) Inst.Tags.TryAdd(tag, true);

            var entry = new LogEntry(message, layer, time, Inst)
            {
                MessageType = messageType,
                LayerText = layerText,
                Tag = tag,
                Stack = stack
            };

            Inst.Log.Add(entry);
            Inst.needRepaint = true;
        }

        [MenuItem("Tools/Pukpukpuk/DataFeed Console")]
        private static void Create()
        {
            var window = GetWindow<ConsoleWindow>();

            Inst = window;

            window.titleContent = new GUIContent(GetDataFeedIcon());
            window.UpdateTitleText();
            window.Show();
        }
        
        public static Texture GetDataFeedIcon()
        {
            return Resources.Load<Texture>("DataFeed/Icon");
        }
        
        #region Table

        private void DrawTable(float areaHeight)
        {
            UpdateStyles();
            UpdateTitleText();

            var previousSeenLogEntriesHash = SeenLogEntries?.GetHashCode() ?? 0;
            SeenLogEntries = GetSeenLogEntries();
            
            if (previousSeenLogEntriesHash != SeenLogEntries.GetHashCode()) ToolbarDrawer.SeenEntriesAreNotActual = true;
            
            if (SeenLogEntries.Count == 0) return;
            UpdateLayerColumnWidth(SeenLogEntries);

            var scrollPosition_safe = UpdateAutoScroll(SeenLogEntries, areaHeight);
            var (firstBound, secondBound) = CalculateBounds(scrollPosition_safe, areaHeight, SeenLogEntries);
            ScrollBarIsVisible = areaHeight < PreviousTotalHeight - 1;
            
            GUILayout.Space(firstBound * LineHeight);
            var previousSelected = SelectedEntry;

            for (var i = firstBound; i <= secondBound; i++)
            {
                var entry = SeenLogEntries[i];

                var style = entry == SelectedEntry ? StyleOfSelected : GetStyle(i % 2 == 0);
                entry.Draw(style);
                UpdateSelectedEntry(entry);
            }
            
            GUILayout.Space((SeenLogEntries.Count - secondBound - 1) * LineHeight);

            GUILayout.FlexibleSpace();
            GUILayout.EndScrollView();
            UpdateScrollBarRect();
            
            if (previousSelected != SelectedEntry) Repaint();
        }

        private void UpdateScrollBarRect()
        {
            ScrollBarRect = GUILayoutUtility.GetLastRect();
            ScrollBarRect.y += LineHeight;
            ScrollBarRect.xMin = ScrollBarRect.xMax - 11;
        }

        private float UpdateAutoScroll(List<ILogEntry> seenLogEntries, float areaHeight)
        {
            var currentTotalHeight = seenLogEntries.Count * LineHeight;
            var wasOnBottom = ScrollPosition + areaHeight >= PreviousTotalHeight - 5;
            if (wasOnBottom) ScrollPosition = currentTotalHeight - areaHeight;
            PreviousTotalHeight = currentTotalHeight;
            
            var scrollPosition_safe = ScrollPosition = GUILayout.BeginScrollView(new Vector2(0, ScrollPosition)).y;
            
            if (Event.current.type == EventType.Repaint) scrollPosition_safe = previousScrollPosition;
            else previousScrollPosition = scrollPosition_safe;

            return scrollPosition_safe;
        }

        private (int firstBound, int secondBound) CalculateBounds(
            float scrollPosition_safe, 
            float areaHeight, 
            List<ILogEntry> seenLogEntries)
        {
            var firstBound = Mathf.FloorToInt(scrollPosition_safe / LineHeight);
            var secondBound = Mathf.FloorToInt((scrollPosition_safe + areaHeight) / LineHeight);

            firstBound = Math.Clamp(firstBound, 0, seenLogEntries.Count - 1);
            secondBound = Math.Clamp(secondBound, 0, seenLogEntries.Count - 1);

            return (firstBound, secondBound);
        }

        private void UpdateSelectedEntry(ILogEntry currentlyDrawnEntry)
        {
            var rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SelectedEntry = currentlyDrawnEntry as LogEntry;
                stackViewerScroll = 0;
            }
        }
        
        #region Pure Methods

        [Pure]
        private List<ILogEntry> GetSeenLogEntries()
        {
            var result = new List<ILogEntry>();
            var hiddenGroups = new List<HiddenEntriesGroup>();

            var hiddenElementsCount = 0;
            var isHiddenGroupExpanded = false;
            foreach (var entry in Log)
            {
                var tagCondition = entry.Tag == null || Tags.GetValueOrDefault(entry.Tag, true);
                var layerCondition = Layers.GetValueOrDefault(entry.Layer.Name, true);

                if (!tagCondition || !layerCondition)
                {
                    if (!IsHiddenGroupsEnabled) continue;
                    if (hiddenElementsCount++ == 0)
                    {
                        var expanded = previousHiddenGroups
                            .Where(previousHiddenGroup => previousHiddenGroup.Expanded)
                            .Any(previousHiddenGroup => previousHiddenGroup.FirstEntry == entry);

                        var group = new HiddenEntriesGroup(entry, this);
                        isHiddenGroupExpanded = group.Expanded = expanded;

                        result.Add(group);
                        hiddenGroups.Add(group);
                    }

                    if (isHiddenGroupExpanded) result.Add(entry);
                    continue;
                }

                TryEndHiddenGroup();
                result.Add(entry);
            }

            TryEndHiddenGroup();
            previousHiddenGroups = hiddenGroups;

            if (!IsTimeBetweenEnabled) return result;
            for (var i = 0; i < result.Count; i++)
            {
                var pauseEntry = new PauseEntry(i, result, this);
                if (pauseEntry.isMayBeDrawn()) result.Insert(i + 1, pauseEntry);
            }

            return result;

            void TryEndHiddenGroup()
            {
                if (hiddenElementsCount > 0)
                {
                    hiddenGroups[^1].Count = hiddenElementsCount;
                    hiddenElementsCount = 0;

                    if (isHiddenGroupExpanded) result.Add(new HiddenEntriesGroupEnd(this));
                }
            }
        }

        [Pure]
        private GUIStyle GetStyle(bool isEven)
        {
            return isEven ? EvenStyle : OddStyle;
        }

        #endregion

        private void UpdateStyles()
        {
            OddStyle = TextureUtils.CreateBackground(new Color(.22f, .22f, .22f));
            EvenStyle = TextureUtils.CreateBackground(new Color(.25f, .25f, .25f));
            StyleOfSelected = TextureUtils.CreateBackground(new Color(.17f, .36f, .53f));

            TextStyle = new GUIStyle("Label");
            TextStyle.richText = true;
            TextStyle.wordWrap = false;
            TextStyle.font = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;
            
            StackStyle = new GUIStyle("Label")
            {
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                richText = true,
                font = TextStyle.font
            };
        }

        public void DrawLabel(string text, 
            float width, 
            Color? color = null,
            TextAnchor alignment = TextAnchor.MiddleLeft, 
            string textWithoutTags = null,
            HighlightData highlightData = null)
        {
            color ??= Color.white;
            textWithoutTags ??= StringUtils.RemoveTags(text);

            var styleCopy = new GUIStyle(TextStyle);
            styleCopy.alignment = alignment;
            styleCopy.normal.textColor = styleCopy.focused.textColor = styleCopy.hover.textColor = color.Value;

            var textRect = GUILayoutUtility.GetRect(new GUIContent(textWithoutTags), styleCopy, 
                GUILayout.Width(width), GUILayout.Height(18));

            DrawHighlightRect(textWithoutTags, textRect, styleCopy, highlightData);
            GUI.Label(textRect, new GUIContent(text), styleCopy);
        }

        private void DrawHighlightRect(string text, Rect textRect, GUIStyle style, HighlightData highlightData)
        {
            if (highlightData == null) return;
            if (highlightData.Length <= 0) return;
            
            int index = Math.Clamp(highlightData.Index, 0, text.Length - 1);
            int length = Math.Clamp(highlightData.Length, 0, text.Length - index);

            var beforeHighlightWidth = index > 0 
                ? style.CalcSize(new GUIContent(text[..(index - 1)])).x + 4
                : 0;
            
            var highlightRect = new Rect(textRect);
            highlightRect.xMin += beforeHighlightWidth;

            var highlightWidth = style.CalcSize(new GUIContent(text.Substring(index, length))).x;
            highlightRect.width = highlightWidth - 2;

            highlightRect.yMin += 1;
            highlightRect.yMax -= 1;

            highlightRect.xMax = Math.Clamp(highlightRect.xMax, 0, textRect.xMax);
            
            if (highlightRect.xMax < highlightRect.xMin) return;
            EditorGUI.DrawRect(highlightRect, HighlightedTextColor);
        }

        public void DrawCenteredLabel(string text, GUIStyle style)
        {
            GUILayout.BeginHorizontal(style);
            DrawLabel(text, WindowWidth, null, TextAnchor.MiddleCenter);
            GUILayout.EndHorizontal();
        }

        private void UpdateLayerColumnWidth(List<ILogEntry> visibleEntries)
        {
            var logEntries = visibleEntries.OfType<LogEntry>();
            if (!logEntries.Any()) return;
            LayerColumnWidth = visibleEntries.OfType<LogEntry>().Max(entry => entry.LayerText.Length) *
                (TextStyle.fontSize - 5) + 10;
        }

        #endregion

        #region Events

        public void Awake()
        {
            Log.Clear();
        }

        private void Update()
        {
            if (autoUpdateCooldown > 0)
            {
                autoUpdateCooldown -= Time.deltaTime;
                return;
            }

            if (!needRepaint) return;
            Repaint();
            autoUpdateCooldown = .3f;
            needRepaint = false;
        }

        private void OnEnable()
        {
            Inst = this;
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void OnBeforeSceneLoad()
        {
            Inst.Log?.Clear();
        }

        private void OnDestroy()
        {
            Log.Clear();
            Tags.Clear();
        }

        private void HandleLog(string message, string stacktrace, LogType type)
        {
            if (type is LogType.Log) return;

            var messageType = type == LogType.Warning ? LogMessageType.Warning : LogMessageType.Error;
            LogToConsole(message, "Undefined", messageType, messageType.ToString(),
                messageType.ToString(), stacktrace);
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Export log to .xlsx"), false, () => TableExporter.CreateXLSXFile(Log));
            menu.AddItem(new GUIContent("Export log to .csv"), false, () => TableExporter.CreateCSVFile(Log));

            var config = GetConfig();
            var currentValue = config.AlsoAddTimeBetweenEntries;
            menu.AddItem(new GUIContent("Also Add Time Between Entries"), 
                currentValue, 
                () => config.AlsoAddTimeBetweenEntries = !currentValue);
            
            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Enabled Sub Entries/Time Between"), IsTimeBetweenEnabled,
                () => IsTimeBetweenEnabled = !IsTimeBetweenEnabled);
            menu.AddItem(new GUIContent("Enabled Sub Entries/Hidden Groups"), IsHiddenGroupsEnabled,
                () => IsHiddenGroupsEnabled = !IsHiddenGroupsEnabled);
        }
        
        #endregion
    }
#endif
}