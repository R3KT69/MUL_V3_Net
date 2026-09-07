using PurrNet;
using UnityEngine;

public class Movement : NetworkBehaviour
{
    private CharacterController controller;
    private Camera player_camera;
    public Transform character_model;
    public float movement_speed = 10;
    public float rotation_speed = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
        player_camera = GetComponentInChildren<Camera>(true);

        if (character_model == null)
        {
            character_model = transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isOwner) return;
        
        Vector3 input_vector = Vector3.zero;


        if (Input.GetKey(KeyCode.D))
        {
            input_vector += Vector3.right;
        }
        if (Input.GetKey(KeyCode.W))
        {
            input_vector += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            input_vector += Vector3.left;
        }
        if (Input.GetKey(KeyCode.S))
        {
            input_vector += Vector3.back;
        }

        if (input_vector.sqrMagnitude > 0f)
        {
            input_vector.Normalize();

            Vector3 forward = player_camera != null ? player_camera.transform.forward : Vector3.forward;
            Vector3 right = player_camera != null ? player_camera.transform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 movement_direction = right * input_vector.x + forward * input_vector.z;
            movement_direction.Normalize();

            Quaternion target_rotation = Quaternion.LookRotation(movement_direction, Vector3.up);
            character_model.rotation = Quaternion.Lerp(
                character_model.rotation,
                target_rotation,
                rotation_speed * Time.deltaTime);

            controller.Move(movement_direction * movement_speed * Time.deltaTime);
        }
    }
}
