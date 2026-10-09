using System;

namespace SeikaGameDevKit.Editor.Conventions
{
    /// <summary>決まりから外れているもの1つ分。</summary>
    [Serializable]
    public sealed class ConventionIssue
    {
        /// <summary>どの決まりか：name（名前の文字）／place（置き場所）／art（Art の中身）／script（スクリプト）。</summary>
        public string rule;
        public string path;
        public string message;
    }
}
