using System;
using System.Collections.Generic;

namespace SeikaGameDevKit.Editor.Conventions
{
    /// <summary>決まりの点検の結果。Logs/Conventions/latest.json にこの形で書く。</summary>
    [Serializable]
    public sealed class ConventionReport
    {
        public string date;
        public string conventions;
        public string rootNamespace;
        public List<ConventionIssue> issues = new();
        /// <summary>点検できなかったことなど、外れている物ではない知らせ。</summary>
        public List<string> notes = new();
    }
}
