using UnityEngine;

namespace ShoreBot
{
    // Keeps a panel in front of the user's head. It stays at the same distance as you (or the robot
    // you ride on) move, but only turns to face you again when you look away, then drifts there gently.
    public class SmoothFollow : MonoBehaviour
    {
        public Transform head;               // the VR camera; found automatically if empty
        public float distance = 1.0f;        // metres in front of the eyes
        public float heightOffset = 0.03f;   // up/down from eye height
        public float followSpeed = 2f;       // higher = snappier
        public float deadZoneDegrees = 25f;  // how far you can look away before it follows

        Vector3 direction = Vector3.forward; // horizontal direction from head to panel

        void Start()
        {
            if (head == null && Camera.main != null) head = Camera.main.transform;
            Recenter();
            transform.SetPositionAndRotation(TargetPosition(), TargetRotation());
        }

        void LateUpdate()
        {
            if (head == null) return;
            Vector3 forward = head.forward; forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f && Vector3.Angle(forward, direction) > deadZoneDegrees) Recenter();
            float t = Time.deltaTime * followSpeed;
            transform.position = Vector3.Lerp(transform.position, TargetPosition(), t * 2f);
            transform.rotation = Quaternion.Slerp(transform.rotation, TargetRotation(), t);
        }

        void Recenter()
        {
            if (head == null) return;
            Vector3 forward = head.forward; forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            direction = forward.normalized;
        }

        Vector3 TargetPosition() => head.position + direction * distance + Vector3.up * heightOffset;
        Quaternion TargetRotation() => Quaternion.LookRotation(direction, Vector3.up);
    }
}
