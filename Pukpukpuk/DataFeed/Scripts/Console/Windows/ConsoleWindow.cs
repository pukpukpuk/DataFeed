using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pukpukpuk.DataFeed.Console.Entries;
using Pukpukpuk.DataFeed.Utils;
using UnityEditor;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Console.Windows
{
#if UNITY_EDITOR
    [Serializable]
    public class ConsoleWindow : EditorWindow
    {
        private const int MaxEntriesCount = 5000;

        public const float TimeColumnWidth = 78f;
        private const float ScrollbarWidth = 30f;
        public const float LineHeight = 22;
        public static ConsoleWindow Instance;

        private static float[] ToolbarWidth = { 45f, 105f, 95f, 140f };
        public float LayerColumnWidth = 100f;
        public GUIStyle TextStyle;

        public List<LogEntry> Log = new();
        [SerializeField] private SerializableDictionary<string, bool> tags = new();

        [SerializeField] private float scrollPosition;
        [SerializeField] private SplitterData SplitterData;
        [SerializeField] private SerializableDictionary<string, bool> layers = new();
        [SerializeField] private ConsoleConfig _config;

        [SerializeField] private List<HiddenEntriesGroup> previousHiddenGroups = new();

        [SerializeField] private bool isTimeBetweenEnabled;
        [SerializeField] private bool isHiddenGroupsEnabled;

        [SerializeField] private string searchText;
        private float autoUpdateCooldown;
        private GUIStyle EvenStyle;

        private bool needRepaint;

        private GUIStyle OddStyle;
        private float previousScrollPosition;

        private float previousTotalHeight;

        private LogEntry selectedEntry;
        private GUIStyle SelectedStyle;
        private GUIStyle StackStyle;
        private float stackViewerScroll;
        private float SearchFieldWidth => Instance.WindowWidth - ToolbarWidth.Sum() + 25;

        public float WindowWidth => position.width - ScrollbarWidth;
        public float MessageColumnWidth => WindowWidth - LayerColumnWidth - TimeColumnWidth;

        private void OnGUI()
        {
            RemoveRedundantEntries();
            DrawToolbar();

            SplitterData ??= new SplitterData(this, false) { YOffset = LineHeight };

            Splitter.BeginFirstPart(SplitterData, out var height);
            DrawTable(height);
            Splitter.BeginSecondPart(SplitterData);
            DrawStackInfo();
            Splitter.End();
        }

        private void DrawStackInfo()
        {
            if (selectedEntry == null) return;

            stackViewerScroll = EditorGUILayout.BeginScrollView(new Vector2(0, stackViewerScroll)).y;

            var text = selectedEntry.Message + "\n" + selectedEntry.GetStackWithHyperlinks();
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
            if (Instance != null && Instance._config != null) return Instance._config;

            var loadedConfig = Resources.Load<ConsoleConfig>("DataFeed/Config");
            if (Instance != null) Instance._config = loadedConfig;
            return loadedConfig;
        }

        public static void LogToConsole(string message, string layerName,
            LogMessageType messageType = LogMessageType.Info,
            string tag = null, string customPrefix = null, string customStack = null)
        {
            if (Instance == null) return;

            var time = Time.realtimeSinceStartup;
            var layerText = customPrefix ?? layerName;
            var stack = customStack ?? StackTraceUtility.ExtractStackTrace();
            var layer = GetConfig().Layers.Find(layer => layer.Name == layerName) ?? Layer.Undefined;

            if (tag != null) Instance.tags.TryAdd(tag, true);

            var entry = new LogEntry(message, layer, time, Instance)
            {
                MessageType = messageType,
                LayerText = layerText,
                Tag = tag,
                Stack = stack
            };

            Instance.Log.Add(entry);
            Instance.needRepaint = true;
        }

        [MenuItem("Tools/Pukpukpuk/DataFeed Console")]
        private static void Create()
        {
            var window = GetWindow<ConsoleWindow>();

            Instance = window;

            window.titleContent = new GUIContent(GetDataFeedIcon());
            window.UpdateTitleText();
            window.Show();
        }

        public static Texture GetDataFeedIcon()
        {
            return Resources.Load<Texture>("DataFeed/Icon");
        }

        #region Toolbar

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear", new GUIStyle("TE toolbarbutton"),
                    GUILayout.Width(ToolbarWidth[0])))
            {
                Log.Clear();
                tags.Clear();
                selectedEntry = null;
            }

            DrawShownLayersPopup();
            DrawShownTagsPopup();
            DrawSearchField();
            DrawSubEntries();

            GUILayout.EndHorizontal();
        }

        private void DrawShownLayersPopup()
        {
            if (GUILayout.Button("Shown Layers", new GUIStyle("ToolbarDropDownLeft"),
                    GUILayout.Width(ToolbarWidth[1])))
            {
                var menu = new GenericMenu();

                var countCondition = layers.Count == GetConfig().Layers.Count;
                var isEverythingOn = countCondition && layers.Values.All(value => value);
                var isNothingOn = countCondition && layers.Values.All(value => !value);

                menu.AddItem(new GUIContent("Everything"), isEverythingOn,
                    () => GetConfig().Layers.ForEach(layer => SetLayerState(layer, true)));
                menu.AddItem(new GUIContent("Nothing"), isNothingOn,
                    () => GetConfig().Layers.ForEach(layer => SetLayerState(layer, false)));

                menu.AddSeparator("");
                foreach (var layer in GetConfig().Layers)
                {
                    var on = true;
                    if (!layers.TryAdd(layer.Name, true)) on = layers[layer.Name];

                    menu.AddItem(new GUIContent(layer.Name), on, () => SetLayerState(layer, !layers[layer.Name]));
                }

                menu.ShowAsContext();
            }
        }

        private void SetLayerState(Layer layer, bool state)
        {
            if (layers.TryAdd(layer.Name, state)) return;
            layers[layer.Name] = state;
        }

        private void DrawSearchField()
        {
            var width = SearchFieldWidth;
            if (width < 10) return;
            searchText = GUILayout.TextField(searchText, new GUIStyle("SearchTextField"),
                GUILayout.Width(width));
        }

        private void DrawShownTagsPopup()
        {
            if (GUILayout.Button("Shown Tags", new GUIStyle("ToolbarDropDownLeft"),
                    GUILayout.Width(ToolbarWidth[2])))
            {
                var menu = new GenericMenu();
                foreach (var pair in tags)
                    menu.AddItem(new GUIContent(pair.Key), pair.Value, () => tags[pair.Key] = !pair.Value);
                menu.ShowAsContext();
            }
        }

        private void DrawSubEntries()
        {
            if (GUILayout.Button("Enabled Sub Entries", new GUIStyle("ToolbarDropDownRight"),
                    GUILayout.Width(ToolbarWidth[3])))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Time Between"), isTimeBetweenEnabled,
                    () => isTimeBetweenEnabled = !isTimeBetweenEnabled);
                menu.AddItem(new GUIContent("Hidden Groups"), isHiddenGroupsEnabled,
                    () => isHiddenGroupsEnabled = !isHiddenGroupsEnabled);
                menu.ShowAsContext();
            }
        }

        #endregion

        #region Table

        private void DrawTable(float areaHeight)
        {
            UpdateStyles();
            UpdateTitleText();

            var seenLogEntries = !string.IsNullOrWhiteSpace(searchText) ? GetSearchedLogEntries() : GetSeenLogEntries();
            if (seenLogEntries.Count == 0) return;
            UpdateLayerColumnWidth(seenLogEntries);

            #region AutoScroll

            // Прикрепляемся ко дну, если количество записей изменилось и скролл и так был на дне
            var currentTotalHeight = seenLogEntries.Count * LineHeight;
            var wasOnBottom = scrollPosition + areaHeight >= previousTotalHeight - LineHeight;
            if (wasOnBottom) scrollPosition = currentTotalHeight - areaHeight;

            var scrollPosition_safe = scrollPosition = GUILayout.BeginScrollView(new Vector2(0, scrollPosition)).y;

            if (Event.current.type == EventType.Repaint) scrollPosition_safe = previousScrollPosition;
            else previousScrollPosition = scrollPosition_safe;

            #endregion

            // Из координат границ видимой части получаем индексы крайних видимых записей
            var firstBound = Mathf.FloorToInt(scrollPosition_safe / LineHeight);
            var secondBound = Mathf.FloorToInt((scrollPosition_safe + areaHeight) / LineHeight);

            firstBound = Math.Clamp(firstBound, 0, seenLogEntries.Count - 1);
            secondBound = Math.Clamp(secondBound, 0, seenLogEntries.Count - 1);

            // Вместо невидимых записей рисуем пробелы, чтобы не менять геометрию интерфейса
            // Пробел, заменяющий записи до видимых
            GUILayout.Space(firstBound * LineHeight);

            var previousSelected = selectedEntry;

            for (var i = firstBound; i <= secondBound; i++)
            {
                var entry = seenLogEntries[i];

                var style = entry == selectedEntry ? SelectedStyle : GetStyle(i % 2 == 0);
                entry.Draw(style);

                var rect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    selectedEntry = entry as LogEntry;
                    stackViewerScroll = 0;
                }
            }

            // Пробел, заменяющий записи после видимых
            GUILayout.Space((seenLogEntries.Count - secondBound - 1) * LineHeight);

            GUILayout.FlexibleSpace();
            previousTotalHeight = currentTotalHeight;
            GUILayout.EndScrollView();

            if (previousSelected != selectedEntry) Repaint();
        }

        #region Pure Methods

        [Pure]
        private List<ILogEntry> GetSearchedLogEntries()
        {
            var temp = searchText.Trim().ToLower();
            var result = Log
                .Where(entry => entry.MessageWithoutTags.Contains(temp, StringComparison.CurrentCultureIgnoreCase))
                .Cast<ILogEntry>()
                .ToList();

            return result;
        }

        [Pure]
        private List<ILogEntry> GetSeenLogEntries()
        {
            var result = new List<ILogEntry>();
            var hiddenGroups = new List<HiddenEntriesGroup>();

            var hiddenElementsCount = 0;
            var isHiddenGroupExpanded = false;
            foreach (var entry in Log)
            {
                var tagCondition = entry.Tag == null || tags.GetValueOrDefault(entry.Tag, true);
                var layerCondition = layers.GetValueOrDefault(entry.Layer.Name, true);

                if (!tagCondition || !layerCondition)
                {
                    if (!isHiddenGroupsEnabled) continue;
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

            if (isTimeBetweenEnabled)
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
            SelectedStyle = TextureUtils.CreateBackground(new Color(.17f, .36f, .53f));

            TextStyle = new GUIStyle("Label");
            TextStyle.richText = true;
            TextStyle.font = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;

            StackStyle = new GUIStyle("Label")
            {
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                richText = true,
                font = TextStyle.font
            };
        }

        public void DrawLabel(string text, float width, Color? color = null,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            color ??= Color.white;

            var copy = new GUIStyle(TextStyle);

            copy.alignment = alignment;
            copy.normal.textColor = copy.focused.textColor = copy.hover.textColor = color.Value;

            GUILayout.Label(text, copy, GUILayout.Width(width), GUILayout.Height(18));
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
            Instance = this;
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void OnBeforeSceneLoad()
        {
            Instance.Log.Clear();
        }

        private void OnDestroy()
        {
            Log.Clear();
            tags.Clear();
        }

        private void HandleLog(string message, string stacktrace, LogType type)
        {
            if (type is LogType.Log) return;

            var messageType = type == LogType.Warning ? LogMessageType.Warning : LogMessageType.Error;
            LogToConsole(message, "Undefined", messageType, messageType.ToString(),
                messageType.ToString(), stacktrace);
        }

        #endregion
    }
#endif
}