using UnityEngine;

public class TrapManager : MonoBehaviour
{
    //trapmanager to handle all traps in the game
    

    public void activateTrap(GameObject trap)
    {
        //activate trap logic
        Debug.Log($"Activating trap: {trap.name}");
        // Example: Enable trap components or play animations
    }

    public void deactivateTrap(GameObject trap)
    {
        //deactivate trap logic
        Debug.Log($"Deactivating trap: {trap.name}");
        // Example: Disable trap components or stop animations
    }
}
