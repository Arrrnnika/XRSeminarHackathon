# ShoreBot UI: controls tutorial, bottom control bar and wrist display

The user-experience layer only. No robot, no blade environment.

## What's inside
- **Controls tutorial**: one page per Quest button (10 pages). Each page waits until that button is pressed;
  A and B need a short press and a hold. Closes itself at the end. Menu = replay, hold Menu = skip, N = next page (computer).
- **Bottom control bar**: fixed to the bottom of the view. Left-controller buttons bottom-left, right-controller buttons
  bottom-right, with icons and PRESS / HOLD text. A key lights up green while pressed; the tutorial highlights the
  button it is teaching in yellow.
- **Wrist display** on the left controller: tool, arm status, progress bar, hint (placeholder text for now).
- **ControlsInfo** (created on first run): ONE place for every button's name, icon and action text.
  The tutorial and the control bar both read from it, so the controls team types the real actions there.

## Needs
Unity 6 with Input System, XR Interaction Toolkit (an `XR Origin (XR Rig)` in the scene with Main Camera,
Left Controller and Right Controller) and TextMeshPro (the menu offers to import TMP Essentials if missing).

## Add it to a project
1. Copy this whole **ShoreBot_UI** folder into the project's **Assets** folder. Keep the folder name.
2. Wait for Unity to finish importing.
3. Open the scene that has your XR Origin and choose **ShoreBot > Add Tutorial and Control Bar to Scene**.
4. Press Play. Click in the Game view and press **N** to step through the tutorial pages.

Run the menu again any time; it replaces its own UI objects and keeps your ControlsInfo texts.

## Edit the button texts
Select **Assets/ShoreBot_UI/ControlsInfo** and change *Press Action*, *Hold Action* and *Description* per button
in the Inspector. Untick *In Tutorial* to leave a button out of the tutorial.

## Scripts
`TutorialManager`, `ControlBar`, `ControlsInfo`, `InfoBar` (wrist display, call `InfoBar.Instance.SetTool(...)`,
`SetArm(...)`, `SetProgress(...)`, `SetHint(...)`), `SmoothFollow` (keeps the tutorial panel in front of you),
`Haptics`, `ShoreBotControls`. Editor: `ShoreBotUIInstaller` (the menu item).
