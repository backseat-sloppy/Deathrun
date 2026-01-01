using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteppingStone : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How far below the surface the stones fall")]
    public float submergedDepth = 2.0f; 

    [Tooltip("The range for movement duration (lower value is faster)")]
    public Vector2 durationRange = new Vector2(0.8f, 1.5f); 

    private struct StoneData
    {
        public Transform transform;
        public float startY;
        public float fallY;
        public Coroutine activeCoroutine;
    }

    private List<StoneData> childStones = new List<StoneData>();

    private void Awake()
    {
        // Find all immediate children
        foreach (Transform child in transform)
        {
            StoneData data = new StoneData();
            data.transform = child;
            data.startY = child.position.y;
            data.fallY = child.position.y - submergedDepth;
            data.activeCoroutine = null;
            childStones.Add(data);
        }
    }

    // --- ACTIVATION METHODS ---

    /// <summary>
    /// This is what ActivationButton.cs is looking for.
    /// It simply redirects to the main logic.
    /// </summary>
    public void ActivateStoneFall()
    {
        ActivateTrap();
    }

    /// <summary>
    /// This is what VRTrapActivator (the VR Grab script) is looking for.
    /// </summary>
    public void ActivateTrap()
    {
        for (int i = 0; i < childStones.Count; i++)
        {
            if (childStones[i].activeCoroutine != null) 
                StopCoroutine(childStones[i].activeCoroutine);

            // We store the reference to the coroutine so we can stop it if needed
            StartCoroutine(MoveChild(i, childStones[i].fallY));
        }
    }

    public void ActivateStoneRise()
    {
        for (int i = 0; i < childStones.Count; i++)
        {
            if (childStones[i].activeCoroutine != null) 
                StopCoroutine(childStones[i].activeCoroutine);

            StartCoroutine(MoveChild(i, childStones[i].startY));
        }
    }

    // --- INTERNAL MOVEMENT LOGIC ---

    private IEnumerator MoveChild(int index, float endY)
    {
        float duration = Random.Range(durationRange.x, durationRange.y); 
        float elapsedTime = 0f;

        Transform stoneTrans = childStones[index].transform;
        Vector3 startPos = stoneTrans.position;
        Vector3 endPos = new Vector3(startPos.x, endY, startPos.z);

        while (elapsedTime < duration)
        {
            if (stoneTrans == null) yield break;

            float t = elapsedTime / duration;
            float easedT = t * t * (3f - 2f * t); // SmoothStep easing

            stoneTrans.position = Vector3.Lerp(startPos, endPos, easedT); 

            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        stoneTrans.position = endPos;
    }
}