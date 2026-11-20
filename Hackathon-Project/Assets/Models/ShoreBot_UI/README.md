# ShoreBot UI: controller panels and wrist display

The user-experience layer only. No robot, no blade environment, no tutorial.

## What's inside
- **Two control panels**, fixed low on the left and right of the view (they move with your head).
  Each shows a drawing of that controller with a line from every button to what it does.
  A pressed button gets a green ring; B's ring fills up while it is held. A NAVIGATION / TOOL MODE badge sits low
  in the middle and flips with A; the grip labels are dimmed outside Tool mode.
- **Wrist display** on the left controller (tool, arm status, progress bar, hint; placeholder text).
- **ControlsInfo** (created on first run): every control's name, text, binding and position on the drawing.

Controls (from the team):
Right: joystick (no function yet, drawn without a label) · A = switch Navigation / Tool mode · B = hold 2 s to recenter the view.
Left: joystick = move and turn the robot · X = switch tool (Scanner / Epoxy filler).
Both, in Tool mode: grip + real hand movement = move the tool arm.

## Needs
Unity 6 with Input System, XR Interaction Toolkit (an `XR Origin (XR Rig)` in the scene with Main Camera and
Left Controller) and TextMeshPro (the menu offers to import TMP Essentials if missing).

## Add it to a project
1. Copy this whole **ShoreBot_UI** folder into the project's **Assets** folder. Keep the folder name.
2. Wait for Unity to finish importing.
3. Open the scene with your XR Origin and choose **ShoreBot > Add Control Panels to Scene**.
4. Press Play.

## Connect the real robot mode
The badge flips when A is pressed. To make it always match the robot, call
`ShoreBot.ControlBar.SetToolMode(true or false)` from the real mode switch, and untick **Toggle Mode With A**
on the ShoreBot_ControlBar object.

## Adjust placement or size
In `Editor/ShoreBotUIInstaller.cs`: `PanelAngle` (degrees out to the sides), `PanelHeight`, `PanelDistance`
and `PanelScale` (bigger = larger text). Run the menu again afterwards.

## Wrist display from code
`InfoBar.Instance.SetTool("SCANNER")`, `SetArm(true/false)`, `SetProgress("Scanned", 0.6f)`, `ClearProgress()`, `SetHint("...")`.
