using UnityEngine;

namespace SeikaGameDevKit.Events
{
    /// <summary>
    /// int を1つ持つイベントを受けて、Inspector でつないだ反応に値を渡す。
    /// </summary>
    [AddComponentMenu("Seika Game Dev Kit/Events/Int Game Event Listener")]
    public class IntGameEventListener : GameEventListener<int, IntGameEvent> { }
}
