using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pukpukpuk.DataFeed.Console.Windows
{
    [CreateAssetMenu(menuName = "DataFeed/Console Config")]
    [Serializable]
    public class ConsoleConfig : ScriptableObject
    {
        public int MaxEntries = 5000;
        public int MaxBufferSize = 20;
        public List<Layer> Layers = new();

        public List<string> GameStartCommands = new();

        public KeyCode UpKey = KeyCode.U;
        public KeyCode LeftKey = KeyCode.H;
        public KeyCode DownKey = KeyCode.J;
        public KeyCode RightKey = KeyCode.K;

        public string ExportTableFont = "Consolas";
        [FormerlySerializedAs("AddTimeBetweenEntries")] public bool AlsoAddTimeBetweenEntries = true;
    }

    [Serializable]
    public class Layer
    {
        public static readonly Layer Undefined = new("Undefined", "#FFFFFF");

        public string Name;
        public string Hex;

        public Layer(string name, string hex)
        {
            Name = name;
            Hex = hex;
        }

        public Color GetColor()
        {
            ColorUtility.TryParseHtmlString(Hex, out var color);
            return color;
        }
    }
}