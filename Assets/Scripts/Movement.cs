using UnityEngine;
using PurrNet;

public class Movement_legacy : NetworkBehaviour
{
    private CharacterController characterController;
    private Animator animator;
    private NetworkAnimator networkAnimator;
    private Transform cameraTransform;
    private Vector3 moveDirection;
    private float moveX, moveZ;
    private float currentSpeed = 0f;
    
    [Header("Movement Settings")]
    public float walkSpeed = 2.5f;
    public float sprintSpeed = 6f;
    public float jogSpeed = 4.5f;
    public float speedReduceFactor = 0.5f;
    public float jumpForce = 5f;
    public float rotationSpeed = 8f;
    bool isSprinting = false;

    [Header("Ground Check Settings")]
    public Transform feetTransform;
    public float sphereRadius = 0.3f;
    public LayerMask groundMask;
    private static Collider[] groundHits = new Collider[10];

    [Header("Physics Settings")]
    private Vector3 velocity;
    private float gravity = -9.81f;
    private bool isGrounded;

    

    void Start()
    {
        characterController = gameObject.GetComponent<CharacterController>();
        cameraTransform = gameObject.GetComponentInChildren<Camera>().transform;
        animator = gameObject.GetComponent<Animator>();
        networkAnimator = gameObject.GetComponent<NetworkAnimator>();
    }

    void Update()
    {
        if (!isOwner) return;

        HandleMovementInput();
        CheckGroundStatus();
        Jump();
        ApplyGravity();
        MovePlayer();
        UpdateAnimations();
        RotatePlayerTowardCamera();
    }

    void HandleMovementInput()
    {
        moveX = Input.GetAxis("Horizontal");
        moveZ = Input.GetAxis("Vertical");
        moveDirection = transform.right * moveX + transform.forward * moveZ;

        if (moveDirection.magnitude > 1) moveDirection.Normalize();

        float targetSpeed = jogSpeed;
        if (animator.GetBool("isAiming"))
        {
            targetSpeed = walkSpeed; 
            isSprinting = false;
        }
        else if (Input.GetKey(KeyCode.LeftShift) && moveZ > 0f && Mathf.Abs(moveX) < 0.01f)
        {
            targetSpeed = sprintSpeed;
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }

        //float targetSpeed = animator.GetBool("isAiming") /*|| Input.GetKey(KeyCode.LeftShift)*/ ? walkSpeed : sprintSpeed; // Toggle Walk/Sprint
        //float targetSpeed = sprintSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 8f);
    }

    void MovePlayer()
    {
        Vector3 moveVelocity = moveDirection * currentSpeed;
        moveVelocity.y = velocity.y;
        characterController.Move(moveVelocity * Time.deltaTime);
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            SetAnimatorTrigger("Jump");
        }
    }

    void ApplyGravity()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (velocity.y < 0)
        {
            velocity.y = -1f;
        }
    }

    void CheckGroundStatus()
    {
        isGrounded = GroundCheck(feetTransform, sphereRadius, groundMask);
        SetAnimatorBool("isGrounded", isGrounded);
    }

    void SetAnimatorTrigger(string parameterName)
    {
        if (networkAnimator != null)
            networkAnimator.SetTrigger(parameterName);
        else
            animator.SetTrigger(parameterName);
    }

    void SetAnimatorBool(string parameterName, bool value)
    {
        if (networkAnimator != null)
            networkAnimator.SetBool(parameterName, value);
        else
            animator.SetBool(parameterName, value);
    }

    bool GroundCheck(Transform feetTransform, float sphereRadius, LayerMask groundMask)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(feetTransform.position, sphereRadius, groundHits, groundMask);
        return hitCount > 0;
    }

    void UpdateAnimations()
    {
        float acceleration = moveZ;
        float horizontal = moveX;

        // Normalize if diagonally moving
        if (Mathf.Abs(moveX) > 0.01f && Mathf.Abs(moveZ) > 0.01f)
        {
            float diagonalMag = Mathf.Sqrt(moveX * moveX + moveZ * moveZ);
            acceleration = moveZ / diagonalMag;
            horizontal = moveX / diagonalMag;
        }
        
        if (animator.GetBool("isAiming"))
        {
            acceleration *= speedReduceFactor;
            horizontal *= speedReduceFactor;
        }

        if (isSprinting) acceleration = 1.5f;
        
        
        animator.SetFloat("Acceleration", acceleration, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", horizontal, 0.1f, Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        if (feetTransform == null) return;
        bool grounded = GroundCheck(feetTransform, sphereRadius, groundMask);
        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(feetTransform.position, sphereRadius);
    }

    void RotatePlayerTowardCamera()
    {
        Vector3 inputDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        if (inputDir.sqrMagnitude > 0.01f)
        {
            // Camera-relative direction
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(camForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
