namespace SeikaGameDevKit.Editor.Tuning
{
    /// <summary>
    /// 再生中に変わった調整値1つ分（どのアセットの、どの値が、何から何に変わったか）。
    /// </summary>
    public sealed class TuningChange
    {
        public string AssetGuid;
        public string AssetPath;
        public string AssetName;
        public string PropertyPath;
        public string DisplayName;
        public string Before;
        public string After;
    }
}
