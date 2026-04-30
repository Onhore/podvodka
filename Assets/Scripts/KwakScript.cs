using System.Reflection;
using UnityEngine;

public class KwakScript : MonoBehaviour
{
    public Animator animator; 
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void PlaySomething()
    {
        int rand = Random.Range(1, 3);
        animator.Play(rand.ToString());
    }
}
