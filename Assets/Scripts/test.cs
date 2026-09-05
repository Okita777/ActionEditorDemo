using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{    

    private Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //按下y键盘，激活animator的test（trigger)
        if (Input.GetKeyDown(KeyCode.Y))
        {
            animator.SetTrigger("test");
        }
   }
}
