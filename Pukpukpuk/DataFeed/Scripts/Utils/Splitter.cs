using System;
using UnityEditor;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Utils
{
    public static class Splitter
    {
#if UNITY_EDITOR
        private static readonly Color Color = new(.14f, .14f, .14f);
        private const float Thickness = 1f;
        private const float HitBoxThickness = 5f;

        public static void BeginFirstPart(SplitterData data, out float height)
        {
            var rect = data.Horizontal
                ? new Rect(0, 0, data.LeftWidth, data.Window.position.height)
                : new Rect(0, 0, data.Window.position.width, data.UpperHeight);

            rect.y += data.YOffset;
            rect.height -= data.YOffset;

            height = rect.height;
            GUILayout.BeginArea(rect);
        }

        private static void EndFirstPart(SplitterData data)
        {
            GUILayout.EndArea();

            // Рисуем границу деления
            var dividerRect = data.Horizontal
                ? new Rect(data.LeftWidth - Thickness / 2f, 0, Thickness, data.Window.position.height)
                : new Rect(0, data.UpperHeight - Thickness / 2f, data.Window.position.width, Thickness);

            EditorGUI.DrawRect(dividerRect, Color);

            var dividerHitBoxRect = data.Horizontal
                ? new Rect(dividerRect.x - HitBoxThickness / 2f, 0,
                    dividerRect.width + HitBoxThickness, data.Window.position.height)
                : new Rect(0, dividerRect.y - HitBoxThickness / 2f,
                    data.Window.position.width, dividerRect.height + HitBoxThickness);

            EditorGUIUtility.AddCursorRect(dividerHitBoxRect,
                data.Horizontal ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical);

            // Обрабатываем события мыши для перемещения границы
            if (Event.current.type == EventType.MouseDown && dividerHitBoxRect.Contains(Event.current.mousePosition))
                data.IsDragging = true;
            if (data.IsDragging && Event.current.type == EventType.MouseDrag)
            {
                var factor = 1 - data.YOffset / data.Window.position.height;

                var newPosition = data.Horizontal
                    ? Event.current.mousePosition.x / data.Window.position.width
                    : Event.current.mousePosition.y * factor / (data.Window.position.height - data.YOffset);
                data.DividerRelativePosition =
                    Mathf.Clamp(newPosition, data.DividerMinPosition, data.DividerMaxPosition);
                data.Window.Repaint();
            }

            if (Event.current.type == EventType.MouseUp) data.IsDragging = false;
        }

        public static void BeginSecondPart(SplitterData data)
        {
            EndFirstPart(data);
            var rect = data.Horizontal
                ? new Rect(data.LeftWidth, 0, data.RightWidth, data.Window.position.height)
                : new Rect(0, data.UpperHeight, data.Window.position.width, data.LowerHeight);

            GUILayout.BeginArea(rect);
        }

        public static void End()
        {
            GUILayout.EndArea();
        }
#endif
    }

#if UNITY_EDITOR
    [Serializable]
    public class SplitterData
    {
        public EditorWindow Window;

        public bool Horizontal;
        public float DividerRelativePosition = .5f;

        public float DividerMinPosition;
        public float DividerMaxPosition;

        public bool IsDragging;

        public float YOffset;

        public SplitterData(EditorWindow window, bool horizontal, float dividerMinPosition = .2f,
            float dividerMaxPosition = .8f)
        {
            Window = window;
            Horizontal = horizontal;
            DividerMinPosition = dividerMinPosition;
            DividerMaxPosition = dividerMaxPosition;
        }

        public float UpperHeight => Window.position.height * DividerRelativePosition;
        public float LowerHeight => Window.position.height - UpperHeight;

        public float LeftWidth => Window.position.width * DividerRelativePosition;
        public float RightWidth => Window.position.width - LeftWidth;
    }
#endif
}