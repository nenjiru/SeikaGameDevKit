using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SeikaGameDevKit.Recording
{
    /// <summary>
    /// プレイの記録を1行1件の JSON（JSON Lines）で書く。Unity に依存しない。
    /// 同じもの（種類と名前が同じ）が1秒に <see cref="MaxPerSecond"/> 回を超えたら、その秒の残りは数えるだけにし、
    /// 秒が変わったときに「回数と最後の値」の1行（kind = "summary"）にまとめて書く。
    /// </summary>
    public sealed class PlayLogWriter : IDisposable
    {
        public const int MaxPerSecond = 10;

        sealed class Window
        {
            public long Second;
            public int Count;
            public int Suppressed;
            public string LastValue;
        }

        readonly TextWriter writer;
        readonly Dictionary<string, Window> windows = new();
        readonly List<string> ended = new();

        public PlayLogWriter(TextWriter writer)
        {
            this.writer = writer;
        }

        /// <summary>
        /// 1件書く。fields は kind・t・frame のあとに続ける項目（名前, 値）。値が null の項目は書かない。
        /// throttleKey を渡すと、同じ key が多すぎるときにまとめる。value はまとめたときの「最後の値」に使う。
        /// </summary>
        public void Write(string kind, double time, int frame, string throttleKey, string value, params (string name, object value)[] fields)
        {
            FlushEndedWindows(time, frame);

            if (throttleKey != null)
            {
                long second = (long)Math.Floor(time);
                if (!windows.TryGetValue(throttleKey, out var window))
                {
                    window = new Window { Second = second };
                    windows[throttleKey] = window;
                }
                if (window.Second != second)
                {
                    WriteSummary(throttleKey, window, time, frame);
                    window.Second = second;
                    window.Count = 0;
                    window.Suppressed = 0;
                }
                window.Count++;
                if (window.Count > MaxPerSecond)
                {
                    window.Suppressed++;
                    window.LastValue = value;
                    return;
                }
            }

            WriteLine(kind, time, frame, fields);
        }

        /// <summary>まとめ途中のものをすべて書き出す（記録の終わりに呼ぶ）。</summary>
        public void FlushAll(double time, int frame)
        {
            foreach (var pair in windows) WriteSummary(pair.Key, pair.Value, time, frame);
            windows.Clear();
            writer.Flush();
        }

        public void Dispose() => writer.Dispose();

        // 秒が変わったのに、まとめを書いていないものを書く（静かになったイベントのまとめが遅れないように）
        void FlushEndedWindows(double time, int frame)
        {
            long second = (long)Math.Floor(time);
            ended.Clear();
            foreach (var pair in windows)
            {
                if (pair.Value.Second < second && pair.Value.Suppressed > 0) ended.Add(pair.Key);
            }
            foreach (var key in ended)
            {
                var window = windows[key];
                WriteSummary(key, window, time, frame);
                window.Suppressed = 0;
            }
        }

        void WriteSummary(string key, Window window, double time, int frame)
        {
            if (window.Suppressed <= 0) return;
            WriteLine("summary", time, frame,
                ("of", key),
                ("second", window.Second),
                ("total", window.Count),
                ("omitted", window.Suppressed),
                ("last", window.LastValue));
        }

        void WriteLine(string kind, double time, int frame, params (string name, object value)[] fields)
        {
            var sb = new StringBuilder(128);
            sb.Append("{\"kind\":");
            AppendString(sb, kind);
            sb.Append(",\"t\":").Append(time.ToString("0.000", CultureInfo.InvariantCulture));
            sb.Append(",\"frame\":").Append(frame.ToString(CultureInfo.InvariantCulture));
            foreach (var (name, value) in fields)
            {
                if (value == null) continue;
                sb.Append(',');
                AppendString(sb, name);
                sb.Append(':');
                AppendValue(sb, value);
            }
            sb.Append('}');
            writer.WriteLine(sb.ToString());
        }

        static void AppendValue(StringBuilder sb, object value)
        {
            switch (value)
            {
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int or long or short or byte: sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); break;
                // NaN と無限大は JSON の数にならないので文字で書く
                case float f when float.IsFinite(f): sb.Append(f.ToString("R", CultureInfo.InvariantCulture)); break;
                case double d when double.IsFinite(d): sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); break;
                default: AppendString(sb, Convert.ToString(value, CultureInfo.InvariantCulture)); break;
            }
        }

        static void AppendString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
