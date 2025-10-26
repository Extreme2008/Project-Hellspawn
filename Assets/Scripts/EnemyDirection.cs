using UnityEngine;

public class EnemyDirection : MonoBehaviour

{
    [Header("References")]
    public Animator animator;
    public Transform viewer;

    [Header("Settings")]
    public float DeadZoneDegrees = 10f;

    private int CurrentDirection = -1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(viewer == null || animator == null)
            return;

        Vector3 ToViewer = viewer.position - transform.position;

        ToViewer.y = 0f;
        if (ToViewer.sqrMagnitude < 0.001f)
            return;

        Vector3 forward = transform.forward;
        forward.y = 0f;

        float angle = Vector3.SignedAngle(forward, ToViewer, Vector3.up);

        if (angle < 0)
            angle += 360f;

        int NewDirection = Mathf.FloorToInt((angle + 22.5f) / 45f) % 8;

        if (NewDirection != CurrentDirection)
        {

            animator.SetInteger("Direction", NewDirection);
            CurrentDirection = NewDirection;
        }
        
    }
}
