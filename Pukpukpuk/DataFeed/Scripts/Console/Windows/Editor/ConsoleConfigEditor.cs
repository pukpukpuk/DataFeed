using System;
using System.Collections.Generic;
using System.Linq;
using Pukpukpuk.DataFeed.Console.Windows.Console;
using UnityEditor;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Console.Windows
{
    [CustomEditor(typeof(ConsoleConfig))]
    public class ConsoleConfigEditor : UnityEditor.Editor
    {
        private const float RemoveButtonWidth = 25f;
        private static GUIStyle BoxStyle;
        private readonly List<Action> DelayedModifications = new();

        private ConsoleConfig _consoleConfig;

        public void OnEnable()
        {
            _consoleConfig = (ConsoleConfig)target;
        }

        public override void OnInspectorGUI()
        {
            UpdateStyles();

            GUILayout.BeginVertical(BoxStyle);
            DrawConsoleSettings();
            GUILayout.EndVertical();

            EditorGUILayout.Separator();

            GUILayout.BeginVertical(BoxStyle);
            DrawInputSettings();
            GUILayout.EndVertical();

            EditorGUILayout.Separator();

            GUILayout.BeginVertical(BoxStyle);
            DrawLogExportSettings();
            GUILayout.EndVertical();
            
            SaveIfNeeded();
        }

        private void DrawConsoleSettings()
        {
            _consoleConfig.MaxEntries = EditorGUILayout.IntField("Max Entries Count", _consoleConfig.MaxEntries);
            DrawTable();
        }

        private void DrawInputSettings()
        {
            _consoleConfig.MaxBufferSize = EditorGUILayout.IntField("Max Buffer Size",
                _consoleConfig.MaxBufferSize);
            DrawStartupCommands();
            DrawKeyFields();
        }

        private void DrawLogExportSettings()
        {
            _consoleConfig.ExportTableFont = EditorGUILayout.TextField("Export Table Font",
                _consoleConfig.ExportTableFont);
            _consoleConfig.AlsoAddTimeBetweenEntries = EditorGUILayout.Toggle("Also Add Time Between Entries",
                _consoleConfig.AlsoAddTimeBetweenEntries);
        }
        
        private void DrawKeyFields()
        {
            GUILayout.BeginVertical(BoxStyle);
            EditorGUILayout.LabelField("Alternative Completion Move Keys");
            _consoleConfig.UpKey = DrawKeyField("Up", _consoleConfig.UpKey);
            _consoleConfig.LeftKey = DrawKeyField("Left", _consoleConfig.LeftKey);
            _consoleConfig.DownKey = DrawKeyField("Down", _consoleConfig.DownKey);
            _consoleConfig.RightKey = DrawKeyField("Right", _consoleConfig.RightKey);
            GUILayout.EndVertical();
        }

        private KeyCode DrawKeyField(string label, KeyCode previous)
        {
            var text = EditorGUILayout.DelayedTextField(label, previous.ToString());
            return Enum.TryParse(text, true, out KeyCode keyCode) ? keyCode : KeyCode.None;
        }

        private void DrawStartupCommands()
        {
            var previousText = string.Join("\n", _consoleConfig.GameStartCommands);

            GUILayout.BeginVertical(BoxStyle);
            EditorGUILayout.LabelField("Game Start-up Commands");
            var newText = EditorGUILayout.TextArea(previousText);
            GUILayout.EndVertical();

            if (newText == previousText) return;
            _consoleConfig.GameStartCommands = newText.Split("\n").ToList();
        }

        private void SaveIfNeeded()
        {
            if (GUI.changed)
            {
                EditorUtility.SetDirty(_consoleConfig);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawTable()
        {
            GUILayout.Label("Layers");
            GUILayout.BeginVertical(BoxStyle);

            DrawTableHeader();
            _consoleConfig.Layers.ForEach(DrawEntry);

            DrawAddElementButton();
            DelayedModifications.ForEach(action => action.Invoke());
            DelayedModifications.Clear();
            GUILayout.EndVertical();
        }

        private void DrawAddElementButton()
        {
            if (GUILayout.Button("Add element"))
                DelayedModifications.Add(() => _consoleConfig.Layers.Add(new Layer("New Layer", "#FFFFFF")));
        }

        private void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Name");
            GUILayout.Label("Color");
            GUILayout.Space(RemoveButtonWidth);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEntry(Layer layer)
        {
            GUILayout.BeginHorizontal();

            layer.Name = EditorGUILayout.TextField(layer.Name);
            layer.Hex = "#" + ColorUtility.ToHtmlStringRGB(EditorGUILayout.ColorField(layer.GetColor()));

            if (GUILayout.Button("\u2501", GUILayout.Width(RemoveButtonWidth)))
                DelayedModifications.Add(() => _consoleConfig.Layers.Remove(layer));

            GUILayout.EndHorizontal();
        }

        private static void UpdateStyles()
        {
            BoxStyle ??= new GUIStyle("HelpBox") { padding = new RectOffset(10, 10, 5, 5) };
        }
        
        [MenuItem("Tools/Pukpukpuk/Open DataFeed Config")]
        private static void Create()
        {
            var config = ConsoleWindow.GetConfig();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

    }
}