using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SeikaGameDevKit.Editor.Conventions
{
    /// <summary>
    /// Assets が PROJECT_CONVENTIONS.md の決まりに沿っているかを点検する（ファイルは変えない）。
    /// - フォルダ構成：PROJECT_CONVENTIONS.md の図（<see cref="FolderTree"/>）を正とする
    /// - 名前の文字・Art の中身・スクリプトの決まり：キットで決める（授業によって変わらない前提）
    /// - ゲームの名前空間：Project Settings > Editor > Root namespace
    /// 結果はウィンドウに出し、Logs/Conventions/latest.json にも書く（エージェントが読む）。
    /// </summary>
    public static class ConventionChecker
    {
        const string MenuPath = "Seika Game Dev Kit/決まりを点検";
        const string ProjectRoot = "Assets/_Project";
        const string ArtRoot = ProjectRoot + "/Art";
        const string ScriptsRoot = ProjectRoot + "/Scripts";
        const string SandboxRoot = ProjectRoot + "/Sandbox";

        static readonly Regex AllowedName = new("^[A-Za-z0-9_-]+$");
        static readonly Regex TypeDeclaration = new(@"\b(?:class|struct|interface|enum|record)\s+([A-Za-z_][A-Za-z0-9_]*)");
        static readonly Regex NamespaceDeclaration = new(@"\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)");

        public static string ReportPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Conventions", "latest.json"));

        [MenuItem(MenuPath, priority = 200)]
        static void RunFromMenu() => ConventionWindow.Open(Run());

        /// <summary>点検して、結果を Logs/Conventions/latest.json に書く。エージェントは MCP からこれを呼ぶか、メニューを実行する。</summary>
        public static ConventionReport Run()
        {
            var report = new ConventionReport
            {
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                conventions = "PROJECT_CONVENTIONS.md",
                rootNamespace = EditorSettings.projectGenerationRootNamespace,
            };

            FolderTree tree = null;
            if (File.Exists(FolderTree.ConventionsPath)) tree = FolderTree.Parse(File.ReadAllText(FolderTree.ConventionsPath));
            if (tree == null) report.notes.Add("PROJECT_CONVENTIONS.md のフォルダ構成の図を読めなかったので、置き場所は点検していない");
            if (string.IsNullOrEmpty(report.rootNamespace)) report.notes.Add("Project Settings > Editor > Root namespace（ゲームの名前空間）が空なので、名前空間は点検していない");

            var paths = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal))
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

            foreach (string path in paths)
            {
                bool isFolder = AssetDatabase.IsValidFolder(path);
                if (tree != null) CheckPlace(path, isFolder, tree, report);
                if (!path.StartsWith(ProjectRoot + "/", StringComparison.Ordinal)) continue;
                CheckName(path, isFolder, report);
                if (isFolder) continue;
                if (path.StartsWith(ArtRoot + "/", StringComparison.Ordinal)) CheckArt(path, report);
                if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) CheckScript(path, report);
            }

            WriteReport(report);
            return report;
        }

        // 名前に使えるのは半角の英字・数字と _ -。ファイルは拡張子を除いて見る
        static void CheckName(string path, bool isFolder, ConventionReport report)
        {
            string fileName = Path.GetFileName(path);
            string name = isFolder ? fileName : StripExtensions(fileName);
            if (!AllowedName.IsMatch(name))
                Add(report, "name", path, $"名前「{fileName}」に、半角の英字・数字と _ - 以外の文字が入っている");
        }

        // 中が決まっているフォルダ（図に子が書かれているもの）の直下には、決まったフォルダしか置かない
        static void CheckPlace(string path, bool isFolder, FolderTree tree, ConventionReport report)
        {
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent) || !tree.HasFixedChildren(parent)) return;
            if (!isFolder)
            {
                Add(report, "place", path, $"「{parent}」の直下にファイルを置かない（決まったフォルダの中に置く）");
                return;
            }
            if (!tree.Children[parent].Contains(Path.GetFileName(path)))
                Add(report, "place", path, $"「{parent}」に、決まっていないフォルダがある（PROJECT_CONVENTIONS.md のフォルダ構成にない）");
        }

        // Art は見た目だけ。スクリプトと、ゲームに関わる部品（当たり判定・Rigidbody・プロジェクトのスクリプト）を入れない
        static void CheckArt(string path, ConventionReport report)
        {
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                Add(report, "art", path, "Art/ にスクリプトを置かない（Scripts/ に置く）");
                return;
            }
            if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;
            var found = new List<string>();
            if (prefab.GetComponentsInChildren<Collider>(true).Length > 0 || prefab.GetComponentsInChildren<Collider2D>(true).Length > 0) found.Add("当たり判定");
            if (prefab.GetComponentsInChildren<Rigidbody>(true).Length > 0 || prefab.GetComponentsInChildren<Rigidbody2D>(true).Length > 0) found.Add("Rigidbody");
            foreach (var behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                var script = MonoScript.FromMonoBehaviour(behaviour);
                string scriptPath = script != null ? AssetDatabase.GetAssetPath(script) : null;
                // UI の Image など、Unity やパッケージの部品は見た目に使うので数えない
                if (scriptPath != null && scriptPath.StartsWith("Assets/", StringComparison.Ordinal)) found.Add(script.name);
            }
            if (found.Count > 0)
                Add(report, "art", path, $"Art/ のプレハブに、ゲームの動きの部品（{string.Join("・", found.Distinct())}）が付いている（Prefabs/ 側のプレハブに付ける）");
        }

        // ファイル名と同じ名前の型があること。Scripts/ の中は、名前空間が「Root namespace + Scripts/ の下のフォルダ」であること
        static void CheckScript(string path, ConventionReport report)
        {
            bool inScripts = path.StartsWith(ScriptsRoot + "/", StringComparison.Ordinal);
            bool inSandbox = path.StartsWith(SandboxRoot + "/", StringComparison.Ordinal);
            bool inArt = path.StartsWith(ArtRoot + "/", StringComparison.Ordinal);
            if (!inScripts && !inSandbox && !inArt)
                Add(report, "script", path, "スクリプトは Scripts/ に置く");

            string text;
            try { text = File.ReadAllText(path); }
            catch (Exception) { return; }
            text = StripComments(text);

            string fileName = Path.GetFileNameWithoutExtension(path);
            var types = TypeDeclaration.Matches(text).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            if (types.Count > 0 && !types.Contains(fileName))
                Add(report, "script", path, $"ファイル名「{fileName}」と同じ名前のクラスがない（中にあるのは {string.Join("・", types.Distinct())}）");

            if (!inScripts || string.IsNullOrEmpty(report.rootNamespace)) return;
            string folder = Path.GetDirectoryName(path).Replace('\\', '/');
            string sub = folder.Length > ScriptsRoot.Length ? folder.Substring(ScriptsRoot.Length + 1).Replace('/', '.') : "";
            string expected = sub.Length > 0 ? report.rootNamespace + "." + sub : report.rootNamespace;
            var ns = NamespaceDeclaration.Match(text);
            if (!ns.Success)
                Add(report, "script", path, $"名前空間がない（{expected} にする）");
            else if (ns.Groups[1].Value != expected)
                Add(report, "script", path, $"名前空間が「{ns.Groups[1].Value}」になっている（{expected} にする）");
        }

        static string StripExtensions(string fileName)
        {
            int dot = fileName.IndexOf('.');
            return dot > 0 ? fileName.Substring(0, dot) : fileName;
        }

        // コメントの中の「class」などを型と間違えないように消す
        static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\n]*", "");
        }

        static void Add(ConventionReport report, string rule, string path, string message)
        {
            report.issues.Add(new ConventionIssue { rule = rule, path = path, message = message });
        }

        static void WriteReport(ConventionReport report)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConventionChecker] 点検の結果をファイルに書けませんでした：{e.Message}");
            }
        }
    }
}
