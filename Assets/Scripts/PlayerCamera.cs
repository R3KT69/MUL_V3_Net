using UnityEngine;
using PurrNet;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Orbit Settings")]
    public float distance = 5f;
    public float mouseSensitivity = 2f;
    public float minYAngle = -30f;
    public float maxYAngle = 75f;

    [Header("Camera Offsets")]
    public Vector3 baseOffset = new Vector3(0f, 1.5f, 0f); // Camera position 
    public Vector3 orbitOffset = new Vector3(0f, 0f, 0f);  // Free-look offset
    public Vector3 aimingOffset = new Vector3(0.5f, 0f, 1f);   // Aiming offset
    private bool isOffsetFlipped = false;

    [Header("Transition Settings")]
    public float bodyRotationSpeed = 10f;
    public float aimingShoulderSnapSpeed = 50f;
    public float offsetTransitionSpeed = 5f;

    private float xAngle = 0f;
    private float yAngle = 15f;

    private Transform playerTransform;
    private Animator animator;
    private Camera controlledCamera;
    private AudioListener controlledAudioListener;
    private Vector3 currentShoulderOffset = Vector3.zero;
    private float aimBlend = 0f; 

    public float VerticalAimAngle => yAngle;
    public float HorizontalAimAngle => xAngle;

    void Start()
    {
        playerTransform = transform.parent;
        animator = GetComponentInParent<Animator>();
        controlledCamera = GetComponentInChildren<Camera>(true);
        controlledAudioListener = GetComponentInChildren<AudioListener>(true);

        if (playerTransform == null)
        {
            enabled = false;
            return;
        }

        if (isOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (!isOwner)
        {
            if (controlledCamera != null)
                controlledCamera.enabled = false;
            if (controlledAudioListener != null)
                controlledAudioListener.enabled = false;
            return;
        }

        if (controlledCamera != null)
            controlledCamera.enabled = true;
        if (controlledAudioListener != null)
            controlledAudioListener.enabled = true;
        
        // Mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xAngle += mouseX;
        yAngle -= mouseY;
        yAngle = Mathf.Clamp(yAngle, minYAngle, maxYAngle);

        // Shoulder Snapping
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            isOffsetFlipped = !isOffsetFlipped;
            aimingOffset = new Vector3(
                isOffsetFlipped ? -Mathf.Abs(aimingOffset.x) : Mathf.Abs(aimingOffset.x),
                aimingOffset.y,
                aimingOffset.z
            );
        }

        Quaternion camRotation = Quaternion.Euler(yAngle, xAngle, 0f);

        bool isAiming = animator.GetBool("isAiming");

        // Smooth blend factor for aim transition
        float targetBlend = isAiming ? 1f : 0f;
        aimBlend = Mathf.MoveTowards(aimBlend, targetBlend, Time.deltaTime * offsetTransitionSpeed);

        // Compute world-space offsets
        Vector3 orbitWorldOffset = orbitOffset;
        Vector3 aimingWorldOffset = camRotation * aimingOffset;
        Vector3 targetOffset = Vector3.Lerp(orbitWorldOffset, aimingWorldOffset, aimBlend);

        currentShoulderOffset = Vector3.Lerp(currentShoulderOffset, targetOffset, Time.deltaTime * aimingShoulderSnapSpeed);

        if (isAiming)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, xAngle, 0f);
            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, Time.deltaTime * bodyRotationSpeed);
        }

        Vector3 camPosition = playerTransform.position + baseOffset + currentShoulderOffset - camRotation * Vector3.forward * distance;

        transform.position = camPosition;
        transform.rotation = camRotation;
    }
}
