# ShoreBot wind farm environment (Blade A v02), robot not included

Offshore wind farm with the broad work blade, sea, sky, six turbines and three cracks. Units are metres.

## Add it to a Unity project
1. Copy this whole **ShoreBot_BladeA_v02** folder into your project's **Assets** folder. Keep the folder name.
2. Wait for Unity to finish importing (spinner bottom-right stops).
3. Menu **Tools > ShoreBot > Blade A v02 > Create or refresh test scenes**.
   This makes the materials (URP or Built-in), the ready prefab and two test scenes in `Generated/`.
4. Drag **Generated/ShoreBot_RH_BladeA_v02.prefab** into your scene at position (0, 0, 0).
   Or open `Generated/ShoreBot_BladeA_CrawlTest.unity` to look at it without a robot.
5. Optional check: **Tools > ShoreBot > Blade A v02 > Validate width, collision and damage**.

If your scene has a floor plane at height 0 (the VR template's `Plane`), hide it: the sea sits below 0.
If the turbines look very dark on one side, raise the scene's ambient light (Window > Rendering > Lighting).

## Useful objects inside the prefab
- `COL_WorkBlade`: invisible collision copy of the blade (MeshCollider). Use it to keep a robot on the surface.
- `Anchor_CrawlerStart`: start point for the robot on the blade surface; `Route_Sample_00`...`_22` run along the crawl route.
- `Damage_01/02/03`: each has `_Visuals` (untick to "repair" that crack), `_Target` and `_Approach` markers.
- `ENV_WTG07_Context`: the turbine the work blade belongs to; `ENV_WTG08`...`12`: background turbines.

## Changes in this copy
- The five background turbines are turned 90 degrees around their towers so every rotor faces the same wind as the
  turbine you stand on (real wind farms are set up this way).
- Setup script fix: it now adds the blade collision correctly, so the prefab is created.
