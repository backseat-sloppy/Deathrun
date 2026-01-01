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

    // This stores the individual stone data
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
        // Find all children with Renderers (actual visual stones)
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

    /// <summary>
    /// This matches the 'Activation Method Name' in your VRTrapActivator.
    /// It triggers every child stone to fall.
    /// </summary>
    public void ActivateTrap()
    {
        for (int i = 0; i < childStones.Count; i++)
        {
            // If the stone is already moving, stop it
            if (childStones[i].activeCoroutine != null) 
                StopCoroutine(childStones[i].activeCoroutine);

            // Start the fall for this specific stone
            StartCoroutine(MoveChild(i, childStones[i].fallY));
        }
    }

    /// <summary>
    /// Call this to make all stones rise back up.
    /// </summary>
    public void ActivateStoneRise()
    {
        for (int i = 0; i < childStones.Count; i++)
        {
            if (childStones[i].activeCoroutine != null) 
                StopCoroutine(childStones[i].activeCoroutine);

            StartCoroutine(MoveChild(i, childStones[i].startY));
        }
    }

    private IEnumerator MoveChild(int index, float endY)
    {
        float duration = Random.Range(durationRange.x, durationRange.y); 
        float elapsedTime = 0f;

        Transform stoneTrans = childStones[index].transform;
        Vector3 startPos = stoneTrans.position;
        Vector3 endPos = new Vector3(startPos.x, endY, startPos.z);

        while (elapsedTime < duration)
        {
            if (stoneTrans == null) yield break; // Safety check

            float t = elapsedTime / duration;
            float easedT = t * t * (3f - 2f * t); // SmoothStep

            stoneTrans.position = Vector3.Lerp(startPos, endPos, easedT); 

            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        stoneTrans.position = endPos;
    }
}