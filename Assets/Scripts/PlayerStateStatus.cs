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

        // ✅ Separate bool for each taunt - simple and reliable
        public NetworkVariable<bool> IsTaunt1 = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        public NetworkVariable<bool> IsTaunt2 = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        public NetworkVariable<bool> IsTaunt3 = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        [Header("Action Timers")]
        [SerializeField] private float swingDuration = 1.5f;
        [SerializeField] private float tauntDuration = 3f;

        private float swingTimer;
        private float taunt1Timer;
        private float taunt2Timer;
        private float taunt3Timer;

        // Events for state changes
        public event System.Action OnLanded;
        public event System.Action OnLeftGround;
        public event System.Action OnStartedJumping;
        public event System.Action OnStartedFalling;
        public event System.Action OnStartedMoving;
        public event System.Action OnStoppedMoving;
        public event System.Action OnSwingStarted;
        public event System.Action OnSwingEnded;
        public event System.Action OnTaunt1Started;
        public event System.Action OnTaunt1Ended;
        public event System.Action OnTaunt2Started;
        public event System.Action OnTaunt2Ended;
        public event System.Action OnTaunt3Started;
        public event System.Action OnTaunt3Ended;
        public event System.Action OnDied;
        public event System.Action OnRespawned;

        public override void OnNetworkSpawn()
        {
            IsGrounded.OnValueChanged += OnGroundedChanged;
            IsJumping.OnValueChanged += OnJumpingChanged;
            IsFalling.OnValueChanged += OnFallingChanged;
            IsMoving.OnValueChanged += OnMovingChanged;
            IsSwinging.OnValueChanged += OnSwingingChanged;
            IsTaunt1.OnValueChanged += OnTaunt1Changed;
            IsTaunt2.OnValueChanged += OnTaunt2Changed;
            IsTaunt3.OnValueChanged += OnTaunt3Changed;
            IsDead.OnValueChanged += OnDeadChanged;
        }

        public override void OnNetworkDespawn()
        {
            IsGrounded.OnValueChanged -= OnGroundedChanged;
            IsJumping.OnValueChanged -= OnJumpingChanged;
            IsFalling.OnValueChanged -= OnFallingChanged;
            IsMoving.OnValueChanged -= OnMovingChanged;
            IsSwinging.OnValueChanged -= OnSwingingChanged;
            IsTaunt1.OnValueChanged -= OnTaunt1Changed;
            IsTaunt2.OnValueChanged -= OnTaunt2Changed;
            IsTaunt3.OnValueChanged -= OnTaunt3Changed;
            IsDead.OnValueChanged -= OnDeadChanged;
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Auto-reset swing after duration
            if (IsSwinging.Value && swingTimer > 0f)
            {
                swingTimer -= Time.deltaTime;
                if (swingTimer <= 0f)
                {
                    SetSwinging(false);
                    Debug.Log("⚾ Swing auto-ended after timer");
                }
            }

            // Auto-reset taunt 1
            if (IsTaunt1.Value && taunt1Timer > 0f)
            {
                taunt1Timer -= Time.deltaTime;
                if (taunt1Timer <= 0f)
                {
                    SetTaunt1(false);
                    Debug.Log("🎭 Taunt1 auto-ended after timer");
                }
            }

            // Auto-reset taunt 2
            if (IsTaunt2.Value && taunt2Timer > 0f)
            {
                taunt2Timer -= Time.deltaTime;
                if (taunt2Timer <= 0f)
                {
                    SetTaunt2(false);
                    Debug.Log("🎭 Taunt2 auto-ended after timer");
                }
            }

            // Auto-reset taunt 3
            if (IsTaunt3.Value && taunt3Timer > 0f)
            {
                taunt3Timer -= Time.deltaTime;
                if (taunt3Timer <= 0f)
                {
                    SetTaunt3(false);
                    Debug.Log("🎭 Taunt3 auto-ended after timer");
                }
            }
        }

        // Setter methods
        public void SetGrounded(bool grounded)
        {
            if (!IsOwner) return;
            if (IsDead.Value && grounded) return;
            IsGrounded.Value = grounded;
        }

        public void SetJumping(bool jumping)
        {
            if (!IsOwner) return;
            if (IsDead.Value) return;
            if (jumping && IsJumping.Value) return;
            IsJumping.Value = jumping;
        }

        public void SetFalling(bool falling)
        {
            if (!IsOwner) return;
            if (IsDead.Value) return;
            IsFalling.Value = falling;
        }

        public void SetDead(bool dead)
        {
            if (!IsOwner) return;
            IsDead.Value = dead;

            if (dead)
            {
                IsJumping.Value = false;
                IsFalling.Value = false;
                IsMoving.Value = false;
                IsSwinging.Value = false;
                IsTaunt1.Value = false;
                IsTaunt2.Value = false;
                IsTaunt3.Value = false;
                CurrentSpeed.Value = 0f;
                swingTimer = 0f;
                taunt1Timer = 0f;
                taunt2Timer = 0f;
                taunt3Timer = 0f;
            }
        }

        public void SetCurrentSpeed(float speed)
        {
            if (!IsOwner) return;
            CurrentSpeed.Value = Mathf.Max(0f, speed);
        }

        public void SetMoving(bool moving)
        {
            if (!IsOwner) return;
            if (IsDead.Value && moving) return;
            IsMoving.Value = moving;
        }

        public void SetSwinging(bool swinging)
        {
            if (!IsOwner) return;
            if (IsDead.Value) return;

            IsSwinging.Value = swinging;

            if (swinging)
            {
                swingTimer = swingDuration;
            }
        }

        // ✅ Three separate methods for three taunts - simple and bulletproof
        public void SetTaunt1(bool active)
        {
            if (!IsOwner) return;
            if (IsDead.Value) return;
            if (IsSwinging.Value && active) return;

            IsTaunt1.Value = active;

            if (active)
            {
                taunt1Timer = tauntDuration;
            }
        }

        public void SetTaunt2(bool active)
        {
            if (!IsOwner) return;
            if (IsDead.Value) return;
            if (IsSwinging.Value && active) return;

            IsTaunt2.Value = active;

            if (active)
            {
                taunt2Timer = tauntDuration;
            }
        }

        public void SetTaunt3(bool active)
        {
            if (!IsOwner) return;
            if (IsDead.Value) return;
            if (IsSwinging.Value && active) return;

            IsTaunt3.Value = active;

            if (active)
            {
                taunt3Timer = tauntDuration;
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

        private void OnTaunt1Changed(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnTaunt1Started?.Invoke();
                if (IsOwner) Debug.Log("🎭 Taunt1 started");
            }
            else if (!current && previous)
            {
                OnTaunt1Ended?.Invoke();
                if (IsOwner) Debug.Log("🎭 Taunt1 ended");
            }
        }

        private void OnTaunt2Changed(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnTaunt2Started?.Invoke();
                if (IsOwner) Debug.Log("🎭 Taunt2 started");
            }
            else if (!current && previous)
            {
                OnTaunt2Ended?.Invoke();
                if (IsOwner) Debug.Log("🎭 Taunt2 ended");
            }
        }

        private void OnTaunt3Changed(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnTaunt3Started?.Invoke();
                if (IsOwner) Debug.Log("🎭 Taunt3 started");
            }
            else if (!current && previous)
            {
                OnTaunt3Ended?.Invoke();
                if (IsOwner) Debug.Log("🎭 Taunt3 ended");
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