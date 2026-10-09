using UnityEngine;
using UnityEngine.Events;

namespace SeikaGameDevKit.Events
{
    /// <summary>
    /// 値を1つ持つイベントを受けて、Inspector でつないだ反応に値を渡す。値の型ごとに1行で作る。
    /// <code>
    /// public class StateChangedEventListener : GameEventListener&lt;StateChange, StateChangedEvent&gt; { }
    /// </code>
    /// </summary>
    public abstract class GameEventListener<T, TEvent> : MonoBehaviour where TEvent : GameEvent<T>
    {
        [SerializeField, Tooltip("受けるイベント")] TEvent gameEvent;
        [SerializeField, Tooltip("イベントが送られたときに呼ぶ（送られた値を渡す）")] UnityEvent<T> response;

        void OnEnable()
        {
            if (gameEvent != null) gameEvent.AddListener(OnRaised);
        }

        void OnDisable()
        {
            if (gameEvent != null) gameEvent.RemoveListener(OnRaised);
        }

        void OnRaised(T value) => response.Invoke(value);
    }
}
