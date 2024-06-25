using System;
using System.Collections.Generic;
using System.Linq;
using Pukpukpuk.DataFeed.Input.Editor;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Input
{
    [Serializable]
    public class CompletionLine
    {
#if UNITY_EDITOR
        public List<Completion> Completions = new();

        public Completion GetNearestFor(Completion otherCompletion)
        {
            var middle = otherCompletion.Middle;
            var otherWidth = otherCompletion.Right - otherCompletion.Left;

            return Completions
                .Where(completion =>
                {
                    // Проекции двух заполнений должны пересекаться, чтобы через них можно было перейти
                    var left = Math.Min(completion.Left, otherCompletion.Left);
                    var right = Math.Max(completion.Right, otherCompletion.Right);

                    var distance = right - left;
                    var width = completion.Right - completion.Left;

                    return width + otherWidth > distance;
                })
                .OrderBy(completion =>
                {
                    var left = Math.Abs(completion.Left - middle);
                    var right = Math.Abs(completion.Right - middle);
                    return Math.Min(left, right);
                }).FirstOrDefault();
        }
#endif
    }

    [Serializable]
    public class Completion
    {
#if UNITY_EDITOR
        public string Text;
        public float Left;
        public float Right;

        public float Middle => (Left + Right) / 2f;

        public Completion(string text, float left, float right)
        {
            Text = text;
            Left = left;
            Right = right;
        }

        public void Focus()
        {
            InputWindow.Instance.FocusedCompletionText = Text;
            GUI.FocusControl(InputWindow.GetNameForCompletion(Text));
        }
#endif
    }
}