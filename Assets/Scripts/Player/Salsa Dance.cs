using UnityEngine;

public class SalsaDance : MonoBehaviour
{
    Animator anim;
    readonly int danceHash = Animator.StringToHash("Dance");

    void Awake()
    {
        anim = GetComponent<Animator>(); 
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            bool isDancing = anim.GetBool(danceHash);
            anim.SetBool(danceHash, !isDancing);
        }else if (Input.GetKeyDown(KeyCode.N))
        {
            anim.SetBool(danceHash, false);
        }
    }
}
