using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SeikaGameDevKit.Editor.Conventions
{
    /// <summary>
    /// Assets/_Project にファイルを取り込んだ・移したとき、名前に使えない文字が入っていれば、その場でコンソールに警告を出す。
    /// 全体の点検は <see cref="ConventionChecker"/>（メニュー「Seika Game Dev Kit > 決まりを点検」）で行う。
    /// </summary>
    public sealed class ConventionImportWatcher : AssetPostprocessor
    {
        const int MaxWarnings = 10;
        static readonly Regex AllowedName = new("^[A-Za-z0-9_-]+$");

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            int count = 0;
            int skipped = 0;
            foreach (string path in Concat(imported, moved))
            {
                if (!path.StartsWith("Assets/_Project/", StringComparison.Ordinal)) continue;
                string fileName = Path.GetFileName(path);
                bool isFolder = AssetDatabase.IsValidFolder(path);
                int dot = fileName.IndexOf('.');
                string name = isFolder || dot <= 0 ? fileName : fileName.Substring(0, dot);
                if (AllowedName.IsMatch(name)) continue;
                if (count++ >= MaxWarnings) { skipped++; continue; }
                Debug.LogWarning($"[決まり] 名前「{fileName}」に、半角の英字・数字と _ - 以外の文字が入っています。Unity の中で名前を変えてください：{path}",
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
            }
            if (skipped > 0)
                Debug.LogWarning($"[決まり] ほかにも名前の決まりから外れたものが {skipped} 件あります。メニュー「Seika Game Dev Kit > 決まりを点検」で一覧を見てください。");
        }

        static System.Collections.Generic.IEnumerable<string> Concat(string[] a, string[] b)
        {
            foreach (var s in a) yield return s;
            foreach (var s in b) yield return s;
        }
    }
}
