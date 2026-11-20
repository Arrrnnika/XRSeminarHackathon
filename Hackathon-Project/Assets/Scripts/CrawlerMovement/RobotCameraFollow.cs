using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class RobotCameraFollow : MonoBehaviour
{
   public enum FollowMode
   {
      Stabilized,
      Locked
   }

   [SerializeField] private Transform cameraMount;
   [SerializeField] private Transform head; //XR main camera
   [SerializeField] private FollowMode mode = FollowMode.Locked;
   [SerializeField] private float turnSmoothing = 8f;
   [SerializeField] private InputActionProperty recenterInput; //hold down B?
   [SerializeField] private float holdDuration = 3f;

   private bool _initialized;
   private float _yawOffset; //how far the head was turned to the left/right inside the XR origin at the last recenter
   private float _heldtime;
   
   
   private void OnEnable()
   {
      recenterInput.action?.Enable();
   }

   private void OnDisable()
   {
      if (recenterInput.reference == null)
      {
         recenterInput.action?.Disable();
      }
   }

   private void Start()
   {
      //Recenter instantly 
      Invoke(nameof(Recenter), 0.5f);
   }

   private void Update()
   {
      bool held = recenterInput.action != null && recenterInput.action.IsPressed();
      _heldtime = held ? _heldtime + Time.deltaTime : 0f;

      if (_heldtime >= holdDuration)
      {
         Recenter();
      }
   }

   public void Recenter()
   {
      if (head == null)
         return;
      
      Vector3 localForward = transform.InverseTransformDirection(head.forward);
      _yawOffset = Mathf.Atan2(localForward.x, localForward.z) * Mathf.Rad2Deg;
      _initialized = false; //snap turn, no smooth turn
   }

   private void LateUpdate()
   {
      if (cameraMount == null)
         return;
      transform.position = cameraMount.position;
      Quaternion target = mode == FollowMode.Locked
         ? cameraMount.rotation
         : GetStabilizedRotation(transform.rotation * Quaternion.Euler(0f, _yawOffset, 0f));
      
      target *= Quaternion.Euler(0f, -_yawOffset, 0f);
      
      //First frame, snap turn, after that: smooth behaviour
      float t = _initialized ? 1f - Mathf.Exp(-turnSmoothing * Time.deltaTime) : 1f;
      transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
      _initialized = true;
   }

   private Quaternion GetStabilizedRotation(Quaternion fallback)
   {
      Vector3 heading = Vector3.ProjectOnPlane(cameraMount.forward, Vector3.up) +
                        Vector3.ProjectOnPlane(-cameraMount.up, Vector3.up);
      if (heading.sqrMagnitude < 0.0001f)
      {
         return fallback;
      }
      return Quaternion.LookRotation(heading.normalized, Vector3.up);
   }
}
