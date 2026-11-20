using UnityEngine;

/// <summary>
/// IK for a 5-DoF repair arm (ShoreBot arm: Turret → Shoulder → Elbow → Wrist → ToolRoll → ToolTip).
///   Joint 1  Turret    – yaw
///   Joint 2  Shoulder  – pitch (upper arm)
///   Joint 3  Elbow     – pitch (forearm)
///   Joint 4  Wrist     – pitch, holds the tool at 'toolPitch' (tilted forward, so it works in front of the robot)
///   Joint 5  ToolRoll  – spins the tool while it touches the surface (optional)
///
/// The arm reaches for 'target' (the ToolHead of the ToolArmController).
///
/// Working area: tilting the tool forward ('toolPitch') moves the reachable area in front of the robot.
/// Right-click this component → "Fit Workspace In Front Of Robot" sets the ToolArmController's yellow box
/// to the reachable area in front of the robot body, on the surface.
///
/// Everything is measured from the model's rest pose at Start, so the model's axes don't matter, as long as:
///  - every part's pivot sits on its joint axis and the parts are nested as above,
///  - the arm is planar (all pitch hinges parallel),
///  - 'turretYawAxis' is the turret's local rotation axis (ShoreBot model: Z).
///
/// Put this on the Crawler (its up = robot up, its forward = robot front). Don't use it together with RobotArmRig.
/// </summary>
public class RobotArmIK : MonoBehaviour
{
    [Header("Joints")]
    [SerializeField] private Transform turret;    //joint 1 (yaw)
    [SerializeField] private Transform shoulder;  //joint 2 (pitch)
    [SerializeField] private Transform elbow;     //joint 3 (pitch)
    [SerializeField] private Transform wrist;     //joint 4 (pitch)
    [SerializeField] private Transform toolRoll;  //joint 5 (roll), optional
    [SerializeField] private Transform toolTip;   //empty at the very end of the tool
    [Tooltip("The turret's local rotation axis. ShoreBot model: Z (0,0,1).")]
    [SerializeField] private Vector3 turretYawAxis = Vector3.forward;

    [Header("Target")]
    [SerializeField] private Transform target;           //ToolHead of the ToolArmController
    [SerializeField] private ToolarmController toolArm;  //for touching/spinning and for fitting the workspace

    [Header("Behaviour")]
    [Tooltip("Tool angle: 0 = straight down, higher = tilted forward. More = working area further in front of the robot.")]
    [SerializeField, Range(0f, 80f)] private float toolPitch = 45f;
    [Tooltip("Elbow bends away from the surface (recommended), otherwise towards it.")]
    [SerializeField] private bool elbowUp = true;
    [Tooltip("How far the turret may turn away from its rest direction, in degrees (ShoreBot: 130).")]
    [SerializeField] private float maxYaw = 130f;
    [Tooltip("Degrees per second the tool spins while touching the surface.")]
    [SerializeField] private float toolSpinSpeed = 360f;

    [Header("Fit Workspace")]
    [Tooltip("How high above the surface the tool may lift, in meters.")]
    [SerializeField] private float workHeight = 0.08f;
    [Tooltip("Gap between the front of the robot body and the start of the working area, in meters.")]
    [SerializeField] private float frontMargin = 0.03f;

    private Transform _mount;              //the turret's parent: fixed to the robot
    private Quaternion _turretRest;        //turret rotation relative to the mount in the rest pose
    private Vector3 _upLocal;              //yaw axis (pointing away from the surface), in mount space
    private Vector3 _restForwardLocal;     //direction the arm reaches in the rest pose, in mount space
    private float _upperLength;            //shoulder → elbow
    private float _foreLength;             //elbow → wrist
    private float _toolLength;             //wrist → tool tip
    private float _yaw;
    private Vector3 _lastForward;

