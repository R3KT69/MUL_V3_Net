using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public float shakeDuration = 0f;
    public float dampingSpeed = 1.0f;

    [Header("Rotation Ranges (in degrees)")]
    public Vector2 pitchRange = new Vector2(-5f, -10f); // Up/down (X axis)
    public Vector2 yawRange = new Vector2(2f, 5f);       // Left/right (Y axis)

    private Quaternion initialLocalRot;
    private Quaternion targetShakeRot;

    void Start()
    {
        initialLocalRot = transform.localRotation;
    }

    public void TriggerShake(float duration)
    {
        shakeDuration = duration;

        // Generate new rotation shake
        float pitch = Random.Range(pitchRange.x, pitchRange.y); // negative = up
        float yaw = Random.Range(yawRange.x, yawRange.y);

        // Apply angles to a rotation
        targetShakeRot = Quaternion.Euler(pitch, yaw, 0f) * initialLocalRot;
    }

    void Update()
    {
        if (shakeDuration > 0)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetShakeRot, Time.deltaTime * dampingSpeed);
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, initialLocalRot, Time.deltaTime * dampingSpeed);
        }
    }
}
