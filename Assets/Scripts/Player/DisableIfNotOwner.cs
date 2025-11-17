using UnityEngine;
using Unity.Netcode;

namespace DeathrunGame
{
    /// <summary>
    /// Disables the GameObject if this client doesn't own it.
    /// Useful for disabling cameras, audio listeners, or other components on non-owned players.
    /// </summary>
    public class DisableIfNotOwner : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // If this client doesn't own this network object, disable the GameObject
            if (!IsOwner)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
