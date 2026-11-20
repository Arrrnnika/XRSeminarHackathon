using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CrawlerController : MonoBehaviour
{
    [Header("Input")] 
    [SerializeField] private InputActionProperty driveinput;
    [SerializeField, Range(0f, 0.5f)] private float deadzone = 0.15f; //how much does the joystick have to be pushed to move the robot

    [Header("Movement")] 
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float turnSpeed = 60f; //in degrees per second
    [SerializeField] private float acceleration = 2f; //how fast it speeds up but also the value for the speed of slowing down 
    [SerializeField] private float deceleration = 12f;
    
    [Header("Surface")] 
    [SerializeField] private LayerMask surfaceMask = 10;

    [SerializeField] private float rideHeight = 0.01f; //distance from crawler (parent obj) pivot to surface
    [SerializeField] private float probeHeight = 0.5f; //from what height the raycast/probe starts (used for bumps and concave curves)
    [SerializeField] private float probeDepth = 0.3f; // how far the the probe reaches beneath the current surface height (forconvex curves)
    [SerializeField] private float alignSpeed = 12f; //how fast the crawler aligsn/tilts to match the surface angle/slope

    public bool DriveMode { get; set; } = true;
    public bool IsOnSurface { get; private set; }
    public float CurrentSpeed => _currentSpeed;
    public Vector3 SurfaceNormal => _surfaceNormal;
    
    private float _currentSpeed;
    private Vector3 _surfaceNormal = Vector3.up;

    private void OnEnable()
    {
        driveinput.action?.Enable();
    }
    
    private void OnDisable()
    {
        if (driveinput.reference == null)
        {
            driveinput.action?.Disable();
        }
    }

    private void Start()
    {
        //At the start of the scene we snap to the surface immediately
        if(Probe(transform.position, out RaycastHit hit))
            ApplySurface(hit, true);
    }

    private void Update()
    {
        Vector2 input = Vector2.zero;
        if (DriveMode && driveinput.action != null)
        {
            input = driveinput.action.ReadValue<Vector2>();
        }
        else
        {
            input = Vector2.zero;
        }

        if (input.magnitude < deadzone)
            input = Vector2.zero;
        
        // rotate around surface normal (=crawler axis up is the same)
        float yaw = input.x * turnSpeed * Time.deltaTime;
        transform.rotation = Quaternion.AngleAxis(yaw, transform.up) * transform.rotation;
        
        // Accerelation
        float targetSpeed = input.y * maxSpeed;
        bool braking = Mathf.Abs(targetSpeed) < Mathf.Abs(_currentSpeed) || targetSpeed * _currentSpeed < 0f;
        float rate = braking ? deceleration : acceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);
        Vector3 step = transform.forward * (_currentSpeed * Time.deltaTime);
        
        //Check if surface where we want to move to exists (or not, or if it is a slope downwards)
        if (Probe(transform.position + step, out RaycastHit hit)) //so we have a surface where we want to move
        {
            ApplySurface(hit, false);
        }
        else //either there is no surface, or the surface is too low or too high for us to move onto it 
        {
            _currentSpeed = 0f; //no movement allowed (to not fall off)
            if(Probe(transform.position, out hit))
                ApplySurface(hit, false);
            else
                IsOnSurface = false;
        }
    }


    //Helper functions

    //Raycasts from above position along the crawlers down axis to find the surface 
    private bool Probe(Vector3 position, out RaycastHit hit)
    {
        Vector3 up = transform.up;
        Vector3 origin = position + up * probeHeight;
        float distance = probeHeight + rideHeight + probeDepth;
        return Physics.Raycast(origin, -up, out hit, distance, surfaceMask, QueryTriggerInteraction.Ignore);
    }

    private void ApplySurface(RaycastHit hit, bool instant)
    {
        Vector3 normal = hit.normal;

        Quaternion targetRot = Quaternion.FromToRotation(transform.up, normal) * transform.rotation;
        float velocity = instant? 1f : 1f - Mathf.Exp(-alignSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, velocity);
        transform.position = hit.point + normal * rideHeight;

        _surfaceNormal = normal;
        IsOnSurface = true;
    }

    
    //Debug Help
    
    private void OnDrawGizmosSelected()
    {
        Vector3 up = transform.up;
        Vector3 origin = transform.position + up * probeHeight;
        Gizmos.color = IsOnSurface ? Color.green : Color.red;
        Gizmos.DrawLine(origin, origin - up * (probeHeight + rideHeight + probeDepth));
    }
}
