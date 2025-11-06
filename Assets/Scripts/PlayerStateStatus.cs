using Unity.Netcode;
using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Centralized state storage for player.
    /// Other components read/write to this to coordinate behavior.
    /// Owner has authority over state changes.
    /// </summary>
    public class PlayerStateStatus : NetworkBehaviour
    {
        [Header("Ground State")]
        public NetworkVariable<bool> IsGrounded = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        [Header("Air State")]
        public NetworkVariable<bool> IsJumping = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        public NetworkVariable<bool> IsFalling = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        [Header("Movement State")]
        public NetworkVariable<bool> IsMoving = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        public NetworkVariable<float> CurrentSpeed = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        [Header("Life State")]
        public NetworkVariable<bool> IsDead = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        [Header("Action State")]
        public NetworkVariable<bool> IsSwinging = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        public NetworkVariable<bool> IsTaunting = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        [Header("Action Timers")]
        [SerializeField] private float swingDuration = 1.5f; // How long swing stays active
        [SerializeField] private float tauntDuration = 3f; // How long taunt stays active

        private float swingTimer;
        private float tauntTimer;

        // Events for state changes (other systems can subscribe)
        public event System.Action OnLanded;
        public event System.Action OnLeftGround;
        public event System.Action OnStartedJumping;
        public event System.Action OnStartedFalling;
        public event System.Action OnStartedMoving;
        public event System.Action OnStoppedMoving;
        public event System.Action OnSwingStarted;
        public event System.Action OnSwingEnded;
        public event System.Action OnTauntStarted;
        public event System.Action OnTauntEnded;
        public event System.Action OnDied;
        public event System.Action OnRespawned;

        public override void OnNetworkSpawn()
        {
            // ✅ FIXED: All clients subscribe to state changes for animations/sounds
            IsGrounded.OnValueChanged += OnGroundedChanged;
            IsJumping.OnValueChanged += OnJumpingChanged;
            IsFalling.OnValueChanged += OnFallingChanged;
            IsMoving.OnValueChanged += OnMovingChanged;
            IsSwinging.OnValueChanged += OnSwingingChanged;
            IsTaunting.OnValueChanged += OnTauntingChanged;
            IsDead.OnValueChanged += OnDeadChanged;
        }

        public override void OnNetworkDespawn()
        {
            // ✅ FIXED: Unsubscribe for all clients
            IsGrounded.OnValueChanged -= OnGroundedChanged;
            IsJumping.OnValueChanged -= OnJumpingChanged;
            IsFalling.OnValueChanged -= OnFallingChanged;
            IsMoving.OnValueChanged -= OnMovingChanged;
            IsSwinging.OnValueChanged -= OnSwingingChanged;
            IsTaunting.OnValueChanged -= OnTauntingChanged;
            IsDead.OnValueChanged -= OnDeadChanged;
        }

        private void Update()
        {
            // ✅ Only owner updates timers
            if (!IsOwner) return;

            // ✅ Auto-reset swing after duration
            if (IsSwinging.Value && swingTimer > 0f)
            {
                swingTimer -= Time.deltaTime;
                if (swingTimer <= 0f)
                {
                    SetSwinging(false);
                    Debug.Log("⚾ Swing auto-ended after timer");
                }
            }

            // ✅ Auto-reset taunt after duration
            if (IsTaunting.Value && tauntTimer > 0f)
            {
                tauntTimer -= Time.deltaTime;
                if (tauntTimer <= 0f)
                {
                    SetTaunting(false);
                    Debug.Log("🎭 Taunt auto-ended after timer");
                }
            }
        }

        // Setter methods (only owner can call these)
        public void SetGrounded(bool grounded)
        {
            if (!IsOwner) return;
            
            // State validation: Can't be grounded while dead
            if (IsDead.Value && grounded) return;
            
            IsGrounded.Value = grounded;
        }

        public void SetJumping(bool jumping)
        {
            if (!IsOwner) return;
            
            // State validation: Can't jump while dead, swinging, or taunting
            if (IsDead.Value) return;
            if (IsTaunting.Value) return;
            if (jumping && IsJumping.Value) return; // Prevent spam
            
            IsJumping.Value = jumping;
        }

        public void SetFalling(bool falling)
        {
            if (!IsOwner) return;
            
            // State validation: Can't fall while dead
            if (IsDead.Value) return;
            
            IsFalling.Value = falling;
        }

        public void SetDead(bool dead)
        {
            if (!IsOwner) return;
            IsDead.Value = dead;
            
            // Clear all states when dying
            if (dead)
            {
                IsJumping.Value = false;
                IsFalling.Value = false;
                IsMoving.Value = false;
                IsSwinging.Value = false;
                IsTaunting.Value = false;
                CurrentSpeed.Value = 0f;
                
                // ✅ Reset timers
                swingTimer = 0f;
                tauntTimer = 0f;
            }
        }

        public void SetCurrentSpeed(float speed)
        {
            if (!IsOwner) return;
            
            // Clamp to valid range
            CurrentSpeed.Value = Mathf.Max(0f, speed);
        }

        public void SetMoving(bool moving)
        {
            if (!IsOwner) return;
            
            // State validation: Can't move while dead
            if (IsDead.Value && moving) return;
            
            IsMoving.Value = moving;
        }

        public void SetSwinging(bool swinging)
        {
            if (!IsOwner) return;
            
            // State validation: Can't swing while dead or taunting
            if (IsDead.Value) return;
            if (IsTaunting.Value && swinging) return;
            
            IsSwinging.Value = swinging;
            
            // ✅ Start timer when swing begins
            if (swinging)
            {
                swingTimer = swingDuration;
            }
        }

        public void SetTaunting(bool taunting)
        {
            if (!IsOwner) return;
            
            // State validation: Can't taunt while dead or swinging
            if (IsDead.Value) return;
            if (IsSwinging.Value && taunting) return;
            
            IsTaunting.Value = taunting;
            
            // ✅ Start timer when taunt begins
            if (taunting)
            {
                tauntTimer = tauntDuration;
            }
        }

        // Event callbacks
        private void OnGroundedChanged(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnLanded?.Invoke();
                if (IsOwner) Debug.Log("🟢 Landed on ground");
            }
            else if (!current && previous)
            {
                OnLeftGround?.Invoke();
                if (IsOwner) Debug.Log("🔵 Left ground");
            }
        }

        private void OnJumpingChanged(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnStartedJumping?.Invoke();
                if (IsOwner) Debug.Log("⬆️ Started jumping");
            }
        }

        private void OnFallingChanged(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnStartedFalling?.Invoke();
                if (IsOwner) Debug.Log("⬇️ Started falling");
            }
        }

        private void OnMovingChanged(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnStartedMoving?.Invoke();
                if (IsOwner) Debug.Log("🏃 Started moving");
            }
            else if (!current && previous)
            {
                OnStoppedMoving?.Invoke();
                if (IsOwner) Debug.Log("🛑 Stopped moving");
            }
        }

        private void OnSwingingChanged(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnSwingStarted?.Invoke();
                if (IsOwner) Debug.Log("⚾ Started swinging");
            }
            else if (!current && previous)
            {
                OnSwingEnded?.Invoke();
                if (IsOwner) Debug.Log("⚾ Swing ended");
            }
        }

        private void OnTauntingChanged(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnTauntStarted?.Invoke();
                if (IsOwner) Debug.Log("🎭 Started taunting");
            }
            else if (!current && previous)
            {
                OnTauntEnded?.Invoke();
                if (IsOwner) Debug.Log("🎭 Taunt ended");
            }
        }

        private void OnDeadChanged(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnDied?.Invoke();
                if (IsOwner) Debug.Log("💀 Player died");
            }
            else if (!current && previous)
            {
                OnRespawned?.Invoke();
                if (IsOwner) Debug.Log("✨ Player respawned");
            }
        }
    }
}