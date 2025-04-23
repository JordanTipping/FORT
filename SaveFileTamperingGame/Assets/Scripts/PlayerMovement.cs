using UnityEngine;


//Very simple movement script. 

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;            
    public float rotationSpeed = 720f;      
    public Transform cameraTransform;     

    private Rigidbody rb;                 
    private Vector3 inputDirection;       

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; //freeze upright
    }

    void Update()
    {
        HandleInput();
        RotateCharacter();

        //old test, press 1 to teleport. 
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TeleportForward();
        }
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void HandleInput()
    {
        
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0; 
        cameraRight.y = 0;

        cameraForward.Normalize();
        cameraRight.Normalize();

        inputDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
    }

    void RotateCharacter()
    {
        if (inputDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void ApplyMovement()
    {
        Vector3 movement = inputDirection * moveSpeed;
        movement.y = rb.velocity.y; 
        rb.velocity = movement;  
    }


    //helper method to make sure we can test saving game (reapplying or updating position. )
    void TeleportForward()
    {
        //20 units ahead
        Vector3 teleportPosition = transform.position + transform.forward * 20f;

        //send to position 
        rb.position = teleportPosition;

        //no momentum 
        rb.velocity = Vector3.zero;

        Debug.Log($"Teleported to: {teleportPosition}");
    }
}
