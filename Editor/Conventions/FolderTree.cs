using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SeikaGameDevKit.Editor.Conventions
{
    /// <summary>
    /// PROJECT_CONVENTIONS.md のフォルダ構成の図（コードブロックの中の Assets から始まる木）を読んだもの。
    /// 図の各行は「├─ 名前  説明」「└─ 名前  説明」の形で、├ と └ の位置で深さが決まる。
    /// </summary>
    public sealed class FolderTree
    {
        /// <summary>決まったフォルダ（Assets から始まるパス）と、その中に決まったフォルダの名前。中が決まっていないフォルダは空の一覧。</summary>
        public readonly Dictionary<string, List<string>> Children = new();

        public bool IsDefined(string folder) => Children.ContainsKey(folder);

        /// <summary>このフォルダの中が決まっているか（決まっていなければ、中は自由）。</summary>
        public bool HasFixedChildren(string folder) => Children.TryGetValue(folder, out var list) && list.Count > 0;

        public static FolderTree Parse(string markdown)
        {
            var lines = markdown.Replace("\r", "").Split('\n');
            int start = -1;
            for (int i = 0; i < lines.Length - 1; i++)
            {
                if (lines[i].TrimStart().StartsWith("```") && lines[i + 1].Trim() == "Assets") { start = i + 1; break; }
            }
            if (start < 0) return null;

            var tree = new FolderTree();
            tree.Children["Assets"] = new List<string>();
            var entries = new List<(int column, string name)>();
            for (int i = start + 1; i < lines.Length && !lines[i].TrimStart().StartsWith("```"); i++)
            {
                string line = lines[i];
                int column = line.IndexOfAny(new[] { '├', '└' });
                if (column < 0) continue;
                string rest = line.Substring(column + 1).TrimStart('─', ' ');
                string name = rest.Split(new[] { ' ', '\t' }, 2)[0];
                if (name.Length > 0) entries.Add((column, name));
            }

            // ├ と └ の位置を浅い順に並べ、深さに直す
            var columns = entries.Select(e => e.column).Distinct().OrderBy(c => c).ToList();
            var parents = new List<string> { "Assets" };
            foreach (var (column, name) in entries)
            {
                int depth = columns.IndexOf(column) + 1;
                while (parents.Count > depth) parents.RemoveAt(parents.Count - 1);
                if (parents.Count < depth) continue; // 図が崩れている行は読まない
                string parent = parents[depth - 1];
                string path = parent + "/" + name;
                tree.Children[parent].Add(name);
                tree.Children[path] = new List<string>();
                parents.Add(path);
            }
            return tree;
        }

        public static string ConventionsPath => Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "PROJECT_CONVENTIONS.md"));
    }
}
