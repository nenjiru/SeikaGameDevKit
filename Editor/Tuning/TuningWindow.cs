using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SeikaGameDevKit.Editor.Tuning
{
    /// <summary>
    /// 再生中に変わった調整値の一覧。値ごと、またはまとめて「上書き」（変わった値を保存する）か「戻す」（再生前の値に戻す）を選ぶ。
    /// 閉じたときに残っている値は「上書き」として扱う。
    /// </summary>
    public sealed class TuningWindow : EditorWindow
    {
        static readonly Color OverwriteColor = new(0.45f, 0.9f, 0.45f);
        static readonly Color RevertColor = new(1f, 0.5f, 0.5f);

        List<TuningChange> changes;
        Vector2 scroll;

        public static void Open(List<TuningChange> changes)
        {
            var window = GetWindow<TuningWindow>(true, "調整値の変更", true);
            window.changes = changes;
            window.minSize = new Vector2(460, 160);
            window.Show();
        }

        public static void CloseIfOpen()
        {
            if (HasOpenInstances<TuningWindow>()) GetWindow<TuningWindow>().Close();
        }

        void OnGUI()
        {
            // スクリプトの再読み込みで一覧が失われたら、上書きしたものとして閉じる
            if (changes == null)
            {
                Close();
                return;
            }

            EditorGUILayout.HelpBox("再生中に、次の調整値が変わりました。値ごとに「上書き」か「戻す」を選んでください。\n閉じると、残っている値は上書きします。", MessageType.Info);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            TuningChange chosen = null;
            bool overwrite = false;
            foreach (var change in changes)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField($"{change.AssetName} / {change.DisplayName}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"{change.Before}  →  {change.After}");
                    }
                    if (ColoredButton("上書き", OverwriteColor, GUILayout.Width(60), GUILayout.Height(36)))
                    {
                        chosen = change;
                        overwrite = true;
                    }
                    if (ColoredButton("戻す", RevertColor, GUILayout.Width(60), GUILayout.Height(36)))
                    {
                        chosen = change;
                        overwrite = false;
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            if (chosen != null)
            {
                if (overwrite) Overwrite(chosen);
                else Revert(chosen);
                changes.Remove(chosen);
                if (changes.Count == 0) Close();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (ColoredButton("すべて上書き", OverwriteColor, GUILayout.Height(28)))
                {
                    foreach (var change in changes) Overwrite(change);
                    changes.Clear();
                    Close();
                    GUIUtility.ExitGUI();
                }
                if (ColoredButton("すべて戻す", RevertColor, GUILayout.Height(28)))
                {
                    foreach (var change in changes) Revert(change);
                    changes.Clear();
                    Close();
                    GUIUtility.ExitGUI();
                }
            }
        }

        void OnDestroy()
        {
            if (changes != null)
            {
                foreach (var change in changes) Overwrite(change);
            }
            changes = null;
            TuningSnapshot.Clear();
        }

        static bool ColoredButton(string label, Color color, params GUILayoutOption[] options)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool pressed = GUILayout.Button(label, options);
            GUI.backgroundColor = previous;
            return pressed;
        }

        static void Overwrite(TuningChange change)
        {
            TuningSnapshot.Overwrite(change);
            TuningTracker.Log("tuning_overwrite", change);
        }

        static void Revert(TuningChange change)
        {
            if (TuningSnapshot.Revert(change)) TuningTracker.Log("tuning_revert", change);
        }
    }
}
