using UnityEngine;

public class SetRocketOnScript : MonoBehaviour
{
    private Animator animator;
    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    public void Switch()
    {
        if (animator.GetBool("rocket"))
            animator.SetBool("rocket", false);
        else
            animator.SetBool("rocket", true);
    }
}
