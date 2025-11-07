using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

namespace CustomInteraction
{
    /// <summary>
    /// Allows grabbing multiple objects simultaneously with a single VR hand.
    /// When pinching/grabbing, it will collect all Grabbable objects being hovered over
    /// and maintain selection on all of them until release.
    /// </summary>
    public class MultiGrabHandInteractor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        [Tooltip("The hand interactor that detects hover and select states")]
        private HandGrabInteractor _handInteractor;

        [SerializeField]
        [Tooltip("Optional: List of specific Grabbable objects to track. Leave empty to grab any hovered Grabbable.")]
        private List<Grabbable> _targetGrabbables = new List<Grabbable>();

        [Header("Settings")]
        [SerializeField]
        [Tooltip("Maximum number of objects that can be grabbed simultaneously. -1 for unlimited.")]
        private int _maxSimultaneousGrabs = -1;

        [SerializeField]
        [Tooltip("If true, objects will be parented to the hand while grabbed")]
        private bool _parentToHand = false;

        [SerializeField]
        [Tooltip("Local offset position when objects are grabbed")]
        private Vector3 _grabPositionOffset = Vector3.zero;

        [SerializeField]
        [Tooltip("Local offset rotation when objects are grabbed")]
        private Vector3 _grabRotationOffset = Vector3.zero;

        // Track currently grabbed objects
        private List<Grabbable> _currentlyGrabbedObjects = new List<Grabbable>();
        private Dictionary<Grabbable, GrabData> _grabDataMap = new Dictionary<Grabbable, GrabData>();

        private bool _isGrabbing = false;
        private bool _wasGrabbingLastFrame = false;

        private class GrabData
        {
            public Transform OriginalParent;
            public Vector3 LocalPositionOffset;
            public Quaternion LocalRotationOffset;
            public bool WasKinematic;
            public Rigidbody Rigidbody;
        }

        private void Start()
        {
            if (_handInteractor == null)
            {
                _handInteractor = GetComponent<HandGrabInteractor>();
                if (_handInteractor == null)
                {
                    Debug.LogError($"MultiGrabHandInteractor on {gameObject.name} requires a HandGrabInteractor component!");
                    enabled = false;
                    return;
                }
            }
        }

        private void Update()
        {
            // Check if hand is currently in grab/select state
            _isGrabbing = _handInteractor.State == InteractorState.Select;

            // Detect grab started
            if (_isGrabbing && !_wasGrabbingLastFrame)
            {
                OnGrabStarted();
            }
            // Detect grab ended
            else if (!_isGrabbing && _wasGrabbingLastFrame)
            {
                OnGrabEnded();
            }
            // Update grabbed objects while holding
            else if (_isGrabbing)
            {
                UpdateGrabbedObjects();
            }

            _wasGrabbingLastFrame = _isGrabbing;
        }

        private void OnGrabStarted()
        {
            // Find all currently hovered Grabbable objects
            List<Grabbable> hoveredGrabbables = GetHoveredGrabbables();

            // Grab each hovered object
            foreach (Grabbable grabbable in hoveredGrabbables)
            {
                if (_maxSimultaneousGrabs != -1 && _currentlyGrabbedObjects.Count >= _maxSimultaneousGrabs)
                {
                    break;
                }

                GrabObject(grabbable);
            }
        }

        private void OnGrabEnded()
        {
            // Release all grabbed objects
            foreach (Grabbable grabbable in _currentlyGrabbedObjects)
            {
                ReleaseObject(grabbable);
            }

            _currentlyGrabbedObjects.Clear();
            _grabDataMap.Clear();
        }

        private void UpdateGrabbedObjects()
        {
            // Update position/rotation of grabbed objects to follow hand
            Pose handPose = _handInteractor.transform.GetPose();

            foreach (Grabbable grabbable in _currentlyGrabbedObjects)
            {
                if (grabbable == null || !_grabDataMap.ContainsKey(grabbable))
                    continue;

                GrabData data = _grabDataMap[grabbable];
                Transform objectTransform = grabbable.Transform;

                if (_parentToHand)
                {
                    // Objects are parented, so they'll follow automatically
                    // Just apply any additional offsets
                    objectTransform.localPosition = data.LocalPositionOffset + _grabPositionOffset;
                    objectTransform.localRotation = data.LocalRotationOffset * Quaternion.Euler(_grabRotationOffset);
                }
                else
                {
                    // Manually update position to follow hand
                    Vector3 targetPos = handPose.position + handPose.rotation * data.LocalPositionOffset;
                    Quaternion targetRot = handPose.rotation * data.LocalRotationOffset;

                    if (data.Rigidbody != null)
                    {
                        data.Rigidbody.MovePosition(targetPos);
                        data.Rigidbody.MoveRotation(targetRot);
                    }
                    else
                    {
                        objectTransform.position = targetPos;
                        objectTransform.rotation = targetRot;
                    }
                }
            }
        }

