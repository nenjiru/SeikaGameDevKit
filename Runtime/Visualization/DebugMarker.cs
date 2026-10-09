using UnityEngine;

namespace SeikaGameDevKit.Visualization
{
    /// <summary>
    /// Scene ビューで見つけやすくするための印。どの GameObject にも付けられ、付けると選んでいなくても常に描く。
    /// 当たり判定があれば、その形（見た目があっても）と名前を描く。なければ（出現地点など）、位置に小さな印と名前を描く。
    /// 描くのはエディタだけで、ゲームの動きには関係しない。
    /// </summary>
    [AddComponentMenu("Seika Game Dev Kit/Visualization/Debug Marker")]
    [DisallowMultipleComponent]
    public class DebugMarker : MonoBehaviour
    {
        [SerializeField, Tooltip("Scene ビューに出す名前（例：落下判定）。空ならオブジェクトの名前")] string label;
        [SerializeField, Tooltip("色を自分で決める（オフならトリガーかどうかで決まる色）")] bool useCustomColor;
        [SerializeField, Tooltip("描く色")] Color color = new(1f, 0.85f, 0.2f, 1f);

        public string Label => string.IsNullOrEmpty(label) ? name : label;
        public bool UseCustomColor => useCustomColor;
        public Color Color => color;
    }
}
