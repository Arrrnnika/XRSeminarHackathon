using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ShoreBot;

public class RobotModeController : MonoBehaviour
{
    public enum RobotMode{Drive, Tool}

    [SerializeField] private CrawlerController crawler;
    [SerializeField] private ToolarmController toolArm;
    [SerializeField] private InputActionProperty toggleModeInput; //A Button to switch: <XRController> {RightHand}/SecondaryButton

    public RobotMode CurrentMode { get; private set; } = RobotMode.Drive;
    //for Ui, haptics, sound,...
    public event Action<RobotMode> ModeChanged;

    private void OnEnable()
    {
        toggleModeInput.action?.Enable();
    }

    private void OnDisable()
    {
        if(toggleModeInput.reference == null)
            toggleModeInput.action?.Disable();
    }

    void Start()
    {
        SetMode(RobotMode.Drive);
    }

    private void Update()
    {
        if (toggleModeInput.action != null && toggleModeInput.action.WasPressedThisFrame())
            SetMode(CurrentMode== RobotMode.Drive ? RobotMode.Tool: RobotMode.Drive);
    }

    public void SetMode(RobotMode mode)
    {
        CurrentMode = mode;

        if (crawler != null)
            crawler.DriveMode = mode == RobotMode.Drive;
        if (toolArm != null)
            toolArm.ToolModeActive = mode == RobotMode.Tool;

        ControlBar.SetToolMode(mode == RobotMode.Tool); 
        InfoBar.Instance?.SetArm(mode == RobotMode.Tool); 

        ModeChanged?.Invoke(mode);
    }
}