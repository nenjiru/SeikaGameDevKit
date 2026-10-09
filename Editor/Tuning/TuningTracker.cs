using System;
using System.Globalization;
using System.IO;
using System.Text;
using SeikaGameDevKit.Recording;
using UnityEditor;
using UnityEngine;

namespace SeikaGameDevKit.Editor.Tuning
{
    /// <summary>
    /// 再生中に変わった調整値を見つけて知らせる（置くものはない）。
    /// 再生を始める前に <see cref="Folder"/> の ScriptableObject の値を控え、再生を止めたら変わった値を <see cref="TuningWindow"/> に出す。
    /// 変わった値と、上書きしたか戻したかは、そのときのプレイの記録（Logs/PlayLog）に書き足す。
    /// </summary>
    [InitializeOnLoad]
    public static class TuningTracker
    {
        /// <summary>見張るフォルダ（PROJECT_CONVENTIONS の「Data：設定値」）。</summary>
        public const string Folder = "Assets/_Project/Data";

        const string LogPathKey = "SeikaGameDevKit.Tuning.LogPath";
        const string LogTimeKey = "SeikaGameDevKit.Tuning.LogTime";
        const string LogFrameKey = "SeikaGameDevKit.Tuning.LogFrame";

        static TuningTracker()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    // 前回の一覧が開いたままなら、上書きしたものとして閉じる
                    TuningWindow.CloseIfOpen();
                    TuningSnapshot.Capture(Folder);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    // 記録が閉じる前に、書き足す先と時刻を控える
                    SessionState.SetString(LogPathKey, PlayRecorder.CurrentPath ?? "");
                    SessionState.SetFloat(LogTimeKey, Time.unscaledTime);
                    SessionState.SetInt(LogFrameKey, Time.frameCount);
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    if (!TuningSnapshot.Exists) break;
                    var changes = TuningSnapshot.Diff();
                    if (changes.Count == 0)
                    {
                        TuningSnapshot.Clear();
                        break;
                    }
                    foreach (var change in changes) Log("tuning_change", change);
                    TuningWindow.Open(changes);
                    break;
            }
        }

        /// <summary>調整値の出来事を、直前のプレイの記録に書き足す（記録がなければ何もしない）。</summary>
        internal static void Log(string kind, TuningChange change)
        {
            string path = SessionState.GetString(LogPathKey, "");
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try
            {
                using var writer = new PlayLogWriter(new StreamWriter(path, true, new UTF8Encoding(false)));
                writer.Write(kind, SessionState.GetFloat(LogTimeKey, 0f), SessionState.GetInt(LogFrameKey, 0), null, null,
                    ("asset", change.AssetName),
                    ("path", change.AssetPath),
                    ("field", change.PropertyPath),
                    ("label", change.DisplayName),
                    ("before", change.Before),
                    ("after", change.After));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TuningTracker] プレイの記録に書き足せませんでした：{e.Message}");
            }
        }
    }
}
