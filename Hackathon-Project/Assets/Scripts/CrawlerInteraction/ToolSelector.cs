using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ShoreBot;

public class ToolSelector : MonoBehaviour
{
    public enum ToolType {Scan, Epoxy}

    [SerializeField] private ToolarmController toolArm;
    [SerializeField] private InputActionProperty switchToolInput; // X button, so primary button on left controller

    [SerializeField] private GameObject scannerModel;     //Tool_Scanner
    [SerializeField] private GameObject epoxyNozzleModel; //Tool_EpoxyNozzle
    

    public ToolType CurrentTool { get; private set; } = ToolType.Scan;
    public event Action<ToolType> ToolChanged;

    private void OnEnable()
    {
        switchToolInput.action?.Enable();
    }

    private void OnDisable()
    {
        if(switchToolInput.reference == null)
            switchToolInput.action?.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetTool(ToolType.Scan);
    }

    // Update is called once per frame
    void Update()
    {
        if (toolArm != null && toolArm.ToolModeActive)
        {
            if (switchToolInput.action != null && switchToolInput.action.WasPressedThisFrame())
            {
                SetTool(CurrentTool == ToolType.Scan ? ToolType.Epoxy : ToolType.Scan);
            }
        }
    }

    public void SetTool(ToolType tool)
    {
        CurrentTool = tool;

        if (scannerModel != null) scannerModel.SetActive(tool == ToolType.Scan);
        if (epoxyNozzleModel != null) epoxyNozzleModel.SetActive(tool == ToolType.Epoxy);

        InfoBar.Instance?.SetTool(tool == ToolType.Scan ? "SCANNER" : "EPOXY FILLER");

        ToolChanged?.Invoke(tool);
    }
}