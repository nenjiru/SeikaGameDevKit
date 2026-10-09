using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeikaGameDevKit.Events
{
    /// <summary>
    /// 値を持たないイベント（例：ボールが落ちた）。
    /// <code>
    /// [SerializeField] GameEvent ballDrained;
    /// ballDrained.Raise();                      // 送る
    /// ballDrained.AddListener(OnBallDrained);   // 受ける（OnDisable で RemoveListener）
    /// </code>
    /// </summary>
    [CreateAssetMenu(menuName = "Seika Game Dev Kit/Events/Game Event", fileName = "NewGameEvent", order = 0)]
    public class GameEvent : GameEventBase
    {
        readonly List<Action> listeners = new();

        public void Raise()
        {
            NotifyAnyRaised(null);
            // 受け手の中で登録を外しても大丈夫なように後ろから呼ぶ（呼ぶ順番は決まっていない）
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (i < listeners.Count) listeners[i]?.Invoke();
            }
        }

        public void AddListener(Action listener)
        {
            if (listener != null && !listeners.Contains(listener)) listeners.Add(listener);
        }

        public void RemoveListener(Action listener) => listeners.Remove(listener);

        protected override void ClearListeners() => listeners.Clear();
    }
}
