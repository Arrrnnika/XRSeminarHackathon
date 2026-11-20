using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class ToolarmController : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private Transform toolHead;

    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private InputActionProperty leftMoveInput; 
    [SerializeField] private InputActionProperty rightMoveInput;

    [Header("Movement")] [SerializeField]
    private float motionScale = 1f; //1=proportional to hand, < 1 = more precise

    [SerializeField] private float followSpeed = 15f;

    //robot-local, in meters
    [Header("Workspace")] [SerializeField] private Vector3 workspaceMin = new Vector3(-0.25f, 0.01f, 0.15f);
    [SerializeField] private Vector3 workspaceMax = new Vector3(0.25f, 0.35f, 0.5f);

    [Header("Surface")] [SerializeField] private LayerMask surfaceMask;
    [SerializeField] private float minSurfaceClearance = 0.01f; //toolhead never gets close to surface than this

    [SerializeField]
    private float touchTolerance = 0.01f; // counts as touching within this extra distance for interactions

    [Header("Arm Visual")] //optional
    [SerializeField]
    private Transform armBase;

    [SerializeField] private Transform armVisual;

    public bool ToolModeActive { get; set; }
    public bool IsMoving { get; private set; }
    public bool IsTouchingSurface { get; private set; }
    public Transform ToolHead => toolHead;
    public Transform ActiveHand => _activeHand; //null when no hand is moving the arm (useful for haptics later on)

    private Transform _activeHand;
    private Vector3 _targetLocal; //where toolhead should be in robot-local space
    private Vector3 _grabHandLocal; //hand position in XR origin space, when grip was pressed
    private Vector3 _grabTargetLocal; //tool target when grip was pressed

    private void OnEnable()
    {
        leftMoveInput.action?.Enable();
        rightMoveInput.action?.Enable();
    }

    private void OnDisable()
    {
        if (leftMoveInput.reference == null)
            leftMoveInput.action?.Disable();
        if (rightMoveInput.reference == null)
            rightMoveInput.action?.Disable();
    }

    private void Start()
    {
        _targetLocal = Clamp(ToolHead.localPosition);
    }

    private void Update()
    {
        bool leftHeld = ToolModeActive && IsPressed(leftMoveInput);
        bool rightHeld = ToolModeActive && IsPressed(rightMoveInput);

        //If active hand lets go of grip button, no one controls the arm for now -> will either stay at none or switch to other hand (see below)
        if ((_activeHand == leftHand && !leftHeld) || (_activeHand == rightHand && !rightHeld))
            _activeHand = null;

        // nobody controls the arm -> hand that holds grip takes over (if at the same time: right wins)
        if (_activeHand == null)
        {
            if (rightHeld)
                StartMoving(rightHand);
            else if (leftHeld)
                StartMoving(leftHand);
        }

        IsMoving = _activeHand != null;

        if (IsMoving) //Calc target position in local robot space
        {
            // Take hand movement since grip button press -> transform into world dir -> transform to robot-foused direction
            Vector3 handLocal = xrOrigin.InverseTransformPoint(_activeHand.position);
            Vector3 wolrdDelta = xrOrigin.TransformVector(handLocal - _grabHandLocal) * motionScale;
            Vector3 robotDelta = transform.InverseTransformVector(wolrdDelta);
            _targetLocal = Clamp(_grabTargetLocal + robotDelta);
        }

        // Move toolhead to target position gradually, but stay above surface
        Vector3 goal = KeepAboveSurface(_targetLocal);
        float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        toolHead.localPosition = Vector3.Lerp(toolHead.localPosition, goal, t);
        toolHead.localRotation = Quaternion.identity;

        UpdateArmVisual();
    }



    //"Helper" functions

    private static bool IsPressed(InputActionProperty input)
    {
        return input.action != null && input.action.IsPressed();
    }



    //Grip just pressed --> Remember where hand and tool were, so only movement from here on counts
    private void StartMoving(Transform hand)
    {
        _activeHand = hand;
        _grabHandLocal = xrOrigin.InverseTransformPoint(hand.position);
        _grabTargetLocal = _targetLocal;
    }



    private Vector3 Clamp(Vector3 local)
    {
        return Vector3.Max(workspaceMin, Vector3.Min(workspaceMax, local));
    }



    private Vector3 KeepAboveSurface(Vector3 local)
    {
        Vector3 up = transform.up;
        Vector3 world = transform.TransformPoint(local);
        float castHeight = 0.5f;

        IsTouchingSurface = false;
        if (Physics.Raycast(world + up * castHeight, -up, out RaycastHit hit, castHeight + 1f, surfaceMask,
                QueryTriggerInteraction.Ignore))
        {
            float height = Vector3.Dot(world - hit.point, up);
            if (height < minSurfaceClearance)
            {
                world += up * (minSurfaceClearance - height);
                height = minSurfaceClearance;
            }

            IsTouchingSurface = height <= minSurfaceClearance + touchTolerance;
        }

        return transform.InverseTransformPoint(world);
    }



    //Placeholder. Stretches cylinder from arm base to tool head
    private void UpdateArmVisual()
    {
        if (armBase == null || armVisual == null)
            return;
        Vector3 start = armBase.position;
        Vector3 end = toolHead.position;
        Vector3 direction = end - start;
        if (direction.magnitude < 0.000001f)
            return;

        armVisual.position = (end + start) * 0.5f;
        armVisual.rotation = Quaternion.FromToRotation(Vector3.up, direction);
        Vector3 scale = armVisual.localScale;
        scale.y = direction.magnitude * 0.5f;
        armVisual.localScale = scale;
    }



    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube((workspaceMin + workspaceMax) * 0.5f, workspaceMax - workspaceMin);
    }
}