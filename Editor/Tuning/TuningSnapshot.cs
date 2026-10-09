using System.Collections.Generic;
using System.Globalization;
using SeikaGameDevKit.Events;
using UnityEditor;
using UnityEngine;

namespace SeikaGameDevKit.Editor.Tuning
{
    /// <summary>
    /// 再生を始める前の調整値（ScriptableObject）の控え。再生を始めるときのドメインの再読み込みをまたぐため、SessionState に置く。
    /// </summary>
    public static class TuningSnapshot
    {
        const string GuidsKey = "SeikaGameDevKit.Tuning.Guids";
        const string AssetKeyPrefix = "SeikaGameDevKit.Tuning.Asset.";

        public static bool Exists => !string.IsNullOrEmpty(SessionState.GetString(GuidsKey, ""));

        /// <summary>フォルダの中の ScriptableObject の値を控える（イベントのアセットは値を持たないので除く）。</summary>
        public static void Capture(string folder)
        {
            Clear();
            if (!AssetDatabase.IsValidFolder(folder)) return;
            var guids = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { folder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || asset is GameEventBase) continue;
                SessionState.SetString(AssetKeyPrefix + guid, EditorJsonUtility.ToJson(asset));
                guids.Add(guid);
            }
            SessionState.SetString(GuidsKey, string.Join(",", guids));
        }

        public static void Clear()
        {
            foreach (var guid in Guids()) SessionState.EraseString(AssetKeyPrefix + guid);
            SessionState.EraseString(GuidsKey);
        }

        /// <summary>控えと今の値を比べ、変わった値の一覧を返す。</summary>
        public static List<TuningChange> Diff()
        {
            var changes = new List<TuningChange>();
            foreach (var guid in Guids())
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;
                var before = CreateBefore(asset, guid);
                if (before == null) continue;
                try
                {
                    var now = new SerializedObject(asset);
                    var old = new SerializedObject(before);
                    var it = now.GetIterator();
                    bool enterChildren = true;
                    while (it.NextVisible(enterChildren))
                    {
                        // Vector3 や Color なども1つの値として比べる。中を見るのは、まとまり（クラス・配列）だけ
                        enterChildren = it.propertyType == SerializedPropertyType.Generic;
                        if (it.propertyPath == "m_Script" || enterChildren) continue;
                        var oldProp = old.FindProperty(it.propertyPath);
                        if (oldProp != null && SerializedProperty.DataEquals(it, oldProp)) continue;
                        changes.Add(new TuningChange
                        {
                            AssetGuid = guid,
                            AssetPath = path,
                            AssetName = asset.name,
                            PropertyPath = it.propertyPath,
                            DisplayName = Label(it),
                            Before = oldProp != null ? ValueText(oldProp) : "（なし）",
                            After = ValueText(it),
                        });
                    }
                }
                finally
                {
                    Object.DestroyImmediate(before);
                }
            }
            return changes;
        }

        /// <summary>1つの値を、控えておいた値に戻す（Ctrl+Z で取り消せる）。</summary>
        public static bool Revert(TuningChange change)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(change.AssetPath);
            if (asset == null) return false;
            var before = CreateBefore(asset, change.AssetGuid);
            if (before == null) return false;
            try
            {
                var oldProp = new SerializedObject(before).FindProperty(change.PropertyPath);
                if (oldProp == null) return false;
                Undo.RecordObject(asset, "調整値を戻す");
                var now = new SerializedObject(asset);
                now.CopyFromSerializedProperty(oldProp);
                now.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
                return true;
            }
            finally
            {
                Object.DestroyImmediate(before);
            }
        }

        /// <summary>変わった値をファイルに保存する（上書き）。</summary>
        public static void Overwrite(TuningChange change)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(change.AssetPath);
            if (asset != null) AssetDatabase.SaveAssetIfDirty(asset);
        }

        static IEnumerable<string> Guids()
        {
            string joined = SessionState.GetString(GuidsKey, "");
            return string.IsNullOrEmpty(joined) ? new string[0] : joined.Split(',');
        }

        // 控えの値を持つ、一時的な複製を作る（使い終わったら DestroyImmediate する）
        static ScriptableObject CreateBefore(ScriptableObject asset, string guid)
        {
            string json = SessionState.GetString(AssetKeyPrefix + guid, "");
            if (string.IsNullOrEmpty(json)) return null;
            var copy = ScriptableObject.CreateInstance(asset.GetType());
            copy.hideFlags = HideFlags.HideAndDontSave;
            EditorJsonUtility.FromJsonOverwrite(json, copy);
            return copy;
        }

        // 配列の中の値は「配列の名前 > Element 0」のように、どこの値か分かる名前にする
        static string Label(SerializedProperty prop)
        {
            if (!prop.propertyPath.Contains(".")) return prop.displayName;
            return prop.propertyPath.Replace(".Array.data[", " [").Replace(".Array.size", " の数").Replace(".", " > ");
        }

        static string ValueText(SerializedProperty prop)
        {
            var c = CultureInfo.InvariantCulture;
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.ArraySize: return prop.longValue.ToString(c);
                case SerializedPropertyType.Boolean: return prop.boolValue ? "true" : "false";
                case SerializedPropertyType.Float: return prop.doubleValue.ToString("0.#####", c);
                case SerializedPropertyType.String: return prop.stringValue;
                case SerializedPropertyType.Enum:
                    int index = prop.enumValueIndex;
                    return index >= 0 && index < prop.enumDisplayNames.Length ? prop.enumDisplayNames[index] : index.ToString(c);
                case SerializedPropertyType.Color: return "#" + ColorUtility.ToHtmlStringRGBA(prop.colorValue);
                case SerializedPropertyType.Vector2: return prop.vector2Value.ToString();
                case SerializedPropertyType.Vector3: return prop.vector3Value.ToString();
                case SerializedPropertyType.Vector4: return prop.vector4Value.ToString();
                case SerializedPropertyType.ObjectReference: return prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "None";
                default: return "（" + prop.propertyType + "）";
            }
        }
    }
}
