using System;
using System.Collections.Generic;

namespace SeikaGameDevKit.Events
{
    /// <summary>
    /// 値を1つ持つイベントの元。値の型ごとに1行で作る。
    /// <code>
    /// [CreateAssetMenu(menuName = "Events/State Changed Event")]
    /// public class StateChangedEvent : GameEvent&lt;StateChange&gt; { }
    /// </code>
    /// </summary>
    public abstract class GameEvent<T> : GameEventBase
    {
        readonly List<Action<T>> listeners = new();

        public void Raise(T value)
        {
            NotifyAnyRaised(value);
            // 受け手の中で登録を外しても大丈夫なように後ろから呼ぶ（呼ぶ順番は決まっていない）
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (i < listeners.Count) listeners[i]?.Invoke(value);
            }
        }

        public void AddListener(Action<T> listener)
        {
            if (listener != null && !listeners.Contains(listener)) listeners.Add(listener);
        }

        public void RemoveListener(Action<T> listener) => listeners.Remove(listener);

        protected override void ClearListeners() => listeners.Clear();
    }
}
