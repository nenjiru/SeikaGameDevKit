using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SeikaGameDevKit.Editor.Conventions
{
    /// <summary>決まりの点検の結果の一覧。項目をクリックすると、そのファイルを Project ウィンドウで示す。</summary>
    public sealed class ConventionWindow : EditorWindow
    {
        static readonly (string rule, string title)[] Groups =
        {
            ("place", "置き場所"),
            ("name", "名前の文字"),
            ("art", "Art の中身"),
            ("script", "スクリプト"),
        };

        ConventionReport report;
        Vector2 scroll;

        public static void Open(ConventionReport report)
        {
            var window = GetWindow<ConventionWindow>(false, "決まりの点検", true);
            window.report = report;
            window.minSize = new Vector2(480, 200);
            window.Show();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("もう一度点検", EditorStyles.toolbarButton, GUILayout.Width(100))) report = ConventionChecker.Run();
                GUILayout.FlexibleSpace();
                if (report != null) GUILayout.Label(report.date, EditorStyles.miniLabel);
            }
            if (report == null)
            {
                EditorGUILayout.HelpBox("「もう一度点検」を押してください。", MessageType.Info);
                return;
            }

            foreach (var note in report.notes) EditorGUILayout.HelpBox(note, MessageType.Warning);
            if (report.issues.Count == 0)
            {
                EditorGUILayout.HelpBox("決まりから外れているものはありません。", MessageType.Info);
                return;
            }
            EditorGUILayout.HelpBox($"決まりから外れているものが {report.issues.Count} 件あります。項目を押すと、その場所を示します。\n直すときは、Unity の中で移動・名前の変更をしてください（.meta が離れないように）。", MessageType.None);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var (rule, title) in Groups)
            {
                var issues = report.issues.Where(i => i.rule == rule).ToList();
                if (issues.Count == 0) continue;
                EditorGUILayout.LabelField($"{title}（{issues.Count}）", EditorStyles.boldLabel);
                foreach (var issue in issues)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        if (GUILayout.Button(issue.path, EditorStyles.linkLabel)) Show(issue.path);
                        EditorGUILayout.LabelField(issue.message, EditorStyles.wordWrappedMiniLabel);
                    }
                }
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();
        }

        static void Show(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset == null) return;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
