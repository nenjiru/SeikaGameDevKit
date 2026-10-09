using SeikaGameDevKit.Visualization;
using UnityEditor;
using UnityEngine;

namespace SeikaGameDevKit.Editor.Visualization
{
    /// <summary>
    /// デバッグマーカーを Scene ビューに描く。形は Collider から取り、中を塗った部分をクリックすると、そのオブジェクトを選べる。
    /// - <see cref="DebugMarker"/> を付けたオブジェクト：常に描く。当たり判定があればその形を、なければ位置に印を描き、名前を出す
    /// - 付けていなくても、見た目のない当たり判定と、すべてのトリガーは描く（名前は選んだときだけ）
    /// </summary>
    public static class DebugMarkerGizmos
    {
        static readonly Color TriggerColor = new(1f, 0.55f, 0.1f);
        static readonly Color SolidColor = new(0.2f, 0.8f, 1f);
        const float FillAlpha = 0.12f;
        const float SelectedFillAlpha = 0.25f;
        const float MarkerSize = 0.25f;

        static GUIStyle labelStyle;

        // [DrawGizmo] は、共通の型（Collider）を指定しても派生の型には呼ばれないので、具体的な型ごとに登録する
        const GizmoType Always = GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable;
        [DrawGizmo(Always)] static void Draw(BoxCollider c, GizmoType type) => DrawCollider(c, type);
        [DrawGizmo(Always)] static void Draw(SphereCollider c, GizmoType type) => DrawCollider(c, type);
        [DrawGizmo(Always)] static void Draw(CapsuleCollider c, GizmoType type) => DrawCollider(c, type);
        [DrawGizmo(Always)] static void Draw(MeshCollider c, GizmoType type) => DrawCollider(c, type);
        [DrawGizmo(Always)] static void Draw(CharacterController c, GizmoType type) => DrawCollider(c, type);
        [DrawGizmo(Always)] static void Draw(BoxCollider2D c, GizmoType type) => DrawCollider2D(c, type);
        [DrawGizmo(Always)] static void Draw(CircleCollider2D c, GizmoType type) => DrawCollider2D(c, type);
        [DrawGizmo(Always)] static void Draw(CapsuleCollider2D c, GizmoType type) => DrawCollider2D(c, type);
        [DrawGizmo(Always)] static void Draw(PolygonCollider2D c, GizmoType type) => DrawCollider2D(c, type);
        [DrawGizmo(Always)] static void Draw(EdgeCollider2D c, GizmoType type) => DrawCollider2D(c, type);
        [DrawGizmo(Always)] static void Draw(CompositeCollider2D c, GizmoType type) => DrawCollider2D(c, type);

        static void DrawCollider(Collider collider, GizmoType type)
        {
            if (!ShouldDraw(collider, collider.isTrigger)) return;
            var (wire, fill) = Colors(collider, collider.isTrigger, type);
            var t = collider.transform;

            switch (collider)
            {
                case BoxCollider box:
                    Gizmos.matrix = t.localToWorldMatrix;
                    Gizmos.color = fill;
                    Gizmos.DrawCube(box.center, box.size);
                    Gizmos.color = wire;
                    Gizmos.DrawWireCube(box.center, box.size);
                    break;
                case SphereCollider sphere:
                {
                    Vector3 s = Abs(t.lossyScale);
                    float radius = sphere.radius * Mathf.Max(s.x, s.y, s.z);
                    Vector3 center = t.TransformPoint(sphere.center);
                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.color = fill;
                    Gizmos.DrawSphere(center, radius);
                    Gizmos.color = wire;
                    Gizmos.DrawWireSphere(center, radius);
                    break;
                }
                case CapsuleCollider capsule:
                    DrawCapsule(t, capsule.center, capsule.radius, capsule.height, capsule.direction, wire, fill);
                    break;
                case CharacterController controller:
                    DrawCapsule(t, controller.center, controller.radius, controller.height, 1, wire, fill);
                    break;
                case MeshCollider mesh when mesh.sharedMesh != null:
                    Gizmos.matrix = t.localToWorldMatrix;
                    Gizmos.color = fill;
                    Gizmos.DrawMesh(mesh.sharedMesh);
                    Gizmos.color = wire;
                    Gizmos.DrawWireMesh(mesh.sharedMesh);
                    break;
                default:
                    DrawBounds(collider.bounds, wire, fill);
                    break;
            }
            Gizmos.matrix = Matrix4x4.identity;
            DrawLabel(collider, collider.bounds, wire, type);
        }

