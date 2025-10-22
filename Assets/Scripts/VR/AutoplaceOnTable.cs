using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections;

public class AutoplaceOnTable : MonoBehaviour
{
    [Header("Assign your miniature Deathrun root prefab here")]
    public GameObject deathrunPrefab;

    [Tooltip("Vertical offset to avoid z-fighting with the table top (meters)")]
    public float lift = 0.02f;

    [Tooltip("Fallback: if no table is found, allow placing on FLOOR")]
    public bool allowFloorFallback = true;

    bool placed;
    private Coroutine waitForMRUKCoroutine;

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
        if (placed || MRUK.Instance == null || deathrunPrefab == null) return;

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

        Vector3 center = target.GetAnchorCenter();
        Quaternion rot = Quaternion.identity;

        var go = Instantiate(deathrunPrefab, center + Vector3.up * lift, rot);
        placed = true;
        Debug.Log($"[AutoplaceOnTable] Placed on {target.Label} at {go.transform.position}");
    }
}
