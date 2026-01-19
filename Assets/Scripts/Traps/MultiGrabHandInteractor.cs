using System.Collections.Generic;
using UnityEngine;


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
        private UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor _handInteractor;

        [SerializeField]
        [Tooltip("Optional: List of specific XRGrabInteractable objects to track. Leave empty to grab any hovered XRGrabInteractable.")]
        private List<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> _targetGrabbables = new List<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

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
        private List<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> _currentlyGrabbedObjects = new List<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        private Dictionary<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, GrabData> _grabDataMap = new Dictionary<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, GrabData>();

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
                _handInteractor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
                if (_handInteractor == null)
                {
                    Debug.LogError($"MultiGrabHandInteractor on {gameObject.name} requires an XRDirectInteractor component!");
                    enabled = false;
                    return;
                }
            }
        }

        private void Update()
        {
            // Check if hand is currently in grab/select state
            _isGrabbing = _handInteractor.hasSelection;

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
            // Find all currently hovered XRGrabInteractable objects
            List<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> hoveredGrabbables = GetHoveredGrabbables();

            // Grab each hovered object
            foreach (UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbable in hoveredGrabbables)
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
            foreach (UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbable in _currentlyGrabbedObjects)
            {
                ReleaseObject(grabbable);
            }

            _currentlyGrabbedObjects.Clear();
            _grabDataMap.Clear();
        }

        private void UpdateGrabbedObjects()
        {
            // Update position/rotation of grabbed objects to follow hand
            Transform handTransform = _handInteractor.transform;
            Vector3 handPosition = handTransform.position;
            Quaternion handRotation = handTransform.rotation;

            foreach (UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbable in _currentlyGrabbedObjects)
            {
                if (grabbable == null || !_grabDataMap.ContainsKey(grabbable))
                    continue;

                GrabData data = _grabDataMap[grabbable];
                Transform objectTransform = grabbable.transform;

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
                    Vector3 targetPos = handPosition + handRotation * data.LocalPositionOffset;
                    Quaternion targetRot = handRotation * data.LocalRotationOffset;

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

        private void GrabObject(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbable)
        {
            if (_currentlyGrabbedObjects.Contains(grabbable))
                return;

            Transform handTransform = _handInteractor.transform;
            Transform objectTransform = grabbable.transform;

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

        private void ReleaseObject(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbable)
        {
            if (!_grabDataMap.ContainsKey(grabbable))
                return;

            GrabData data = _grabDataMap[grabbable];
            Transform objectTransform = grabbable.transform;

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

        private List<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> GetHoveredGrabbables()
        {
            List<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> hoveredGrabbables = new List<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            // If specific target grabbables are defined, check those
            if (_targetGrabbables != null && _targetGrabbables.Count > 0)
            {
                foreach (UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbable in _targetGrabbables)
                {
                    if (grabbable != null && IsGrabbableHovered(grabbable))
                    {
                        hoveredGrabbables.Add(grabbable);
                    }
                }
            }
            else
            {
                // Find all XRGrabInteractable objects in the scene that are being hovered
                UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable[] allGrabbables = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                foreach (UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbable in allGrabbables)
                {
                    if (IsGrabbableHovered(grabbable))
                    {
                        hoveredGrabbables.Add(grabbable);
                    }
                }
            }

            return hoveredGrabbables;
        }

        private bool IsGrabbableHovered(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbable)
        {
            // Check if the hand interactor is hovering over this grabbable
            return _handInteractor.interactablesHovered.Contains(grabbable);
        }

        public void AddTargetGrabbable(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbable)
        {
            if (!_targetGrabbables.Contains(grabbable))
            {
                _targetGrabbables.Add(grabbable);
            }
        }

        public void RemoveTargetGrabbable(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbable)
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

        public List<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> GetCurrentlyGrabbedObjects()
        {
            return new List<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(_currentlyGrabbedObjects);
        }
    }
}