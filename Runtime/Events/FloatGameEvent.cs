using UnityEngine;

namespace SeikaGameDevKit.Events
{
    /// <summary>
    /// float を1つ持つイベント。
    /// </summary>
    [CreateAssetMenu(menuName = "Seika Game Dev Kit/Events/Float Game Event", fileName = "NewFloatGameEvent", order = 2)]
    public class FloatGameEvent : GameEvent<float> { }
}
