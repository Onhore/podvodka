using UnityEngine;

public class HpScript : MonoBehaviour
{
    public Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void HpUpdate()
    {
        animator.SetInteger("hp", animator.GetInteger("hp")-1);

        if (animator.GetInteger("hp") < 1)
            animator.SetInteger("hp", 3);
        
    }
}