        private void GrabObject(Grabbable grabbable)
        {
            if (_currentlyGrabbedObjects.Contains(grabbable))
                return;

            Transform handTransform = _handInteractor.transform;
            Transform objectTransform = grabbable.Transform;

            // Store grab data
            GrabData data = new GrabData
            {
                OriginalParent = objectTransform.parent
            };

            // Calculate local offset from hand to object
            if (_parentToHand)
            {
                data.LocalPositionOffset = handTransform.InverseTransformPoint(objectTransform.position);
                data.LocalRotationOffset = Quaternion.Inverse(handTransform.rotation) * objectTransform.rotation;
            }
            else
            {
                data.LocalPositionOffset = Quaternion.Inverse(handTransform.rotation) * (objectTransform.position - handTransform.position);
                data.LocalRotationOffset = Quaternion.Inverse(handTransform.rotation) * objectTransform.rotation;
            }

            // Handle rigidbody
            Rigidbody rb = objectTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                data.Rigidbody = rb;
                data.WasKinematic = rb.isKinematic;
                rb.isKinematic = true;
            }

            _grabDataMap[grabbable] = data;
            _currentlyGrabbedObjects.Add(grabbable);

            // Optionally parent to hand
            if (_parentToHand)
            {
                objectTransform.SetParent(handTransform);
            }

            Debug.Log($"Grabbed {grabbable.gameObject.name}. Total grabbed: {_currentlyGrabbedObjects.Count}");
        }

        private void ReleaseObject(Grabbable grabbable)
        {
            if (!_grabDataMap.ContainsKey(grabbable))
                return;

            GrabData data = _grabDataMap[grabbable];
            Transform objectTransform = grabbable.Transform;

            // Restore parent
            if (_parentToHand)
            {
                objectTransform.SetParent(data.OriginalParent);
            }

            // Restore rigidbody state
            if (data.Rigidbody != null)
            {
                data.Rigidbody.isKinematic = data.WasKinematic;

                // Optionally apply throw velocity here
                // You can calculate velocity based on hand movement
            }

            Debug.Log($"Released {grabbable.gameObject.name}");
        }

        private List<Grabbable> GetHoveredGrabbables()
        {
            List<Grabbable> hoveredGrabbables = new List<Grabbable>();

            // If specific target grabbables are defined, check those
            if (_targetGrabbables != null && _targetGrabbables.Count > 0)
            {
                foreach (Grabbable grabbable in _targetGrabbables)
                {
                    if (grabbable != null && IsGrabbableHovered(grabbable))
                    {
                        hoveredGrabbables.Add(grabbable);
                    }
                }
            }
            else
            {
                // Find all Grabbable objects in the scene that are being hovered
                Grabbable[] allGrabbables = FindObjectsOfType<Grabbable>();
                foreach (Grabbable grabbable in allGrabbables)
                {
                    if (IsGrabbableHovered(grabbable))
                    {
                        hoveredGrabbables.Add(grabbable);
                    }
                }
            }

            return hoveredGrabbables;
        }

        private bool IsGrabbableHovered(Grabbable grabbable)
        {
            // Check if the hand interactor is hovering over this grabbable
            // This assumes the grabbable implements IInteractable and is registered with the interactor
            if (grabbable is IPointable pointable)
            {
                return _handInteractor.Interactable == pointable ||
                       (_handInteractor.HasCandidate && _handInteractor.Candidate == pointable);
            }

            return false;
        }

        public void AddTargetGrabbable(Grabbable grabbable)
        {
            if (!_targetGrabbables.Contains(grabbable))
            {
                _targetGrabbables.Add(grabbable);
            }
        }

        public void RemoveTargetGrabbable(Grabbable grabbable)
        {
            _targetGrabbables.Remove(grabbable);
        }

        public void ClearTargetGrabbables()
        {
            _targetGrabbables.Clear();
        }

        public int GetGrabbedObjectCount()
        {
            return _currentlyGrabbedObjects.Count;
        }

        public List<Grabbable> GetCurrentlyGrabbedObjects()
        {
            return new List<Grabbable>(_currentlyGrabbedObjects);
        }
    }
}