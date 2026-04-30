using UnityEngine;

public class SetLightOn : MonoBehaviour
{
    private Animator animator;
    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    public void SetOn()
    {
        animator.SetBool("on", true);
    }
    public void SetOff()
    {
        animator.SetBool("on", false);
    }
}
