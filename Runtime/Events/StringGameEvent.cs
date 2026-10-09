using UnityEngine;

namespace SeikaGameDevKit.Events
{
    /// <summary>
    /// string を1つ持つイベント。
    /// </summary>
    [CreateAssetMenu(menuName = "Seika Game Dev Kit/Events/String Game Event", fileName = "NewStringGameEvent", order = 3)]
    public class StringGameEvent : GameEvent<string> { }
}