        static void DrawCollider2D(Collider2D collider, GizmoType type)
        {
            if (!ShouldDraw(collider, collider.isTrigger)) return;
            var (wire, fill) = Colors(collider, collider.isTrigger, type);
            var t = collider.transform;

            switch (collider)
            {
                case BoxCollider2D box:
                    Gizmos.matrix = t.localToWorldMatrix;
                    Gizmos.color = fill;
                    Gizmos.DrawCube(box.offset, box.size);
                    Gizmos.color = wire;
                    Gizmos.DrawWireCube(box.offset, box.size);
                    break;
                case CircleCollider2D circle:
                {
                    Vector3 s = Abs(t.lossyScale);
                    float radius = circle.radius * Mathf.Max(s.x, s.y);
                    Vector3 center = t.TransformPoint(circle.offset);
                    Handles.color = fill;
                    Handles.DrawSolidDisc(center, t.forward, radius);
                    Handles.color = wire;
                    Handles.DrawWireDisc(center, t.forward, radius);
                    break;
                }
                case PolygonCollider2D polygon:
                    Gizmos.color = wire;
                    for (int p = 0; p < polygon.pathCount; p++) DrawPolyline(t, polygon.offset, polygon.GetPath(p), true);
                    break;
                case EdgeCollider2D edge:
                    Gizmos.color = wire;
                    DrawPolyline(t, edge.offset, edge.points, false);
                    break;
                default:
                    DrawBounds(collider.bounds, wire, fill);
                    break;
            }
            Gizmos.matrix = Matrix4x4.identity;
            DrawLabel(collider, collider.bounds, wire, type);
        }

        // DebugMarker の名前を出す。当たり判定がなければ（出現地点など）、位置に小さな印も描く
        [DrawGizmo(Always)]
        static void DrawMarker(DebugMarker marker, GizmoType type)
        {
            if (!VisualizationSettings.Enabled) return;
            var colliders = marker.GetComponents<Collider>();
            var colliders2D = marker.GetComponents<Collider2D>();
            if (colliders.Length > 0 || colliders2D.Length > 0)
            {
                // 形は当たり判定ごとに描かれるので、ここでは名前を1回だけ出す
                bool trigger = colliders.Length > 0 ? colliders[0].isTrigger : colliders2D[0].isTrigger;
                Bounds bounds = colliders.Length > 0 ? colliders[0].bounds : colliders2D[0].bounds;
                foreach (var c in colliders) bounds.Encapsulate(c.bounds);
                foreach (var c in colliders2D) bounds.Encapsulate(c.bounds);
                DrawLabel(marker, bounds, Colors(marker, trigger, type).wire, type);
                return;
            }
            var (wire, fill) = Colors(marker, false, type);
            Vector3 p = marker.transform.position;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = fill;
            Gizmos.DrawSphere(p, MarkerSize * 0.5f);
            Gizmos.color = wire;
            Gizmos.DrawLine(p - Vector3.right * MarkerSize, p + Vector3.right * MarkerSize);
            Gizmos.DrawLine(p - Vector3.up * MarkerSize, p + Vector3.up * MarkerSize);
            Gizmos.DrawLine(p - Vector3.forward * MarkerSize, p + Vector3.forward * MarkerSize);
            DrawLabel(marker, new Bounds(p, Vector3.one * MarkerSize), wire, type);
        }

