using UnityEngine;

namespace ShoreBot
{
    /// <summary>
    /// Drives the five joints of the ShoreBot repair arm (from RobotArm.fbx or ShoreBot_Crawler.fbx).
    ///
    /// Put this component on the object that contains the arm (the imported model root, or ArmMount).
    /// Joints are found by name automatically. All joints rest at their imported rotation, so an angle of 0 = rest pose.
    ///
    /// Joint axes (local):  Turret = Z (yaw) · Shoulder, Elbow, Wrist = X (pitch) · ToolRoll = Z (roll along the tool).
    /// Sign check: in Play mode set Shoulder to -10. The arm should lift. If it drops instead, tick "Invert" on that joint.
    ///
    /// Your IK can either write angles through SetJointAngles(...) (limits are applied), or drive the transforms itself;
    /// in that case disable this component so the two don't fight.
    /// </summary>
    public class RobotArmRig : MonoBehaviour
    {
        public enum Tool { Scanner, EpoxyNozzle }

        [System.Serializable]
        public class Joint
        {
            public string name;
            public Transform transform;
            public Vector3 localAxis = Vector3.right;
            public float minAngle = -90f;
            public float maxAngle = 90f;
            public float angle;
            public bool invert;

            Quaternion restRotation = Quaternion.identity;
            bool hasRest;

            public Joint() { }

            public Joint(string name, Vector3 axis, float min, float max)
            {
                this.name = name; localAxis = axis; minAngle = min; maxAngle = max;
            }

            public void CaptureRest()
            {
                if (transform != null && !hasRest) { restRotation = transform.localRotation; hasRest = true; }
            }

            public void Apply()
            {
                if (transform == null) return;
                CaptureRest();
                angle = Mathf.Clamp(angle, minAngle, maxAngle);
                float a = invert ? -angle : angle;
                transform.localRotation = restRotation * Quaternion.AngleAxis(a, localAxis);
            }
        }

        // Default limits were measured in Blender: the range each joint can move from the rest pose
        // before the arm hits the robot (touching the blade surface is allowed).
        public Joint turret   = new Joint("Turret",   Vector3.forward, -130f, 130f);
        public Joint shoulder = new Joint("Shoulder", Vector3.right,   -15f,  90f);
        public Joint elbow    = new Joint("Elbow",    Vector3.right,  -140f,  95f);
        public Joint wrist    = new Joint("Wrist",    Vector3.right,  -120f,  35f);
        public Joint toolRoll = new Joint("ToolRoll", Vector3.forward, -180f, 180f);

        [Header("Tools")]
        public Tool activeTool = Tool.Scanner;
        public GameObject scanner;
        public GameObject epoxyNozzle;
        [Tooltip("Contact point at the end of both tools.")]
        public Transform toolTip;

        Tool appliedTool;
        bool toolApplied;

        Joint[] Joints => new[] { turret, shoulder, elbow, wrist, toolRoll };

        void Reset() { FindParts(); }

        void Awake()
        {
            FindParts();
            foreach (var j in Joints) j.CaptureRest();
            ApplyTool();
        }

        void LateUpdate()
        {
            foreach (var j in Joints) j.Apply();
            if (!toolApplied || appliedTool != activeTool) ApplyTool();
        }

        /// <summary>Set all five joint angles in degrees (0 = rest pose). Limits are applied.</summary>
        public void SetJointAngles(float turretYaw, float shoulderPitch, float elbowPitch, float wristPitch, float roll)
        {
            turret.angle = turretYaw; shoulder.angle = shoulderPitch; elbow.angle = elbowPitch;
            wrist.angle = wristPitch; toolRoll.angle = roll;
        }

        public void SetTool(Tool tool) { activeTool = tool; ApplyTool(); }

        void ApplyTool()
        {
            if (scanner != null) scanner.SetActive(activeTool == Tool.Scanner);
            if (epoxyNozzle != null) epoxyNozzle.SetActive(activeTool == Tool.EpoxyNozzle);
            appliedTool = activeTool; toolApplied = true;
        }

        [ContextMenu("Find Parts By Name")]
        public void FindParts()
        {
            foreach (var j in Joints)
                if (j.transform == null) j.transform = FindDeep(j.name);
            if (scanner == null) { var t = FindDeep("Tool_Scanner"); if (t != null) scanner = t.gameObject; }
            if (epoxyNozzle == null) { var t = FindDeep("Tool_EpoxyNozzle"); if (t != null) epoxyNozzle = t.gameObject; }
            if (toolTip == null) toolTip = FindDeep("ToolTip");
        }

        Transform FindDeep(string objectName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == objectName) return t;
            return null;
        }
    }
}
