using UnityEngine;

namespace SeikaGameDevKit.Events
{
    /// <summary>
    /// float を1つ持つイベントを受けて、Inspector でつないだ反応に値を渡す。
    /// </summary>
    [AddComponentMenu("Seika Game Dev Kit/Events/Float Game Event Listener")]
    public class FloatGameEventListener : GameEventListener<float, FloatGameEvent> { }
}