        // 描くのは、DebugMarker を付けたオブジェクトの当たり判定（何でも）と、
        // 付けていなくても、見た目（Renderer）が自分にも子にもない当たり判定と、すべてのトリガー（カメラとライトは除く）
        static bool ShouldDraw(Component collider, bool isTrigger)
        {
            if (!VisualizationSettings.Enabled) return false;
            var go = collider.gameObject;
            if (go.GetComponent<DebugMarker>() != null) return true;
            if (go.GetComponent<Camera>() != null || go.GetComponent<Light>() != null) return false;
            if (isTrigger) return true;
            return go.GetComponentInChildren<Renderer>() == null;
        }

        static (Color wire, Color fill) Colors(Component component, bool isTrigger, GizmoType type)
        {
            var marker = component.GetComponent<DebugMarker>();
            Color baseColor = marker != null && marker.UseCustomColor ? marker.Color : isTrigger ? TriggerColor : SolidColor;
            bool selected = (type & GizmoType.Selected) != 0;
            Color wire = baseColor;
            wire.a = selected ? 1f : 0.8f;
            Color fill = baseColor;
            fill.a = selected ? SelectedFillAlpha : FillAlpha;
            return (wire, fill);
        }

        static void DrawLabel(Component component, Bounds bounds, Color color, GizmoType type)
        {
            var marker = component.GetComponent<DebugMarker>();
            bool selected = (type & GizmoType.Selected) != 0;
            // マーカーがあれば、名前はマーカー側で1回だけ出す。なければ選んだときだけ出す
            if (marker != null && component is not DebugMarker) return;
            if (marker == null && !selected) return;
            labelStyle ??= new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.LowerCenter };
            labelStyle.normal.textColor = new Color(color.r, color.g, color.b, 1f);
            string text = marker != null ? marker.Label : component.name;
            Handles.Label(bounds.center + Vector3.up * bounds.extents.y, text, labelStyle);
        }

        static void DrawCapsule(Transform t, Vector3 center, float radius, float height, int direction, Color wire, Color fill)
        {
            Vector3 s = Abs(t.lossyScale);
            float axisScale = direction == 0 ? s.x : direction == 1 ? s.y : s.z;
            float radiusScale = direction == 0 ? Mathf.Max(s.y, s.z) : direction == 1 ? Mathf.Max(s.x, s.z) : Mathf.Max(s.x, s.y);
            float r = radius * radiusScale;
            float half = Mathf.Max(height * axisScale * 0.5f - r, 0f);

            Gizmos.matrix = Matrix4x4.TRS(t.TransformPoint(center), t.rotation, Vector3.one);
            Vector3 axis = direction == 0 ? Vector3.right : direction == 1 ? Vector3.up : Vector3.forward;
            Vector3 side1 = direction == 0 ? Vector3.up : Vector3.right;
            Vector3 side2 = direction == 2 ? Vector3.up : Vector3.forward;
            Vector3 top = axis * half;
            Vector3 bottom = -axis * half;

            Gizmos.color = fill;
            Gizmos.DrawSphere(top, r);
            Gizmos.DrawSphere(bottom, r);
            Gizmos.color = wire;
            Gizmos.DrawWireSphere(top, r);
            Gizmos.DrawWireSphere(bottom, r);
            Gizmos.DrawLine(top + side1 * r, bottom + side1 * r);
            Gizmos.DrawLine(top - side1 * r, bottom - side1 * r);
            Gizmos.DrawLine(top + side2 * r, bottom + side2 * r);
            Gizmos.DrawLine(top - side2 * r, bottom - side2 * r);
        }

        static void DrawBounds(Bounds bounds, Color wire, Color fill)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = fill;
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = wire;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        static void DrawPolyline(Transform t, Vector2 offset, Vector2[] points, bool closed)
        {
            if (points == null || points.Length < 2) return;
            Gizmos.matrix = Matrix4x4.identity;
            int count = closed ? points.Length : points.Length - 1;
            for (int i = 0; i < count; i++)
            {
                Vector3 a = t.TransformPoint(points[i] + offset);
                Vector3 b = t.TransformPoint(points[(i + 1) % points.Length] + offset);
                Gizmos.DrawLine(a, b);
            }
        }

        static Vector3 Abs(Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}
