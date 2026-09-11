using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a designated region where the Black Hole can function.
/// Attach this component to an empty/invisible GameObject with any 2D Collider (e.g., BoxCollider2D, CircleCollider2D).
/// You can duplicate (Ctrl+D) this object across your scene to create multiple functional areas.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
[SelectionBase]
public class BlackHoleZone : MonoBehaviour
{
    private static readonly List<BlackHoleZone> activeZones = new List<BlackHoleZone>();

    /// <summary>
    /// Read-only access to all currently active Black Hole Zones in the scene.
    /// </summary>
    public static IReadOnlyList<BlackHoleZone> ActiveZones => activeZones;

    /// <summary>
    /// True if there is at least one active zone in the scene.
    /// </summary>
    public static bool HasAnyZones => activeZones.Count > 0;

    [Header("Gizmo Visualization (Editor Only)")]
    [Tooltip("Color of the zone wireframe and fill in the Scene view.")]
    [SerializeField] private Color zoneColor = new Color(0.15f, 0.85f, 1f, 0.2f);

    [Tooltip("Border outline color in the Scene view.")]
    [SerializeField] private Color borderColor = new Color(0.15f, 0.85f, 1f, 0.85f);

    private Collider2D cachedCollider;

    public Collider2D ZoneCollider
    {
        get
        {
            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<Collider2D>();
            }
            return cachedCollider;
        }
    }

    private void Awake()
    {
        EnsureTrigger();
    }

    private void Reset()
    {
        EnsureTrigger();
    }

    private void OnValidate()
    {
        EnsureTrigger();
    }

    private void OnEnable()
    {
        if (!activeZones.Contains(this))
        {
            activeZones.Add(this);
        }
    }

    private void OnDisable()
    {
        activeZones.Remove(this);
    }

    private void EnsureTrigger()
    {
        if (ZoneCollider != null && !ZoneCollider.isTrigger)
        {
            ZoneCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// Checks if a 2D world position lies within this zone's collider boundary.
    /// </summary>
    public bool Contains(Vector2 worldPosition)
    {
        if (ZoneCollider == null || !ZoneCollider.enabled) return false;
        return ZoneCollider.OverlapPoint(worldPosition);
    }

    /// <summary>
    /// Returns the closest point on or inside this zone's collider to the target position.
    /// </summary>
    public Vector2 ClosestPoint(Vector2 worldPosition)
    {
        if (ZoneCollider == null) return worldPosition;
        return ZoneCollider.ClosestPoint(worldPosition);
    }

    #region Static Helper Queries

    /// <summary>
    /// Checks whether a given 2D world point is inside ANY active Black Hole Zone.
    /// </summary>
    public static bool IsPointInsideAnyZone(Vector2 worldPoint, out BlackHoleZone hitZone)
    {
        for (int i = 0; i < activeZones.Count; i++)
        {
            var zone = activeZones[i];
            if (zone != null && zone.Contains(worldPoint))
            {
                hitZone = zone;
                return true;
            }
        }

        hitZone = null;
        return false;
    }

    /// <summary>
    /// Finds the closest point across ALL active zones to the specified world point.
    /// </summary>
    public static Vector2 GetClosestPointInAnyZone(Vector2 worldPoint, out BlackHoleZone nearestZone)
    {
        nearestZone = null;
        if (activeZones.Count == 0) return worldPoint;

        float minSqrDistance = float.MaxValue;
        Vector2 bestPoint = worldPoint;

        for (int i = 0; i < activeZones.Count; i++)
        {
            var zone = activeZones[i];
            if (zone == null || zone.ZoneCollider == null) continue;

            Vector2 candidate = zone.ClosestPoint(worldPoint);
            float sqrDist = (candidate - worldPoint).sqrMagnitude;

            // If the point is already inside a zone, sqrDist is 0 (immediate match)
            if (sqrDist < 0.0001f)
            {
                nearestZone = zone;
                return worldPoint;
            }

            if (sqrDist < minSqrDistance)
            {
                minSqrDistance = sqrDist;
                bestPoint = candidate;
                nearestZone = zone;
            }
        }

        return bestPoint;
    }

    #endregion

    #region Editor Scene Gizmos

    private void OnDrawGizmos()
    {
        DrawGizmoVisuals(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmoVisuals(true);
    }

    private void DrawGizmoVisuals(bool isSelected)
    {
        var col = ZoneCollider;
        if (col == null) return;

        Color fill = isSelected ? new Color(zoneColor.r, zoneColor.g, zoneColor.b, Mathf.Clamp01(zoneColor.a * 2f)) : zoneColor;
        Color border = isSelected ? Color.white : borderColor;

        Matrix4x4 originalMatrix = Gizmos.matrix;

        if (col is BoxCollider2D box)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            
            // Draw transparent area
            Gizmos.color = fill;
            Gizmos.DrawCube(box.offset, box.size);

            // Draw crisp outline
            Gizmos.color = border;
            Gizmos.DrawWireCube(box.offset, box.size);
        }
        else if (col is CircleCollider2D circle)
        {
            Vector3 center = transform.TransformPoint(circle.offset);
            float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            float radius = circle.radius * maxScale;

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = fill;
            Gizmos.DrawSphere(center, radius);

            Gizmos.color = border;
            Gizmos.DrawWireSphere(center, radius);
        }
        else if (col is CapsuleCollider2D capsule)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.color = fill;
            Gizmos.DrawCube(capsule.offset, capsule.size);
            Gizmos.color = border;
            Gizmos.DrawWireCube(capsule.offset, capsule.size);
        }

        Gizmos.matrix = originalMatrix;
    }

    #endregion
}
