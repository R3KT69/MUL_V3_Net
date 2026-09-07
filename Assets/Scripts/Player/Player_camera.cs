using PurrNet;
using UnityEngine;

public class Player_camera : NetworkBehaviour
{
    public Camera p_camera;
    public Transform target;
    public float distance = 6f;
    public float height = 2f;
    public float mouse_sensitivity = 3f;
    public float minimum_pitch = -30f;
    public float maximum_pitch = 70f;

    private float yaw;
    private float pitch = 15f;

    void Start()
    {
        if (target == null)
        {
            target = transform.root;
        }

        
        if (p_camera == null)
        {
            p_camera = GetComponentInChildren<Camera>(true);
        }

        yaw = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (!isOwner || p_camera == null || target == null){
            p_camera.gameObject.SetActive(false);
            return;
        } 
        

        yaw += Input.GetAxis("Mouse X") * mouse_sensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouse_sensitivity;
        pitch = Mathf.Clamp(pitch, minimum_pitch, maximum_pitch);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Vector3 target_position = target.position + Vector3.up * height;
        Quaternion orbit_rotation = Quaternion.Euler(pitch, yaw, 0f);

        p_camera.transform.position = target_position + orbit_rotation * Vector3.back * distance;
        p_camera.transform.LookAt(target_position);
    }
}