    private void Start()
    {
        _mount = turret.parent;
        _turretRest = Quaternion.Inverse(_mount.rotation) * turret.rotation;

        //Yaw axis, flipped if needed so it points away from the surface (same side as the robot's up)
        Vector3 up = turret.TransformDirection(turretYawAxis).normalized;
        if (Vector3.Dot(up, transform.up) < 0f)
            up = -up;
        _upLocal = _mount.InverseTransformDirection(up);

        //Which way the arm points in its rest pose (seen from above)
        Vector3 restForward = Vector3.ProjectOnPlane(toolTip.position - turret.position, up);
        if (restForward.sqrMagnitude < 0.0001f)
            restForward = Vector3.ProjectOnPlane(transform.forward, up);
        _restForwardLocal = _mount.InverseTransformDirection(restForward.normalized);
        _lastForward = transform.forward;

        _upperLength = Vector3.Distance(shoulder.position, elbow.position);
        _foreLength = Vector3.Distance(elbow.position, wrist.position);
        _toolLength = Vector3.Distance(wrist.position, toolTip.position);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 up = _mount.TransformDirection(_upLocal);
        Vector3 restForward = _mount.TransformDirection(_restForwardLocal);

        //1. Turret: turn the arm's plane towards the target.
        //   Directly below the turret the direction is undefined → keep the last yaw.
        Vector3 toTarget = Vector3.ProjectOnPlane(target.position - turret.position, up);
        if (toTarget.sqrMagnitude > 0.0004f) //> 2 cm sideways
            _yaw = Mathf.Clamp(Vector3.SignedAngle(restForward, toTarget, up), -maxYaw, maxYaw);

        Quaternion yawRotation = Quaternion.AngleAxis(_yaw, up);
        turret.rotation = yawRotation * (_mount.rotation * _turretRest);

        //The arm's plane: 'forward' and 'up' span it, 'hinge' is the axis all pitch joints rotate around
        Vector3 forward = yawRotation * restForward;
        Vector3 hinge = Vector3.Cross(up, forward);
        _lastForward = forward;

        //2. The tool is held at 'toolPitch', so the wrist sits one tool length behind/above the target
        Vector3 toolDir = ToolDirection(forward, up);
        Vector3 wristTarget = target.position - toolDir * _toolLength;

        //3. Shoulder + elbow: two-bone IK in the plane (law of cosines)
        Vector3 toWrist = wristTarget - shoulder.position;
        float x = Vector3.Dot(toWrist, forward);
        float y = Vector3.Dot(toWrist, up);

        float minReach = Mathf.Abs(_upperLength - _foreLength) + 0.001f;
        float maxReach = _upperLength + _foreLength - 0.001f;
        float distance = Mathf.Clamp(new Vector2(x, y).magnitude, minReach, maxReach);

        float lineAngle = Mathf.Atan2(y, x);
        float shoulderOffset = Angle(_upperLength, distance, _foreLength) * Mathf.Deg2Rad;
        float upperAngle = elbowUp ? lineAngle + shoulderOffset : lineAngle - shoulderOffset;

        Vector3 upperDir = forward * Mathf.Cos(upperAngle) + up * Mathf.Sin(upperAngle);
        Vector3 reachedWrist = shoulder.position + (forward * Mathf.Cos(lineAngle) + up * Mathf.Sin(lineAngle)) * distance;
        Vector3 elbowPosition = shoulder.position + upperDir * _upperLength;

        AimJoint(shoulder, elbow, upperDir, hinge);
        AimJoint(elbow, wrist, reachedWrist - elbowPosition, hinge);

        //4. Wrist: hold the tool at the set angle
        AimJoint(wrist, toolTip, toolDir, hinge);

        //5. Tool roll: spin while working on the surface (cleaning / epoxy look)
        if (toolRoll != null && toolArm != null && toolArm.ToolModeActive && toolArm.IsTouchingSurface)
        {
            Vector3 toolAxis = (toolTip.position - toolRoll.position).normalized;
            toolRoll.Rotate(toolAxis, toolSpinSpeed * Time.deltaTime, Space.World);
        }
    }

    /// <summary>
    /// Moves a world point to the nearest point the tool tip can actually reach.
    /// The ToolArmController uses this so the ToolHead never runs away from the arm.
    /// </summary>
    public Vector3 ClampToReach(Vector3 worldTarget)
    {
        if (_upperLength <= 0f) //Start hasn't run yet
            return worldTarget;

        Vector3 up = _mount.TransformDirection(_upLocal);
        Vector3 forward = Vector3.ProjectOnPlane(worldTarget - turret.position, up);
        forward = forward.sqrMagnitude > 0.0004f ? forward.normalized : _lastForward;

        Vector3 toolDir = ToolDirection(forward, up);
        Vector3 wristPoint = worldTarget - toolDir * _toolLength;
        Vector3 fromShoulder = wristPoint - shoulder.position;

        float maxReach = (_upperLength + _foreLength) * 0.98f;
        float minReach = Mathf.Abs(_upperLength - _foreLength) + 0.01f;
        float distance = fromShoulder.magnitude;

        if (distance > maxReach)
            wristPoint = shoulder.position + fromShoulder / distance * maxReach;
        else if (distance < minReach && distance > 0.0001f)
            wristPoint = shoulder.position + fromShoulder / distance * minReach;

        return wristPoint + toolDir * _toolLength;
    }

