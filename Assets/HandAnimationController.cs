using UnityEngine;
using UnityEngine.InputSystem;

public class HandAnimationController : MonoBehaviour
{
   public Animator handAnimator;

    public InputActionProperty grabAction;


    // Update is called once per frame
    void Update()
    {
        float grabValue = grabAction.action.ReadValue<float>();
        handAnimator.SetFloat("Grap", grabValue);
    }
}
