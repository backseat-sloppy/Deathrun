using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections;
using System.Linq;

public class AutoplaceOnTable : MonoBehaviour
{
    [Header("Assign your prefabs here")]
    [Tooltip("Base platform prefab that should cover the entire table top (optional).")]
    public GameObject platformPrefab;

    [Tooltip("Optional override for the platform prefab footprint in meters (X = width, Y = depth). Leave zero to auto-measure.")]
    public Vector2 platformFootprintOverride = Vector2.zero;

    [Tooltip("Deathrun content prefab. Required.")]
    public GameObject deathrunPrefab;

    [Tooltip("Optional override for the deathrun prefab footprint in meters (X = width, Y = depth). Leave zero to auto-measure.")]
    public Vector2 deathrunFootprintOverride = Vector2.zero;

    [Tooltip("Vertical offset to avoid z-fighting with the table top (meters)")]
    public float lift = 0.02f;

    [Tooltip("Fallback: if no table is found, allow placing on FLOOR")]
    public bool allowFloorFallback = true;

    [Header("Deathrun layout")]
    [Tooltip("Extra padding (meters) to keep the detailed deathrun content away from the table edge.")]
    public float deathrunEdgePadding = 0.02f;

    [Tooltip("If true, the detailed deathrun content keeps its aspect ratio when scaled.")]
    public bool maintainDeathrunAspect = true;

    [Tooltip("Vertical spacing (meters) between the platform top and the deathrun content.")]
    public float deathrunHeightOffset = 0.03f;

    [Tooltip("If true, disables all automatic scaling and uses original prefab size.")]
    public bool disableAutoScaling = false;

    [Tooltip("When auto-scaling is enabled, use simple uniform scaling instead of complex table fitting.")]
    public bool useSimpleScaling = true;

    [Tooltip("Maximum scale factor to apply when using simple scaling (prevents extreme sizes).")]
    [Range(0.01f, 2.0f)]
    public float maxScaleFactor = 0.3f;

    [Tooltip("If greater than 0, ignores automatic scaling and uses this fixed scale factor instead.")]
    [Range(0f, 1f)]
    public float manualScaleOverride = 0f;

    bool placed;
    private Coroutine waitForMRUKCoroutine;

    [Header("Debug")]
    [Tooltip("Click to reset placement for testing")]
    public bool resetPlacement = false;

    void OnEnable()
    {
        // If MRUK has already been initialized, subscribe immediately.
        if (MRUK.Instance != null)
        {
            SubscribeToMRUK();
        }
        else
        {
            // Otherwise wait for initialization.
            Debug.LogWarning("[AutoplaceOnTable] MRUK.Instance is null. Waiting for MRUK initialization.");
            waitForMRUKCoroutine = StartCoroutine(WaitForMRUK());
        }
    }

    void Update()
    {
        // Debug helper for testing
        if (resetPlacement)
        {
            resetPlacement = false;
            ResetPlacement();
        }
    }

    /// <summary>
    /// Reset the placement state and try placing again. Useful for testing.
    /// </summary>
    public void ResetPlacement()
    {
        placed = false;
        
        // Destroy any existing placement
        var existingPlacements = GameObject.FindObjectsOfType<GameObject>()
            .Where(go => go.name == "DeathrunPlacement")
            .ToArray();
        
        foreach (var placement in existingPlacements)
        {
            if (Application.isPlaying)
                DestroyImmediate(placement);
        }
        
        Debug.Log("[AutoplaceOnTable] Placement reset. Will try placing again.");
        TryPlace();
    }

    void OnDisable()
    {
        UnsubscribeFromMRUK();
        if (waitForMRUKCoroutine != null)
        {
            StopCoroutine(waitForMRUKCoroutine);
            waitForMRUKCoroutine = null;
        }
    }

    // Subscribe to MRUK's events
    private void SubscribeToMRUK()
    {
        MRUK.Instance.SceneLoadedEvent.AddListener(OnSceneLoaded);
        MRUK.Instance.RoomCreatedEvent.AddListener(OnRoomChanged);
        MRUK.Instance.RoomUpdatedEvent.AddListener(OnRoomChanged);
    }

