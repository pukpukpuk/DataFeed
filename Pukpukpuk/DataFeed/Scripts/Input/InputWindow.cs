using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pukpukpuk.DataFeed.Console;
using Pukpukpuk.DataFeed.Console.Windows;
using Pukpukpuk.DataFeed.Console.Windows.Console;
using Pukpukpuk.DataFeed.Utils;
using UnityEditor;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Input
{
#if UNITY_EDITOR
    [Serializable]
    public class InputWindow : EditorWindow, IHasCustomMenu
    {
        private const string InputFieldName = "DataFeed InputField";
        public static InputWindow Instance;

        private static readonly Color SelectedColor = new(.74f, .9f, .92f);
        private static GUIStyle ButtonStyle;

        [SerializeField] private string textFieldValue = "";

        private Dictionary<string, Command> commands;
        private Dictionary<string, Command> editorCommands;
        private bool needMoveCursorToEnd;
        private Dictionary<string, Command> SuitableCommands => Application.isPlaying ? commands : editorCommands;

        #region Completions Variables

        private float FirstPartWidth => position.width * .5f;
        private float SecondPartWidth => position.width - FirstPartWidth;
        private bool InputIsFocused => GUI.GetNameOfFocusedControl() == InputFieldName;

        [SerializeField] private float completionsScroll;
        [SerializeField] private List<string> commandBuffer = new();

        [SerializeField] private List<CompletionLine> completionLines = new();
        [SerializeField] private Vector2Int focusPosition = Vector2Int.zero;

        [SerializeField] private string previouslyFocusedControl;
        [SerializeField] public string FocusedCompletionText;

        [SerializeField] private int ReturnCooldown;

        #endregion
        
        private void OnGUI()
        {
            GUI.FocusControl(previouslyFocusedControl);

            UpdateStyles();
            UpdateCommandsLayer();
            UpdateCommandsList();

            DrawInputLine();

            DrawAllCompletions();
            HandleKeyboardEvents();

            previouslyFocusedControl = GUI.GetNameOfFocusedControl();
        }

        private void HandleKeyboardEvents()
        {
            var ev = Event.current;
            if (!hasFocus) return;
            if (!ev.isKey) return;

            var key = ev.keyCode;

            if (key == KeyCode.Return)
            {
                if (ReturnCooldown > 0)
                {
                    ReturnCooldown--;
                    return;
                }

                if (!InputIsFocused) return;
                if (string.IsNullOrWhiteSpace(textFieldValue)) return;
                ExecuteCommand(textFieldValue);
                AddInputToBuffer(textFieldValue);

                textFieldValue = "";
                Repaint();
                return;
            }

            if (ev.type != EventType.KeyDown) return;

            if (key == KeyCode.Tab)
            {
                if (InputIsFocused)
                {
                    focusPosition = Vector2Int.zero;
                    if (completionLines.Count == 0) return;
                    completionLines[0].Completions[0].Focus();
                    return;
                }

                FocusInput();
                needMoveCursorToEnd = true;
            }

            if (InputIsFocused) return;

            var previousFocusPosition = focusPosition;
            var config = ConsoleWindow.GetConfig();
            if (key == config.UpKey || key == KeyCode.UpArrow) MoveFocusPosition(-1, false);
            if (key == config.LeftKey || key == KeyCode.LeftArrow) MoveFocusPosition(-1, true);
            if (key == config.DownKey || key == KeyCode.DownArrow) MoveFocusPosition(1, false);
            if (key == config.RightKey || key == KeyCode.RightArrow) MoveFocusPosition(1, true);

            if (previousFocusPosition == focusPosition) return;
            var newFocused = GetCompletionAtFocus();

            newFocused.Focus();
            Repaint();
        }

        private void MoveCursorToEnd()
        {
            var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            editor.cursorIndex = int.MaxValue;
            editor.SelectNone();
        }

        [MenuItem("Tools/Pukpukpuk/DataFeed Input #u")]
        private static void Create()
        {
            var window = GetWindow<InputWindow>();

            Instance = window;

            window.titleContent = new GUIContent("DataFeed Input", ConsoleWindow.GetDataFeedIcon());
            window.Show();
        }

        private static void Log(object message, LogMessageType messageType = LogMessageType.Info)
        {
            ConsoleWindow.LogToConsole(message.ToString(), "Commands", messageType);
        }
        
        #region Input

        private void DrawInputLine()
        {
            GUI.SetNextControlName(InputFieldName);
            textFieldValue = GUILayout.TextField(textFieldValue);

            if (needMoveCursorToEnd) MoveCursorToEnd();
            needMoveCursorToEnd = false;
        }

        public void ExecuteStartUpCommands()
        {
            var enumerable = ConsoleWindow.GetConfig().GameStartCommands
                .Where(command => !string.IsNullOrWhiteSpace(command))
                .Where(command => !command.StartsWith('#'));
            foreach (var command in enumerable)
            {
                ExecuteCommand(command);
            }
        }

        public void ExecuteCommand(string input)
        {
            UpdateCommandsList();

            var firstSpaceIndex = input.IndexOf(' ');
            if (firstSpaceIndex == -1) firstSpaceIndex = input.Length;
            var commandAlias = input[..firstSpaceIndex];

            if (!SuitableCommands.TryGetValue(commandAlias, out var command))
            {
                Log($"Command {commandAlias} doesn't exists!", LogMessageType.Error);
                return;
            }

            var args = (firstSpaceIndex < input.Length ? input[(firstSpaceIndex + 1)..] : "").Split(' ');
            var output = command.Execute(args, out var isError);
            Log(output, isError ? LogMessageType.Error : LogMessageType.Info);
        }

        private void AddInputToBuffer(string input)
        {
            commandBuffer.Add(input);
            while (commandBuffer.Count > ConsoleWindow.GetConfig().MaxBufferSize) commandBuffer.RemoveAt(0);
        }

        private void FocusInput()
        {
            GUI.FocusControl(InputFieldName);
            focusPosition = Vector2Int.zero;
        }

        #endregion

        #region Completions

        private void DrawAllCompletions()
        {
            completionLines.Clear();
            completionsScroll = GUILayout.BeginScrollView(new Vector2(0, completionsScroll),
                false, true).y;
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true), GUILayout.MaxWidth(position.width));

            // Левая часть
            var leftCompletions = GetCompletionsForCommands(out var altText);
            leftCompletions.Sort();
            DrawCompletionsList(leftCompletions, altText, FirstPartWidth, false);

            // Правая часть
            var rightCompletions = GetCompletionsFromBuffer();
            rightCompletions.RemoveAll(completion => leftCompletions.Contains(completion));
            DrawCompletionsList(rightCompletions, "There are no completions from buffer",
                SecondPartWidth - 12, true);

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            if (completionLines.Count == 0) FocusInput();
        }

        private void DrawCompletionsList(List<string> completions, string altText, float width, bool fullyReplaceInput)
        {
            if (completions.Count == 0)
            {
                GUILayout.Label(altText, GUILayout.MaxWidth(width - 12));
                return;
            }

            var lineWidth = width - 6 - 6;

            var remainWidth = lineWidth;
            var lineIndex = 0;
            var isFirst = true;

            GUILayout.BeginVertical(GUILayout.Width(width - 6));
            GUILayout.BeginHorizontal();
            foreach (var text in completions.Distinct())
            {
                var content = new GUIContent(text);
                var minCompletionWidth = ButtonStyle.CalcSize(content).x;

                // Переход на следующую строку
                if (minCompletionWidth > remainWidth && !isFirst)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();

                    remainWidth = lineWidth;
                    lineIndex++;

                    isFirst = true;
                }

                // Покраска кнопки в синий, если она сейчас выделена
                var previousColor = GUI.color;
                if (!InputIsFocused && focusPosition.y == lineIndex && text == FocusedCompletionText)
                {
                    GUI.color = SelectedColor;
                    // Долистываем до выбранного завершения
                    var lineBottomPosition = lineIndex * 22;
                    var lineUpPosition = lineBottomPosition - 22;

                    var scrollBottomPosition = completionsScroll + position.height - 22;

                    if (scrollBottomPosition < lineBottomPosition) completionsScroll = lineBottomPosition;
                    else if (completionsScroll > lineUpPosition) completionsScroll = lineUpPosition;

                    completionsScroll = Math.Clamp(completionsScroll, 0, float.MaxValue);
                }

                var maxWidth = isFirst ? lineWidth + 6 : remainWidth;
                GUI.SetNextControlName(GetNameForCompletion(text));
                if (GUILayout.Button(content, ButtonStyle, GUILayout.MinWidth(minCompletionWidth),
                        GUILayout.MaxWidth(maxWidth), GUILayout.ExpandWidth(true)))
                    OnCompletionClick(text, fullyReplaceInput);

                // Заполнение списка completionLines
                var rect = GUILayoutUtility.GetLastRect();
                var completion = new Completion(text, rect.x, rect.x + rect.width);

                if (lineIndex > completionLines.Count - 1) completionLines.Add(new CompletionLine());
                completionLines[lineIndex].Completions.Add(completion);

                remainWidth -= minCompletionWidth + 4;
                isFirst = false;
                GUI.color = previousColor;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        public static string GetNameForCompletion(string completion)
        {
            return $"DataFeed {completion} Completion";
        }

        private List<string> GetCompletionsForCommands(out string altText)
        {
            // Если в сплите ток одно слово - значит юзверь пока еще пишет только название команды
            var split = textFieldValue.Split(' ');
            if (split.Length <= 1)
            {
                altText = SuitableCommands.Count == 0
                    ? "No class found that extends Command class"
                    : "There are no commands matching the input";
                return SuitableCommands.Keys
                    .Where(alias => alias.Contains(split[0], StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
            }

            if (!SuitableCommands.TryGetValue(split[0], out var command))
            {
                altText = "There is no command with this alias";
                return new List<string>();
            }

            var args = split.Skip(1).ToList();
            var lastArgument = args[^1];
            var lastArgumentIndex = args.Count - 1;

            altText = $"The list of completions provided by {command.GetType().Name} command is empty";
            return command.GetCompletions(lastArgument, lastArgumentIndex, args);
        }

        private List<string> GetCompletionsFromBuffer()
        {
            return commandBuffer
                .Where(input => input.StartsWith(textFieldValue, StringComparison.CurrentCultureIgnoreCase))
                .ToList().Invert().ToList();
        }

        private void OnCompletionClick(string completion, bool fullyReplaceInput)
        {
            if (Event.current.keyCode == KeyCode.Return) ReturnCooldown = 1;

            if (fullyReplaceInput)
            {
                textFieldValue = completion;
                FocusInput();
            }
            else
            {
                var spaceIndex = textFieldValue.LastIndexOf(' ');
                textFieldValue = textFieldValue[..(spaceIndex + 1)] + completion + " ";
            }

            needMoveCursorToEnd = true;
        }

        private void MoveFocusPosition(int shift, bool horizontally)
        {
            shift = Math.Sign(shift);
            var linesCount = completionLines.Count;

            if (linesCount == 0) return;

            if (horizontally)
            {
                var line = Math.Clamp(focusPosition.y, 0, linesCount - 1);
                var count = completionLines[line].Completions.Count;
                focusPosition.x = (focusPosition.x + shift + count) % count;
                return;
            }

            var newLineIndex = (focusPosition.y + shift + linesCount) % linesCount;

            // Если получилось найти кнопку рядом с текущей - перемещаемся на линию с индексом newLineIndex
            var current = GetCompletionAtFocus();
            var next = completionLines[newLineIndex].GetNearestFor(current);

            if (next == null)
            {
                newLineIndex = 0;
                next = completionLines[newLineIndex].GetNearestFor(current);
            }

            var x = completionLines[newLineIndex].Completions.IndexOf(next);
            focusPosition = new Vector2Int(x, newLineIndex);
        }

        private Completion GetCompletionAtFocus()
        {
            return completionLines[focusPosition.y % completionLines.Count].Completions[focusPosition.x];
        }

        #endregion

        #region Events

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Clear Command Buffer"), false, () => commandBuffer.Clear());
        }

        private void OnEnable()
        {
            Instance = this;
        }

        #endregion

        #region Initialization

        private static void UpdateStyles()
        {
            ButtonStyle = new GUIStyle("Button")
            {
                stretchWidth = true
            };
        }

        private static void UpdateCommandsLayer()
        {
            var config = ConsoleWindow.GetConfig().Layers;
            if (config.Any(layer => layer.Name == "Commands")) return;

            var layer = new Layer("Commands", "#B2A4EF");
            config.Insert(0, layer);
        }

        private void UpdateCommandsList()
        {
            if (commands != null && editorCommands != null) return;

            var tuples = TypeUtils.GetAllSubclasses<Command>()
                .Select(type => (type, info: type.GetCustomAttribute<CommandInfoAttribute>(true)))
                .Where(tuple => tuple.info != null)
                .Select(tuple => (command: (Command)Activator.CreateInstance(tuple.type), tuple.info));

            commands = new Dictionary<string, Command>();
            editorCommands = new Dictionary<string, Command>();
            foreach (var (command, info) in tuples)
            {
                commands.Add(info.Alias, command);
                if (!info.IsOnlyForGame) editorCommands.Add(info.Alias, command);
            }
        }

        #endregion
    }
#endif
}