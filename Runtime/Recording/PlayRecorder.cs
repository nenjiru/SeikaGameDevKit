using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SeikaGameDevKit.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeikaGameDevKit.Recording
{
    /// <summary>
    /// エディタで再生するたびに、何が起きたかを自動で記録する（置くものはない）。
    /// 書き出し先：プロジェクト直下の Logs/PlayLog/&lt;日時&gt;.jsonl（新しいものから <see cref="KeepFiles"/> 個を残す）。
    /// 記録するもの：再生の開始と終了、シーンの読み込み、キットのイベント、コンソールのログ・警告・エラー。
    /// 止めたいときは、Player Settings の Scripting Define Symbols に SEIKA_NO_PLAYLOG を足す。
    /// </summary>
    public static class PlayRecorder
    {
        public const int KeepFiles = 20;
        const int StackLines = 3;

        static PlayLogWriter log;
        static string path;
        static int eventCount;
        static bool writing;

        /// <summary>今の記録のファイル（記録していなければ null）。</summary>
        public static string CurrentPath => log != null ? path : null;

        /// <summary>記録のフォルダ（プロジェクト直下の Logs/PlayLog）。</summary>
        public static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "PlayLog"));

        // Enter Play Mode の設定でドメインを再読み込みしない場合に、前回の再生の状態を片付ける
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            Stop();
        }

        // イベントの窓口は SubsystemRegistration で空になるので、それより後で登録する
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Begin()
        {
#if SEIKA_NO_PLAYLOG
            return;
#else
            if (!Application.isEditor) return;
            try
            {
                Directory.CreateDirectory(Folder);
                path = Path.Combine(Folder, DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".jsonl");
                log = new PlayLogWriter(new StreamWriter(path, false, new UTF8Encoding(false)) { AutoFlush = true });
            }
            catch (Exception e)
            {
                log = null;
                Debug.LogWarning($"[PlayRecorder] 記録のファイルを作れなかったので、今回は記録しません：{e.Message}");
                return;
            }
            eventCount = 0;
            DeleteOldFiles();

            Write("session_start", null, null,
                ("date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                ("unity", Application.unityVersion),
                ("scene", SceneManager.GetActiveScene().name));

            GameEventBase.AnyRaised += OnEventRaised;
            Application.logMessageReceived += OnLogMessage;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting += OnQuitting;
#endif
        }

        static void OnEventRaised(GameEventBase gameEvent, object value)
        {
            eventCount++;
            string text = value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
            Write("event", "event:" + gameEvent.name, text, ("name", gameEvent.name), ("value", text));
        }

        static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            // 記録の中で出したログを、また記録しない
            if (writing) return;
            string level = type switch
            {
                LogType.Log => "log",
                LogType.Warning => "warning",
                LogType.Assert => "assert",
                LogType.Error => "error",
                _ => "exception",
            };
            string stack = type is LogType.Error or LogType.Exception or LogType.Assert
                ? string.Join("\n", (stackTrace ?? "").Split('\n').Where(l => l.Trim().Length > 0).Take(StackLines))
                : null;
            Write("log", "log:" + level + ":" + message, message, ("level", level), ("message", message), ("stack", string.IsNullOrEmpty(stack) ? null : stack));
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 再生を始めたときの最初の読み込みは、Single / Additive 以外の内部の値で届く
            string how = mode switch
            {
                LoadSceneMode.Single => "single",
                LoadSceneMode.Additive => "additive",
                _ => "first",
            };
            Write("scene_loaded", null, null, ("scene", scene.name), ("mode", how));
        }

        static void OnQuitting()
        {
            Write("session_end", null, null, ("duration", Math.Round(Time.unscaledTime, 3)), ("events", eventCount));
            Stop();
        }

        static void Write(string kind, string throttleKey, string value, params (string name, object value)[] fields)
        {
            if (log == null) return;
            writing = true;
            try
            {
                log.Write(kind, Time.unscaledTime, Time.frameCount, throttleKey, value, fields);
            }
            catch (Exception)
            {
                // 書けなくなったら、ゲームを止めないように記録だけやめる
                Stop();
            }
            finally
            {
                writing = false;
            }
        }

        static void Stop()
        {
            GameEventBase.AnyRaised -= OnEventRaised;
            Application.logMessageReceived -= OnLogMessage;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.quitting -= OnQuitting;
            if (log == null) return;
            try
            {
                log.FlushAll(Time.unscaledTime, Time.frameCount);
                log.Dispose();
            }
            catch (Exception)
            {
                // 片付けの失敗は無視する
            }
            log = null;
        }

        static void DeleteOldFiles()
        {
            try
            {
                var old = new DirectoryInfo(Folder).GetFiles("*.jsonl")
                    .OrderByDescending(f => f.Name)
                    .Skip(KeepFiles);
                foreach (var file in old) file.Delete();
            }
            catch (Exception)
            {
                // 古い記録を消せなくても、記録は続ける
            }
        }
    }
}
