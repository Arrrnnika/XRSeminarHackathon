using UnityEngine;

/// <summary>
/// IK for a 5-DoF repair arm like the one by Sadeghian &amp; Sareh:
///   Joint 1  Turret    – yaw around the mount's up axis
///   Joint 2  Shoulder  – pitch (upper arm)
///   Joint 3  Elbow     – pitch (forearm)
///   Joint 4  Wrist     – pitch, keeps the tool pointing straight at the surface
///   Joint 5  ToolRoll  – spins the tool around its own axis (optional, while touching the surface)
///
/// The arm reaches for 'target' (the ToolHead of the ToolArmController, which stays the "where should the
/// tool be" point and handles workspace limits, surface clearance and touching).
///
/// Works with any rest pose, as long as:
///  - every part's pivot sits on its joint axis and the parts are nested
///    Turret → Shoulder → Elbow → Wrist → ToolRoll → ToolTip,
///  - the arm is planar (all hinge axes parallel),
///  - under 'mount', the turret's yaw axis points along the mount's +Y
///    and the arm reaches towards the mount's +Z in its rest pose.
///
/// </summary>
public class RobotArmIK : MonoBehaviour
{
    [Header("Joints (nested in this order)")]
    [SerializeField] private Transform mount;     //empty on the robot: +Y = yaw axis, +Z = forward
    [SerializeField] private Transform turret;    //joint 1 (yaw)
    [SerializeField] private Transform shoulder;  //joint 2 (pitch)
    [SerializeField] private Transform elbow;     //joint 3 (pitch)
    [SerializeField] private Transform wrist;     //joint 4 (pitch)
    [SerializeField] private Transform toolRoll;  //joint 5 (roll), optional
    [SerializeField] private Transform toolTip;   //empty at the very end of the tool

    [Header("Target")]
    [SerializeField] private Transform target;           //ToolHead of the ToolArmController
    [SerializeField] private ToolarmController toolArm;  //optional: spins the tool while touching the surface

    [Header("Behaviour")]
    [Tooltip("Elbow bends away from the surface (recommended), otherwise towards it.")]
    [SerializeField] private bool elbowUp = true;
    [Tooltip("Degrees per second the tool spins while touching the surface.")]
    [SerializeField] private float toolSpinSpeed = 360f;

    private Quaternion _turretRest;  //turret rotation relative to the mount in the rest pose
    private float _upperLength;      //shoulder → elbow
    private float _foreLength;       //elbow → wrist
    private float _toolLength;       //wrist → tool tip
    private float _yaw;

    private void Start()
    {
        if (mount == null)
            mount = turret.parent;

        //Measured once from the model's rest pose
        _turretRest = Quaternion.Inverse(mount.rotation) * turret.rotation;
        _upperLength = Vector3.Distance(shoulder.position, elbow.position);
        _foreLength = Vector3.Distance(elbow.position, wrist.position);
        _toolLength = Vector3.Distance(wrist.position, toolTip.position);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 up = mount.up;

        //1. Turret: turn the arm's plane towards the target.
        //   Directly below the turret the direction is undefined → keep the last yaw.
        Vector3 local = mount.InverseTransformPoint(target.position) - mount.InverseTransformPoint(turret.position);
        if (new Vector2(local.x, local.z).sqrMagnitude > 0.0004f) //> 2 cm sideways
            _yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        turret.rotation = mount.rotation * Quaternion.AngleAxis(_yaw, Vector3.up) * _turretRest;

        //The arm's plane: 'forward' and 'up' span it, 'hinge' is the axis all pitch joints rotate around
        Vector3 forward = mount.TransformDirection(Quaternion.AngleAxis(_yaw, Vector3.up) * Vector3.forward);
        Vector3 hinge = Vector3.Cross(up, forward);

        //2. The tool points straight down, so the wrist has to sit 'toolLength' above the target
        Vector3 wristTarget = target.position + up * _toolLength;

        //3. Shoulder + elbow: two-bone IK in the plane (law of cosines)
        Vector3 toWrist = wristTarget - shoulder.position;
        float x = Vector3.Dot(toWrist, forward);
        float y = Vector3.Dot(toWrist, up);

        float minReach = Mathf.Abs(_upperLength - _foreLength) + 0.001f;
        float maxReach = _upperLength + _foreLength - 0.001f;
        float distance = Mathf.Clamp(new Vector2(x, y).magnitude, minReach, maxReach); //out of reach → stretch towards it

        float lineAngle = Mathf.Atan2(y, x);
        float shoulderOffset = Angle(_upperLength, distance, _foreLength) * Mathf.Deg2Rad;
        float upperAngle = elbowUp ? lineAngle + shoulderOffset : lineAngle - shoulderOffset;

        Vector3 upperDir = forward * Mathf.Cos(upperAngle) + up * Mathf.Sin(upperAngle);
        Vector3 reachedWrist = shoulder.position + (forward * Mathf.Cos(lineAngle) + up * Mathf.Sin(lineAngle)) * distance;
        Vector3 elbowPosition = shoulder.position + upperDir * _upperLength;

        AimJoint(shoulder, elbow, upperDir, hinge);
        AimJoint(elbow, wrist, reachedWrist - elbowPosition, hinge);

        //4. Wrist: tool points straight at the surface
        AimJoint(wrist, toolTip, -up, hinge);

        //5. Tool roll: spin while working on the surface (cleaning / epoxy look)
        if (toolRoll != null && toolArm != null && toolArm.ToolModeActive && toolArm.IsTouchingSurface)
        {
            Vector3 toolAxis = (toolTip.position - toolRoll.position).normalized;
            toolRoll.Rotate(toolAxis, toolSpinSpeed * Time.deltaTime, Space.World);
        }
    }

    /// <summary>Rotates 'joint' around 'axis' so that the direction joint → child matches 'desiredDirection'.</summary>
    private static void AimJoint(Transform joint, Transform child, Vector3 desiredDirection, Vector3 axis)
    {
        Vector3 current = Vector3.ProjectOnPlane(child.position - joint.position, axis);
        Vector3 desired = Vector3.ProjectOnPlane(desiredDirection, axis);
        float angle = Vector3.SignedAngle(current, desired, axis);
        joint.rotation = Quaternion.AngleAxis(angle, axis) * joint.rotation;
    }

    /// <summary>Angle (degrees) opposite side c in a triangle with sides a, b, c.</summary>
    private static float Angle(float a, float b, float c)
    {
        float cos = (a * a + b * b - c * c) / (2f * a * b);
        return Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;
    }

    /// <summary>Orange sphere: where the tool tip can reach (tool pointing down). Keep the yellow workspace box inside it.</summary>
    private void OnDrawGizmosSelected()
    {
        if (shoulder == null || elbow == null || wrist == null || toolTip == null)
            return;

        Transform frame = mount != null ? mount : turret != null ? turret.parent : transform;
        float reach = Vector3.Distance(shoulder.position, elbow.position) + Vector3.Distance(elbow.position, wrist.position);
        float toolLength = Vector3.Distance(wrist.position, toolTip.position);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(shoulder.position - frame.up * toolLength, reach);
    }
}