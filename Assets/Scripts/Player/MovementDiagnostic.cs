using UnityEngine;
using Unity.Netcode;

namespace DeathrunGame
{
    /// <summary>
    /// Diagnostic script to identify which movement scripts are active and potentially conflicting
    /// Add this to your player to see what's controlling movement
    /// </summary>
    public class MovementDiagnostic : MonoBehaviour
    {
        [Header("Diagnostic Results")]
        [SerializeField] private bool hasCharacterController;
        [SerializeField] private bool hasRigidbody;
        [SerializeField] private bool hasPlayerNetworkController;
        [SerializeField] private bool hasPlayerMovementController;
        [SerializeField] private bool hasPlayerController;
        [SerializeField] private bool hasPlayerMovement;
        [SerializeField] private bool hasInputPayloadNetwork;
        
        [Header("Component States")]
        [SerializeField] private bool playerNetworkControllerEnabled;
        [SerializeField] private bool playerMovementControllerEnabled;
        [SerializeField] private bool playerControllerEnabled;
        [SerializeField] private bool playerMovementEnabled;
        
        [Header("Current Velocity")]
        [SerializeField] private Vector3 characterControllerVelocity;
        [SerializeField] private Vector3 rigidbodyVelocity;
        
        private CharacterController cc;
        private Rigidbody rb;
        
        void Start()
        {
            cc = GetComponent<CharacterController>();
            rb = GetComponent<Rigidbody>();
            
            AnalyzeComponents();
        }
        
        void Update()
        {
            // Update velocity info
            if (cc != null)
                characterControllerVelocity = cc.velocity;
            
            if (rb != null)
                rigidbodyVelocity = rb.linearVelocity;
                
            // Check enabled states
            var pnc = GetComponent<PlayerNetworkController>();
            if (pnc != null) playerNetworkControllerEnabled = pnc.enabled;
            
            var pmc = GetComponent<PlayerMovementController>();
            if (pmc != null) playerMovementControllerEnabled = pmc.enabled;
            
            var pc = GetComponent<PlayerController>();
            if (pc != null) playerControllerEnabled = pc.enabled;
            
            var pm = GetComponent<PlayerMovement>();
            if (pm != null) playerMovementEnabled = pm.enabled;
        }
        
        void AnalyzeComponents()
        {
            hasCharacterController = GetComponent<CharacterController>() != null;
            hasRigidbody = GetComponent<Rigidbody>() != null;
            hasPlayerNetworkController = GetComponent<PlayerNetworkController>() != null;
            hasPlayerMovementController = GetComponent<PlayerMovementController>() != null;
            hasPlayerController = GetComponent<PlayerController>() != null;
            hasPlayerMovement = GetComponent<PlayerMovement>() != null;
            hasInputPayloadNetwork = GetComponent<InputPayloadNetwork>() != null;
            
            Debug.Log("=== MOVEMENT DIAGNOSTIC RESULTS ===");
            Debug.Log($"CharacterController: {hasCharacterController}");
            Debug.Log($"Rigidbody: {hasRigidbody}");
            Debug.Log($"PlayerNetworkController: {hasPlayerNetworkController} (enabled: {playerNetworkControllerEnabled})");
            Debug.Log($"PlayerMovementController: {hasPlayerMovementController} (enabled: {playerMovementControllerEnabled})");
            Debug.Log($"PlayerController: {hasPlayerController} (enabled: {playerControllerEnabled})");
            Debug.Log($"PlayerMovement: {hasPlayerMovement} (enabled: {playerMovementEnabled})");
            Debug.Log($"InputPayloadNetwork: {hasInputPayloadNetwork}");
            
            // Warn about conflicts
            int movementScriptCount = 0;
            if (hasPlayerNetworkController && playerNetworkControllerEnabled) movementScriptCount++;
            if (hasPlayerMovementController && playerMovementControllerEnabled) movementScriptCount++;
            if (hasPlayerController && playerControllerEnabled) movementScriptCount++;
            if (hasPlayerMovement && playerMovementEnabled) movementScriptCount++;
            
            if (movementScriptCount > 1)
            {
                Debug.LogError($"CONFLICT DETECTED: {movementScriptCount} movement scripts are enabled! This will cause movement issues.");
                Debug.LogError("SOLUTION: Disable all movement scripts except the one you want to use.");
            }
            
            if (hasCharacterController && hasRigidbody)
            {
                Debug.LogWarning("POTENTIAL CONFLICT: Both CharacterController and Rigidbody detected. Usually only one should be used.");
            }
        }
        
        [ContextMenu("Re-analyze Components")]
        void ReAnalyze()
        {
            AnalyzeComponents();
        }
        
        [ContextMenu("Disable All Movement Scripts Except PlayerNetworkController")]
        void FixForPlayerNetworkController()
        {
            var pmc = GetComponent<PlayerMovementController>();
            if (pmc != null) pmc.enabled = false;
            
            var pc = GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;
            
            var pm = GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = false;
            
            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; // Disable physics
            
            Debug.Log("Fixed: Disabled conflicting movement scripts for PlayerNetworkController");
        }
    }
}