    // Unsubscribe from MRUK's events
    private void UnsubscribeFromMRUK()
    {
        if (MRUK.Instance == null) return;
        MRUK.Instance.SceneLoadedEvent.RemoveListener(OnSceneLoaded);
        MRUK.Instance.RoomCreatedEvent.RemoveListener(OnRoomChanged);
        MRUK.Instance.RoomUpdatedEvent.RemoveListener(OnRoomChanged);
    }

    // Coroutine to wait until MRUK initializes
    private IEnumerator WaitForMRUK()
    {
        yield return new WaitUntil(() => MRUK.Instance != null);
        SubscribeToMRUK();
    }

    // Callback for when the scene model is loaded
    private void OnSceneLoaded()
    {
        TryPlace();
    }

    // Callback for when a room is created or updated
    private void OnRoomChanged(MRUKRoom room)
    {
        TryPlace();
    }

    // Attempt to place the prefab on a table anchor or floor if allowed
    void TryPlace()
    {
        // Only place once, and only if MRUK is initialized and prefab assigned
        if (placed)
        {
            Debug.Log("[AutoplaceOnTable] Already placed, skipping.");
            return;
        }
        
        if (MRUK.Instance == null)
        {
            Debug.LogWarning("[AutoplaceOnTable] MRUK.Instance is null, cannot place.");
            return;
        }
        
        if (deathrunPrefab == null)
        {
            Debug.LogError("[AutoplaceOnTable] deathrunPrefab is null! Please assign the SCENETEST prefab to the 'Deathrun content prefab' field in the inspector.");
            return;
        }

        var room = MRUK.Instance.GetCurrentRoom();
        if (room == null) return;

        // Prefer table anchors
        MRUKAnchor table = null;
        foreach (var a in room.Anchors)
        {
            if ((a.Label & MRUKAnchor.SceneLabels.TABLE) != 0)
            {
                table = a;
                break;
            }
        }

        // Fallback to floor if no table and allowed
        MRUKAnchor target = table;
        if (target == null && allowFloorFallback)
        {
            foreach (var a in room.Anchors)
            {
                if ((a.Label & MRUKAnchor.SceneLabels.FLOOR) != 0)
                {
                    target = a;
                    break;
                }
            }
        }

        if (target == null) return;

        bool targetIsTable = target.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE);

        if (!TryGetSurfaceData(target, out var surfaceData))
        {
            return;
        }

        Quaternion rotation = Quaternion.LookRotation(surfaceData.Forward, surfaceData.Normal);

        var rootGO = new GameObject("DeathrunPlacement");
        var rootTransform = rootGO.transform;
        rootTransform.rotation = rotation;
        rootTransform.localScale = Vector3.one;

        Transform platformTransform = null;
        if (platformPrefab != null)
        {
            platformTransform = Instantiate(platformPrefab, rootTransform).transform;
            ResetChildTransform(platformTransform);
        }

        var deathrunTransform = Instantiate(deathrunPrefab, rootTransform).transform;
        ResetChildTransform(deathrunTransform);

        if (targetIsTable && !disableAutoScaling)
        {
            if (useSimpleScaling)
            {
                Debug.Log($"[AutoplaceOnTable] Applying simple scaling. Surface size: {surfaceData.Size}");
                ApplySimpleScaling(rootTransform, deathrunTransform, surfaceData);
            }
            else
            {
                Debug.Log($"[AutoplaceOnTable] Applying complex table scaling. Surface size: {surfaceData.Size}");
                ApplyTableScaling(rootTransform, platformTransform, deathrunTransform, surfaceData);
            }
        }
        else if (targetIsTable && disableAutoScaling)
        {
            Debug.Log("[AutoplaceOnTable] Auto-scaling disabled, using original prefab size.");
        }
        else
        {
            Debug.Log("[AutoplaceOnTable] Placing on floor, skipping table scaling.");
        }

        var adjustedBounds = CalculateLocalBounds(rootTransform);
        Debug.Log($"[AutoplaceOnTable] Calculated bounds: {adjustedBounds}");
        AlignObjectToSurface(rootTransform, adjustedBounds, surfaceData.Center, surfaceData.Normal);

