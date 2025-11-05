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

        // Events for state changes (other systems can subscribe)
        public event System.Action OnLanded;
        public event System.Action OnLeftGround;
        public event System.Action OnStartedJumping;
        public event System.Action OnStartedFalling;
        public event System.Action OnDied;
        public event System.Action OnRespawned;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                IsGrounded.OnValueChanged += OnGroundedChanged;
                IsJumping.OnValueChanged += OnJumpingChanged;
                IsFalling.OnValueChanged += OnFallingChanged;
                IsDead.OnValueChanged += OnDeadChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                IsGrounded.OnValueChanged -= OnGroundedChanged;
                IsJumping.OnValueChanged -= OnJumpingChanged;
                IsFalling.OnValueChanged -= OnFallingChanged;
                IsDead.OnValueChanged -= OnDeadChanged;
            }
        }

        // Setter methods (only owner can call these)
        public void SetGrounded(bool grounded)
        {
            if (!IsOwner) return;
            IsGrounded.Value = grounded;
        }

        public void SetJumping(bool jumping)
        {
            if (!IsOwner) return;
            IsJumping.Value = jumping;
        }

        public void SetFalling(bool falling)
        {
            if (!IsOwner) return;
            IsFalling.Value = falling;
        }

        public void SetDead(bool dead)
        {
            if (!IsOwner) return;
            IsDead.Value = dead;
        }

        public void SetCurrentSpeed(float speed)
        {
            if (!IsOwner) return;
            CurrentSpeed.Value = speed;
        }

        public void SetMoving(bool moving)
        {
            if (!IsOwner) return;
            IsMoving.Value = moving;
        }

        // Event callbacks
        private void OnGroundedChanged(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnLanded?.Invoke();
                Debug.Log("🟢 Landed on ground");
            }
            else if (!current && previous)
            {
                OnLeftGround?.Invoke();
                Debug.Log("🔵 Left ground");
            }
        }

        private void OnJumpingChanged(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnStartedJumping?.Invoke();
                Debug.Log("⬆️ Started jumping");
            }
        }

        private void OnFallingChanged(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnStartedFalling?.Invoke();
                Debug.Log("⬇️ Started falling");
            }
        }

        private void OnDeadChanged(bool previous, bool current)
        {
            if (current && !previous)
            {
                OnDied?.Invoke();
                Debug.Log("💀 Player died");
            }
            else if (!current && previous)
            {
                OnRespawned?.Invoke();
                Debug.Log("✨ Player respawned");
            }
        }
    }
}