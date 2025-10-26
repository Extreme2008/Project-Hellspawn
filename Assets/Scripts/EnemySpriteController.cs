using UnityEngine;

public class EnemySpriteController : MonoBehaviour
{
    private Transform Target;
    public bool CanLookVertically;



    
    void Start()
    {
        Target = FindObjectOfType<PlayerMovement>().transform;
    }

    
    void Update()
    {
        if (CanLookVertically)
        {
            transform.LookAt(Target);
        }
        else
        {
            Vector3 ModifiedTarget = Target.position;
            ModifiedTarget.y = transform.position.y;
            transform.LookAt(ModifiedTarget);
        }
        
    }
}
