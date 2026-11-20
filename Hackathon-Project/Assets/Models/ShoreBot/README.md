# ShoreBot crawler + repair arm for Unity

## What's in here
| File | Use it for |
|---|---|
| `Models/ShoreBot_Crawler.fbx` | The whole robot, arm included. Every part is its own GameObject. |
| `Models/RobotArm.fbx` | The arm on its own, with `ArmMount` at the origin. |
| `Scripts/RobotArmRig.cs` | Optional helper that drives the 5 arm joints with limits and swaps the tools. |

Use **one** of the two models in a scene, not both, or you'll get two arms.

## Import
1. Drag the whole `ShoreBot` folder into your project's `Assets` folder.
2. Select each FBX and check the Import Settings (the defaults are fine. Only check these):
   - **Model:** Scale Factor 1, Convert Units ✓, **Bake Axis Conversion ☐ (leave off)**, Import Cameras ☐, Import Lights ☐
   - **Rig:** Animation Type = None
   - **Animation:** Import Animation ☐
   - **Materials:** Material Creation Mode = Import via MaterialDescription, Location = Use Embedded Materials
     (click "Extract Materials…" if you want to edit them; URP converts them to Lit materials)
3. Drag the model into the scene.

Units are meters. The root object shows Rotation X = -90. That is normal for Blender files and it is what keeps
the joint axes below intact; put the model under an empty GameObject if you want to move or rotate the whole robot.

## Hierarchy and joint axes
```
ArmMount                  fixed to the robot (child of Arm_ToolboxPlate > ArmMountPoint in the full robot)
 └─ Turret                yaw    – local Z
     └─ Shoulder          pitch  – local X
         └─ Elbow         pitch  – local X
             └─ Wrist     pitch  – local X
                 ├─ Camera_CloseUpStereo > CloseUpCameraPoint
                 └─ ToolRoll   roll – local Z (along the tool)
                     ├─ Tool_Scanner
                     ├─ Tool_EpoxyNozzle
                     └─ ToolTip   contact point at the end of both tools
```
Every joint has local rotation (0, 0, 0) in the rest pose, and its pivot sits on the servo shaft.
Lengths: turret → shoulder 0.09 m, shoulder → elbow 0.12 m, elbow → wrist 0.12 m, wrist → ToolTip 0.14 m.
In the rest pose the tool tip hovers 15 mm above the blade surface, about 0.43 m in front of the body.

## RobotArmRig (optional)
Add `RobotArmRig` to the model root. It finds the joints by name.
- Set angles in the Inspector or from code: `rig.SetJointAngles(yaw, shoulder, elbow, wrist, roll);` (degrees, 0 = rest)
- Swap tools: `rig.SetTool(RobotArmRig.Tool.EpoxyNozzle);`
- `rig.toolTip` is the end-effector Transform for your IK.
- **Sign check:** in Play mode set Shoulder to -10. The arm should lift. If it drops, tick *Invert* on that joint.
- Default limits (measured in Blender; how far each joint can move from rest before hitting the robot):
  turret ±130°, shoulder -15° (up) to +90° (down), elbow -140° to +95°, wrist -120° to +35°, roll ±180°.
  Lowering the shoulder or elbow from rest brings the tool onto the blade surface, which is intended.
- If your own IK script writes the joint rotations directly, disable this component so the two don't fight.