    /// <summary>
    /// Sets the ToolArmController's workspace box to the area the tool can reach on the surface,
    /// in front of the robot body. Run it outside Play mode (right-click the component).
    /// </summary>
    [ContextMenu("Fit Workspace In Front Of Robot")]
    private void FitWorkspace()
    {
        if (toolArm == null || turret == null || shoulder == null || elbow == null || wrist == null || toolTip == null)
        {
            Debug.LogWarning("RobotArmIK: assign all joints and the Tool Arm first.");
            return;
        }

        //Everything in the Crawler's local space (y = 0 is the surface, +z is the robot's front)
        float scale = transform.lossyScale.x;
        float reach = (Vector3.Distance(shoulder.position, elbow.position) + Vector3.Distance(elbow.position, wrist.position)) / scale;
        float toolLength = Vector3.Distance(wrist.position, toolTip.position) / scale;

        float pitch = toolPitch * Mathf.Deg2Rad;
        Vector3 toolDirLocal = new Vector3(0f, -Mathf.Cos(pitch), Mathf.Sin(pitch));
        Vector3 center = transform.InverseTransformPoint(shoulder.position) + toolDirLocal * toolLength;

        float bottom = 0.01f;
        float top = workHeight;
        float radius = Mathf.Min(SliceRadius(reach, center.y, bottom), SliceRadius(reach, center.y, top));

        float zMin = Mathf.Max(BodyFrontZ() + frontMargin, center.z - radius);
        float zMax = center.z + radius * 0.9f;
        if (radius <= 0f || zMin >= zMax - 0.02f)
        {
            Debug.LogWarning("RobotArmIK: the arm can't reach the surface in front of the robot. " +
                             "Increase Tool Pitch, or move/scale the ArmMount forward, then try again.");
            return;
        }

        float zMid = (zMin + zMax) * 0.5f;
        float halfWidth = Mathf.Sqrt(Mathf.Max(0f, radius * radius - (zMid - center.z) * (zMid - center.z))) * 0.9f;

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(toolArm, "Fit Workspace");
#endif
        toolArm.SetWorkspace(new Vector3(center.x - halfWidth, bottom, zMin), new Vector3(center.x + halfWidth, top, zMax));
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(toolArm);
#endif
        Debug.Log($"RobotArmIK: workspace set to z {zMin:F2}–{zMax:F2} m in front of the Crawler, {halfWidth * 2f:F2} m wide.");
    }

    /// <summary>The front edge of the robot body (everything except the arm and the ToolHead), in Crawler space.</summary>
    private float BodyFrontZ()
    {
        float front = transform.InverseTransformPoint(turret.position).z;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r.transform.IsChildOf(turret) || (target != null && r.transform.IsChildOf(target)))
                continue;

            Bounds b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                front = Mathf.Max(front, transform.InverseTransformPoint(corner).z);
            }
        }
        return front;
    }

    private static float SliceRadius(float sphereRadius, float centerHeight, float height)
    {
        float dy = height - centerHeight;
        return Mathf.Sqrt(Mathf.Max(0f, sphereRadius * sphereRadius - dy * dy));
    }

    private Vector3 ToolDirection(Vector3 forward, Vector3 up)
    {
        float pitch = toolPitch * Mathf.Deg2Rad;
        return (forward * Mathf.Sin(pitch) - up * Mathf.Cos(pitch)).normalized;
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

    /// <summary>Orange sphere: where the tool tip can reach with the current Tool Pitch.</summary>
    private void OnDrawGizmosSelected()
    {
        if (shoulder == null || elbow == null || wrist == null || toolTip == null)
            return;

        float reach = Vector3.Distance(shoulder.position, elbow.position) + Vector3.Distance(elbow.position, wrist.position);
        float toolLength = Vector3.Distance(wrist.position, toolTip.position);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(shoulder.position + ToolDirection(transform.forward, transform.up) * toolLength, reach);
    }
}