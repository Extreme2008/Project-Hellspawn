using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float MoveSpeed;
    public float WalkSpeed;
    public float SprintSpeed;

    public float GroundDrag;
    public float JumpForce;
    public float JumpCooldown;
    public float AirMultiplier;
    bool ReadyToJump;

    [Header("Keybinds")]
    public KeyCode JumpKey = KeyCode.Space;
    public KeyCode SprintKey = KeyCode.LeftShift;
    public KeyCode CrouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]

    public float PlayerHeight;
    public LayerMask WhatIsGround;
    bool Grounded;

    public Transform Orientation;
    [Header("Crouching")]
    public float CrouchSpeed;
    public float CrouchYScale;
    public float StartYScale;

    [Header("Slope Handling")]
    public float MaxSlopeAngle;
    private RaycastHit SlopeHit;

    float horizontalInput;
    float verticalInput;

    Vector3 MoveDirection;

    Rigidbody rb;

    public MovementState State;
    
    public enum MovementState
    {
        Walking,
        Sprinting,
        Crouching,
        Air
    }


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        ReadyToJump = true;
        StartYScale = transform.localScale.y;
    }
    private void FixedUpdate()
    {
       

        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        // when to jump
        if (Input.GetKey(JumpKey) && ReadyToJump && Grounded)
        {
            ReadyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), JumpCooldown);
        }

        //Start Crouch
        if (Input.GetKeyDown(CrouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, CrouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        //end crouch
        if(Input.GetKeyUp(CrouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, StartYScale, transform.localScale.z);
        }

    }
    private void Update()
    {

        // ground check
        Grounded = Physics.Raycast(transform.position, Vector3.down, PlayerHeight * 0.5f + 0.3f, WhatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();

        // handle drag
        if (Grounded)
            rb.drag = GroundDrag;
        else
            rb.drag = 0;


    }
    private void StateHandler()
    {
        //mode - sprinting
        if (Grounded && Input.GetKey(SprintKey))
        {
            State = MovementState.Sprinting;
            MoveSpeed = SprintSpeed;
        }
        //mode - walking
        else if (Grounded)
        {
            State = MovementState.Walking;
            MoveSpeed = WalkSpeed;
        }
        //mode - jumping
        else
        {
            State = MovementState.Air;
        }
        //mode - crouching
        if (Input.GetKey(CrouchKey))
        {
            State = MovementState.Crouching;
            MoveSpeed = CrouchSpeed;
        }
    }

    private void MovePlayer()
    {
        MoveDirection = Orientation.forward * verticalInput + Orientation.right * horizontalInput;

        //on ground
        if(Grounded){

            rb.AddForce(MoveDirection.normalized * MoveSpeed * 10f, ForceMode.Force);
        }
        else if (!Grounded)
        {
            rb.AddForce(MoveDirection.normalized * MoveSpeed * 10f * AirMultiplier, ForceMode.Force);
        }

        // on slope
        if (OnSlope())
        {
            rb.AddForce(GetSlopeMoveDirection() * MoveSpeed * 20f, ForceMode.Force);
        }
    
        

    }
    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if (flatVel.magnitude > MoveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * MoveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        //reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * JumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        ReadyToJump = true;
    }
    
    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out SlopeHit, PlayerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, SlopeHit.normal);
            return angle < MaxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(MoveDirection, SlopeHit.normal).normalized;
    }

   
}