        if (deathrunTransform != null && !Mathf.Approximately(deathrunHeightOffset, 0f))
        {
            deathrunTransform.position += surfaceData.Normal * deathrunHeightOffset;
        }

        placed = true;
        Debug.Log($"[AutoplaceOnTable] Placed on {target.Label} at {rootTransform.position}");
    }

    private static void ResetChildTransform(Transform child)
    {
        if (child == null)
        {
            return;
        }

        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
    }

    private void ApplySimpleScaling(Transform root, Transform content, SurfaceData surfaceData)
    {
        if (content == null || surfaceData.Size.x <= 0 || surfaceData.Size.y <= 0)
        {
            Debug.LogWarning("[AutoplaceOnTable] Cannot apply simple scaling - invalid content or surface size.");
            return;
        }

        // Calculate a simple scale factor based on the smaller dimension to ensure it fits
        var contentBounds = CalculateLocalBounds(content);
        if (contentBounds.size.x <= 0 || contentBounds.size.z <= 0)
        {
            Debug.LogWarning("[AutoplaceOnTable] Cannot calculate content bounds for simple scaling.");
            return;
        }

        float uniformScale;
        
        if (manualScaleOverride > 0f)
        {
            // Use manual override
            uniformScale = manualScaleOverride;
            Debug.Log($"[AutoplaceOnTable] Using manual scale override: {uniformScale}");
        }
        else
        {
            // Calculate automatic scale
            float scaleX = surfaceData.Size.x / contentBounds.size.x;
            float scaleZ = surfaceData.Size.y / contentBounds.size.z;
            
            // Use the smaller scale to ensure it fits, and clamp to reasonable bounds
            uniformScale = Mathf.Min(scaleX, scaleZ) * 0.2f; // 0.2f for much smaller size - use only 20% of available space
            uniformScale = Mathf.Clamp(uniformScale, 0.01f, maxScaleFactor);
            
            Debug.Log($"[AutoplaceOnTable] Auto-calculated scale: content bounds {contentBounds.size}, surface {surfaceData.Size}, scale: {uniformScale}");
        }

        Vector3 newScale = content.localScale * uniformScale;
        content.localScale = newScale;
        Debug.Log($"[AutoplaceOnTable] Applied scale: {uniformScale}, final scale: {newScale}");
    }

    private void ApplyTableScaling(Transform root, Transform platform, Transform content, SurfaceData surfaceData)
    {
        if (root == null)
        {
            return;
        }

        Vector2 targetSize = surfaceData.Size;
        if (targetSize.x <= Mathf.Epsilon || targetSize.y <= Mathf.Epsilon)
        {
            return;
        }

        bool platformScaled = false;

        if (platform != null)
        {
            Vector2 platformSize = platformFootprintOverride;
            if (platformSize.x <= Mathf.Epsilon || platformSize.y <= Mathf.Epsilon)
            {
                platformSize = CalculateProjectedSize(platform, surfaceData.Right, surfaceData.Forward);
            }

            if (platformSize.x > Mathf.Epsilon && platformSize.y > Mathf.Epsilon)
            {
                ScaleSurfaceFill(platform, surfaceData.Size, platformSize);
                platformScaled = true;
            }
        }

        bool contentScaled = false;

        if (content != null)
        {
            Vector2 contentSize = deathrunFootprintOverride;
            if (contentSize.x <= Mathf.Epsilon || contentSize.y <= Mathf.Epsilon)
            {
                contentSize = CalculateProjectedSize(content, surfaceData.Right, surfaceData.Forward);
            }

            Debug.Log($"[AutoplaceOnTable] Content size calculated: {contentSize}, Surface size: {surfaceData.Size}");

            if (contentSize.x > Mathf.Epsilon && contentSize.y > Mathf.Epsilon)
            {
                Vector3 originalScale = content.localScale;
                ScaleDeathrunContent(content, surfaceData.Size, contentSize);
                Debug.Log($"[AutoplaceOnTable] Content scaled from {originalScale} to {content.localScale}");
                contentScaled = true;
            }
            else
            {
                Debug.LogWarning($"[AutoplaceOnTable] Content size is too small or zero: {contentSize}");
            }
        }

        if (!platformScaled && !contentScaled)
        {
            Vector2 rootSize = platformFootprintOverride;
            if (rootSize.x <= Mathf.Epsilon || rootSize.y <= Mathf.Epsilon)
            {
                rootSize = deathrunFootprintOverride;
            }

            if (rootSize.x <= Mathf.Epsilon || rootSize.y <= Mathf.Epsilon)
            {
                rootSize = CalculateProjectedSize(root, surfaceData.Right, surfaceData.Forward);
            }

            if (rootSize.x > Mathf.Epsilon && rootSize.y > Mathf.Epsilon)
            {
                ScaleSurfaceFill(root, surfaceData.Size, rootSize);
            }
        }
    }

    private static void ScaleSurfaceFill(Transform target, Vector2 targetSize, Vector2 baseSize)
    {
        if (target == null)
        {
            return;
        }

        if (baseSize.x <= Mathf.Epsilon || baseSize.y <= Mathf.Epsilon)
        {
            return;
        }

        if (targetSize.x <= Mathf.Epsilon || targetSize.y <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 scale = target.localScale;
        float originalY = scale.y;
        scale.x *= targetSize.x / baseSize.x;
        scale.z *= targetSize.y / baseSize.y;
        scale.y = originalY;
        target.localScale = scale;
    }

    private void ScaleDeathrunContent(Transform target, Vector2 surfaceSize, Vector2 baseSize)
    {
        if (target == null)
        {
            return;
        }

        if (surfaceSize.x <= Mathf.Epsilon || surfaceSize.y <= Mathf.Epsilon)
        {
            return;
        }

        if (baseSize.x <= Mathf.Epsilon || baseSize.y <= Mathf.Epsilon)
        {
            return;
        }

        float padding = Mathf.Max(0f, deathrunEdgePadding);
        float availableX = Mathf.Max(surfaceSize.x - 2f * padding, 0.001f);
        float availableZ = Mathf.Max(surfaceSize.y - 2f * padding, 0.001f);

        if (maintainDeathrunAspect)
        {
            float uniform = Mathf.Min(availableX / baseSize.x, availableZ / baseSize.y);
            Debug.Log($"[AutoplaceOnTable] Uniform scale factor: {uniform}, availableX: {availableX}, availableZ: {availableZ}, baseSize: {baseSize}");
            
            if (uniform <= 0f)
            {
                Debug.LogWarning("[AutoplaceOnTable] Uniform scale factor is zero or negative, skipping scaling.");
                return;
            }

            // Clamp scaling to reasonable bounds to prevent distortion
            uniform = Mathf.Clamp(uniform, 0.1f, 10f);

            Vector3 scale = target.localScale;
            float originalY = scale.y;
            scale.x *= uniform;
            scale.z *= uniform;
            scale.y = originalY;
            target.localScale = scale;
            Debug.Log($"[AutoplaceOnTable] Applied uniform scale: {uniform}, new scale: {scale}");
        }
        else
        {
            float scaleX = availableX / baseSize.x;
            float scaleZ = availableZ / baseSize.y;
            
            // Clamp scaling to reasonable bounds to prevent distortion
            scaleX = Mathf.Clamp(scaleX, 0.1f, 10f);
            scaleZ = Mathf.Clamp(scaleZ, 0.1f, 10f);
            
            Debug.Log($"[AutoplaceOnTable] Non-uniform scaling - X: {scaleX}, Z: {scaleZ}");

            Vector3 scale = target.localScale;
            float originalY = scale.y;
            scale.x *= scaleX;
            scale.z *= scaleZ;
            scale.y = originalY;
            target.localScale = scale;
            Debug.Log($"[AutoplaceOnTable] Applied non-uniform scale, new scale: {scale}");
        }
    }

    private void AlignObjectToSurface(Transform root, Bounds localBounds, Vector3 surfacePoint, Vector3 surfaceNormal)
    {
        Vector3 localBaseCenter = new Vector3(localBounds.center.x, localBounds.min.y, localBounds.center.z);
        Vector3 worldBaseCenter = root.TransformPoint(localBaseCenter);
        Vector3 adjustment = surfacePoint - worldBaseCenter + surfaceNormal * lift;
        root.position += adjustment;
    }

    private static Bounds CalculateLocalBounds(Transform root)
    {
        bool hasPoint = false;
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);

        ForEachModelCorner(root, worldCorner =>
        {
            Vector3 localCorner = root.InverseTransformPoint(worldCorner);
            if (!hasPoint)
            {
                localBounds = new Bounds(localCorner, Vector3.zero);
                hasPoint = true;
            }
            else
            {
                localBounds.Encapsulate(localCorner);
            }
        });

        return hasPoint ? localBounds : new Bounds(Vector3.zero, Vector3.zero);
    }

    private static Vector2 CalculateProjectedSize(Transform root, Vector3 axisRight, Vector3 axisForward)
    {
        axisRight = axisRight.normalized;
        axisForward = axisForward.normalized;

        if (axisRight.sqrMagnitude < 1e-6f || axisForward.sqrMagnitude < 1e-6f)
        {
            return Vector2.zero;
        }

        float minRight = float.PositiveInfinity;
        float maxRight = float.NegativeInfinity;
        float minForward = float.PositiveInfinity;
        float maxForward = float.NegativeInfinity;
        bool hasPoint = false;
        Vector3 origin = root.position;

        ForEachModelCorner(root, worldCorner =>
        {
            hasPoint = true;
            Vector3 offset = worldCorner - origin;
            float projRight = Vector3.Dot(offset, axisRight);
            float projForward = Vector3.Dot(offset, axisForward);

            if (projRight < minRight) minRight = projRight;
            if (projRight > maxRight) maxRight = projRight;
            if (projForward < minForward) minForward = projForward;
            if (projForward > maxForward) maxForward = projForward;
        });

        if (!hasPoint)
        {
            return Vector2.zero;
        }

        return new Vector2(maxRight - minRight, maxForward - minForward);
    }

    private static bool TryGetSurfaceData(MRUKAnchor anchor, out SurfaceData data)
    {
        data = default;

        Vector3 normal = anchor.transform.forward;
        if (Vector3.Dot(normal, Vector3.up) < 0f)
        {
            normal = -normal;
        }

        if (!TryBuildPlaneAxes(anchor, normal, out var planeRight, out var planeForward))
        {
            return false;
        }

        Vector3 surfaceCenter;
        Vector2 surfaceSize;

        if (anchor.VolumeBounds.HasValue)
        {
            var bounds = anchor.VolumeBounds.Value;
            surfaceCenter = anchor.transform.TransformPoint(new Vector3(bounds.center.x, bounds.center.y, bounds.max.z));
            surfaceSize = CalculateSurfaceSize(anchor, bounds, planeRight, planeForward);
        }
        else if (anchor.PlaneRect.HasValue)
        {
            var rect = anchor.PlaneRect.Value;
            surfaceCenter = anchor.transform.TransformPoint(new Vector3(rect.center.x, rect.center.y, 0f));
            surfaceSize = CalculateSurfaceSize(anchor, rect, planeRight, planeForward);
        }
        else
        {
            surfaceCenter = anchor.transform.position;
            surfaceSize = Vector2.zero;
        }

        data = new SurfaceData
        {
            Center = surfaceCenter,
            Normal = normal.normalized,
            Right = planeRight,
            Forward = planeForward,
            Size = surfaceSize
        };

        return true;
    }

    private static bool TryBuildPlaneAxes(MRUKAnchor anchor, Vector3 normal, out Vector3 planeRight, out Vector3 planeForward)
    {
        planeRight = Vector3.ProjectOnPlane(anchor.transform.right, normal);
        if (planeRight.sqrMagnitude < 1e-4f)
        {
            planeRight = Vector3.ProjectOnPlane(anchor.transform.up, normal);
        }

        if (planeRight.sqrMagnitude < 1e-4f)
        {
            Vector3 axis = Mathf.Abs(Vector3.Dot(normal, Vector3.right)) > 0.9f ? Vector3.forward : Vector3.right;
            planeRight = Vector3.Cross(normal, axis);
        }

        if (planeRight.sqrMagnitude < 1e-4f)
        {
            planeForward = Vector3.zero;
            return false;
        }

        planeRight.Normalize();
        planeForward = Vector3.Cross(normal, planeRight).normalized;
        return planeForward.sqrMagnitude >= 1e-4f;
    }

    private static Vector2 CalculateSurfaceSize(MRUKAnchor anchor, Bounds bounds, Vector3 planeRight, Vector3 planeForward)
    {
        Vector3 localCenterTop = new Vector3(bounds.center.x, bounds.center.y, bounds.max.z);
        Vector3 worldCenter = anchor.transform.TransformPoint(localCenterTop);
        Vector3 extents = bounds.extents;

        float minRight = float.PositiveInfinity;
        float maxRight = float.NegativeInfinity;
        float minForward = float.PositiveInfinity;
        float maxForward = float.NegativeInfinity;

        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sy = -1; sy <= 1; sy += 2)
            {
                Vector3 localCorner = localCenterTop + new Vector3(extents.x * sx, extents.y * sy, 0f);
                Vector3 worldCorner = anchor.transform.TransformPoint(localCorner);
                Vector3 offset = worldCorner - worldCenter;
                float projRight = Vector3.Dot(offset, planeRight);
                float projForward = Vector3.Dot(offset, planeForward);

                minRight = Mathf.Min(minRight, projRight);
                maxRight = Mathf.Max(maxRight, projRight);
                minForward = Mathf.Min(minForward, projForward);
                maxForward = Mathf.Max(maxForward, projForward);
            }
        }

        return new Vector2(maxRight - minRight, maxForward - minForward);
    }

    private static Vector2 CalculateSurfaceSize(MRUKAnchor anchor, Rect rect, Vector3 planeRight, Vector3 planeForward)
    {
        Vector3 worldCenter = anchor.transform.TransformPoint(new Vector3(rect.center.x, rect.center.y, 0f));

        float minRight = float.PositiveInfinity;
        float maxRight = float.NegativeInfinity;
        float minForward = float.PositiveInfinity;
        float maxForward = float.NegativeInfinity;

        for (int cx = 0; cx <= 1; cx++)
        {
            for (int cy = 0; cy <= 1; cy++)
            {
                float x = cx == 0 ? rect.xMin : rect.xMax;
                float y = cy == 0 ? rect.yMin : rect.yMax;

                Vector3 localCorner = new Vector3(x, y, 0f);
                Vector3 worldCorner = anchor.transform.TransformPoint(localCorner);
                Vector3 offset = worldCorner - worldCenter;

                float projRight = Vector3.Dot(offset, planeRight);
                float projForward = Vector3.Dot(offset, planeForward);

                minRight = Mathf.Min(minRight, projRight);
                maxRight = Mathf.Max(maxRight, projRight);
                minForward = Mathf.Min(minForward, projForward);
                maxForward = Mathf.Max(maxForward, projForward);
            }
        }

        return new Vector2(maxRight - minRight, maxForward - minForward);
    }

    private static void ForEachModelCorner(Transform root, System.Action<Vector3> handler)
    {
        if (root == null || handler == null)
        {
            return;
        }

        bool processed = false;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            processed = true;
            EmitBoundsCorners(renderer.bounds, handler);
        }

        var colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (var collider in colliders)
        {
            processed = true;
            EmitBoundsCorners(collider.bounds, handler);
        }

        if (!processed)
        {
            handler(root.position);
        }
    }

    private static void EmitBoundsCorners(Bounds bounds, System.Action<Vector3> handler)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        handler(new Vector3(min.x, min.y, min.z));
        handler(new Vector3(max.x, min.y, min.z));
        handler(new Vector3(min.x, max.y, min.z));
        handler(new Vector3(max.x, max.y, min.z));
        handler(new Vector3(min.x, min.y, max.z));
        handler(new Vector3(max.x, min.y, max.z));
        handler(new Vector3(min.x, max.y, max.z));
        handler(new Vector3(max.x, max.y, max.z));
    }

    private struct SurfaceData
    {
        public Vector3 Center;
        public Vector3 Normal;
        public Vector3 Right;
        public Vector3 Forward;
        public Vector2 Size;
    }
}
