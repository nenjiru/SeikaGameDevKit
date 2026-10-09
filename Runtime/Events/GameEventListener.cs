using UnityEngine;
using UnityEngine.Events;

namespace SeikaGameDevKit.Events
{
    /// <summary>
    /// 値を持たないイベントを受けて、Inspector でつないだ反応を呼ぶ。コードを書かずにつなぐときに使う。
    /// </summary>
    [AddComponentMenu("Seika Game Dev Kit/Events/Game Event Listener")]
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField, Tooltip("受けるイベント")] GameEvent gameEvent;
        [SerializeField, Tooltip("イベントが送られたときに呼ぶ")] UnityEvent response;

        void OnEnable()
        {
            if (gameEvent != null) gameEvent.AddListener(OnRaised);
        }

        void OnDisable()
        {
            if (gameEvent != null) gameEvent.RemoveListener(OnRaised);
        }

        void OnRaised() => response.Invoke();
    }
}
