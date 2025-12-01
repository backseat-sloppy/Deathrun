using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class RumbleManager : MonoBehaviour
{
    public static RumbleManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RumblePulse(float lowFrequency, float highFrequency, float duration)
    {
        Gamepad pad = Gamepad.current;
        if (pad == null) return;

        pad.SetMotorSpeeds(lowFrequency, highFrequency);
        StartCoroutine(StopRumble(duration, pad));
    }

    private IEnumerator StopRumble(float duration, Gamepad pad)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        pad.SetMotorSpeeds(0f, 0f);
    }
}
