using UnityEngine;
using UnityEngine.InputSystem;

public class RumbleTest : MonoBehaviour
{
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 1f)
        {
            timer = 0f;

            if (Gamepad.current != null)
            {
                RumbleManager.instance.RumblePulse(0.4f, 1f, 0.2f);
                Debug.Log("PULSE");
            }
            else
            {
                Debug.Log("NO GAMEPAD FOUND");
            }
        }
    }
}
