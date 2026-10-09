using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace SeikaGameDevKit.Editor.Visualization
{
    /// <summary>
    /// デバッグマーカーの表示のオン・オフ。人ごとの好みなので、この PC の EditorPrefs に置く。
    /// </summary>
    public static class VisualizationSettings
    {
        const string EnabledKey = "SeikaGameDevKit.Visualization.Colliders";
        const string MenuPath = "Seika Game Dev Kit/デバッグマーカーを表示";

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set
            {
                EditorPrefs.SetBool(EnabledKey, value);
                SceneView.RepaintAll();
            }
        }

        [MenuItem(MenuPath, priority = 100)]
        static void Toggle() => Enabled = !Enabled;

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        // 割り当ては Edit > Shortcuts で変えられる
        [Shortcut("Seika Game Dev Kit/デバッグマーカーの表示を切り替え", KeyCode.G, ShortcutModifiers.Shift | ShortcutModifiers.Alt)]
        static void ToggleShortcut() => Toggle();
    }
}
