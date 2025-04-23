using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    public Transform target; 
    public float rotationSpeed = 100f; 
    public Vector3 offset = new Vector3(0, 2, -5); 

    private float _yaw = 0f; 
    private float _pitch = 0f; 

    void Start()
    {
       
        _yaw = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;

        if (target)
        {
            transform.position = target.position + offset;
        }

        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;               

    }


    public void SetCameraRotation(Vector3 rotation)
    {
        _yaw = rotation.y;
        _pitch = rotation.x;

        Quaternion cameraRotation = Quaternion.Euler(_pitch, _yaw, 0);
        transform.position = target.position + cameraRotation * offset;
        transform.LookAt(target);
    }

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        
        if (Input.GetMouseButtonDown(1)) 
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _yaw += mouseX * rotationSpeed * Time.deltaTime;
        _pitch -= mouseY * rotationSpeed * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, -35f, 60f);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
        transform.position = target.position + rotation * offset;
        transform.LookAt(target);
    }

}
