using UnityEngine;

namespace SeikaGameDevKit.Events
{
    /// <summary>
    /// int を1つ持つイベント。
    /// </summary>
    [CreateAssetMenu(menuName = "Seika Game Dev Kit/Events/Int Game Event", fileName = "NewIntGameEvent", order = 1)]
    public class IntGameEvent : GameEvent<int> { }
}
