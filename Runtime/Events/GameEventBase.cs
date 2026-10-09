using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeikaGameDevKit.Events
{
    /// <summary>
    /// イベントのアセットの共通部分。どのイベントが送られたかを1か所で受け取る窓口と、再生ごとの片付けを持つ。
    /// </summary>
    public abstract class GameEventBase : ScriptableObject
    {
        /// <summary>
        /// いずれかのイベントが送られたとき（イベント, 値。値のないイベントは null）。プレイの記録などが使う。
        /// 再生の開始時（SubsystemRegistration）に登録が消えるので、それより後（BeforeSceneLoad など）で登録する。
        /// </summary>
        public static event Action<GameEventBase, object> AnyRaised;

        // 読み込まれているイベントのアセット。再生の開始時に、前回の再生の登録を消すために使う
        static readonly HashSet<GameEventBase> loaded = new();

        [SerializeField, TextArea, Tooltip("何の出来事か（メモ）")] string eventDescription;

        public string EventDescription => eventDescription;

        protected virtual void OnEnable() => loaded.Add(this);

        protected virtual void OnDisable() => loaded.Remove(this);

        protected void NotifyAnyRaised(object value)
        {
            // 窓口に誰もいなければ値を箱に入れない（毎フレーム送るイベントで無駄なメモリを使わないため）
            if (AnyRaised != null) AnyRaised(this, value);
        }

        protected abstract void ClearListeners();

        // Enter Play Mode の設定でドメインを再読み込みしない場合、前回の再生の登録が残るので消す
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            AnyRaised = null;
            foreach (var gameEvent in loaded) gameEvent.ClearListeners();
        }
    }
}
