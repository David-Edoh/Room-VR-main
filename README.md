# Interior Decoration Simulator - VR

Interior Decoration Simulator - VR is an immersive room-design simulation built with Unity. Explore a furnished room, customize colors and surface finishes, and rearrange furniture using VR controllers or Unity's built-in XR Device Simulator.

## Features

- **Paint objects:** choose a color and apply it to paintable room objects. Objects can trigger reactions such as animation, sound, or other effects when painted.
- **Apply surface finishes:** use the in-world design toolbar to choose finishes such as Oak, Botanical, Ceramic, Linen, and Plaster, then apply a finish to an object.
- **Rearrange furniture:** switch to Move mode, point at a movable object, and drag it to a new floor position.
- **VR and desktop simulation:** the project includes an XR Origin, controller ray interactions, world-space UI, and the XR Interaction Toolkit Device Simulator for testing without a headset.
- **Guided onboarding and progress:** a tutorial introduces the controls, while a progress counter tracks painted objects and shows a completion panel.
- **Persistent design:** paint colors and room layout/finish choices are saved locally and restored on later runs.

## Requirements

- **Unity 6000.6.6f1** (see `ProjectSettings/ProjectVersion.txt`).
- Unity packages resolve through Package Manager using `Packages/manifest.json`.
- The project is configured with **URP**, **XR Interaction Toolkit**, **XR Management**, and **OpenXR**.
- The active build scene is `Assets/Scenes/Color Room.unity`. The project settings include an Android target and OpenXR loader configuration; headset/device compatibility still depends on your Unity XR setup and target hardware.

## Open the project

1. Open Unity Hub and choose **Add project from disk**.
2. Select the project folder containing this README and `ProjectSettings/`.
3. Open it with Unity **6000.6.6f1** and allow Package Manager to resolve dependencies and the editor to import assets.
4. In the Project window, open `Assets/Scenes/Color Room.unity` if it is not already open.
5. Press **Play**. The scene contains the XR Device Simulator, so you can test the room without connecting a headset.

The scene uses Unity's XR Device Simulator rather than the Meta XR Simulator. For headset testing, configure the appropriate OpenXR runtime and device support in Unity before building or running on hardware.

## Controls

### Design controls

Controls are performed with the simulated or physical controllers:

- **Point and press the right controller trigger** to paint the targeted object or apply the selected finish.
- Choose **COLOR WHEEL** to paint with the selected color.
- Choose **TEXTURE** and select a finish swatch to apply that surface finish.
- Choose **MOVE**, then hold the trigger while pointing at furniture to drag it; release to place it.
- Use the left controller's primary button, or the palette's pin control when available, to switch the palette between hand-following and pinned world modes.
- Point controller rays at world-space controls and press the controller UI press input to activate buttons. The mouse cursor is captured during Play mode; press **Escape** to show it and restore mouse UI input.

### Desktop / XR Device Simulator

- The Game view should have focus while testing.
- **W/A/S/D** or the **arrow keys** move the XR Origin relative to the view direction.
- Hold **Shift** while moving for faster travel.
- Move the mouse to look around when the XR Device Simulator is using its HMD input mode.
- Use the simulator's on-screen control legend for simulator-specific device selection and controller-pose shortcuts. In the default XRI simulator controls, **Tab** cycles XR input, **U** selects the HMD, and **T/Y** toggle controller manipulation.

> The XR Device Simulator UI and mappings come from the XR Interaction Toolkit sample included under `Assets/Samples/XR Interaction Toolkit/`. If your Unity version or sample setup changes, use the overlay's current control legend as the source of truth.

## How to play

1. Follow or skip the introductory tutorial.
2. Use the palette to choose a color or use the toolbar to select a surface finish.
3. Aim at an object with a controller ray and press the trigger to apply the current design.
4. Switch to Move mode to reposition supported furniture.
5. Continue decorating; painted-object progress is restored from the local save.

Some objects have reactions attached to paint events. The tutorial, room-completion panel, palette, and in-world toolbar are part of the main scene.

## Project structure

| Path | Purpose |
| --- | --- |
| `Assets/Scenes/Color Room.unity` | Main and currently configured build scene |
| `Assets/Scripts/Interaction/` | XR ray painting, simulator locomotion/pose support, palette anchoring, and play-mode cursor handling |
| `Assets/Scripts/Design/` | Runtime decoration toolbar, finish selection, and furniture layout editing |
| `Assets/Scripts/Coloring/` | Paintable objects and groups, material updates, and paint events |
| `Assets/Scripts/Data/` | Color state and JSON persistence |
| `Assets/Scripts/Progress/` | Painted-object progress tracking |
| `Assets/Scripts/UI/` | Tutorial, world-panel placement, progress display, and room-completion UI |
| `Assets/Scripts/Reactions/` | Object reactions driven by paint and scene events |
| `Assets/Scripts/Audio/` | Shared UI, paint, and completion sound handling |
| `Assets/Prefabs/Room/` | Furnishings and room objects used in the scene |
| `Assets/Prefabs/Effects/` | Paint feedback effects |
| `Assets/XR/` and `Assets/XRI/` | OpenXR and XR Interaction Toolkit project configuration |
| `Packages/manifest.json` | Unity package dependencies |

### Main runtime systems

- **`ObjectDetection`** uses the right controller ray to find paintable targets, show hover feedback, and apply color/texture or move actions.
- **`PaintableObject` / `PaintableGroup`** apply colors and textures while preserving shared material assets, expose paint events, and support hover outlines.
- **`InteriorDesignManager`** creates the world-space toolbar, manages the selected tool and finish, and persists object layout and finish selections.
- **`PaletteAnchor`** follows the left controller or pins the palette in front of the player.
- **`XRUIInputModule` + `TrackedDeviceGraphicRaycaster`** route controller rays to world-space UI.
- **`KeyboardXROriginLocomotion` and `SimulatedControllerPitchFollow`** make desktop simulator navigation and controller aiming work with the headset view.
- **`PlayModeCursorCapture`** hides and locks the desktop cursor during Play mode so UI interaction is controller-driven; Escape restores the cursor.

## Saves and reset

The application saves under Unity's `Application.persistentDataPath` on the current device:

- **Paint colors** are stored as `ColorsRoom_<roomID>` using the color persistence service.
- **Textures and furniture layout** are stored in `RoomDesign.json`.
- **Tutorial completion** is stored using Unity PlayerPrefs.

Because these files live in the platform-specific persistent-data folder, removing/reinstalling the application or clearing its data can remove the saved design. The in-game room reset clears saved colors, finish choices, and furniture layout before reloading the scene. Tutorial completion is stored separately in PlayerPrefs and is not cleared by that reset.

## Build and run

1. Open **File → Build Profiles** (or **Build Settings**, depending on Unity UI version).
2. Confirm `Assets/Scenes/Color Room.unity` is enabled in the build scene list.
3. Choose the target platform, configure its XR Plug-in Management settings, and verify the OpenXR features for the target headset.
4. Build and run on a supported device.

Android is configured as a target in the project, but a successful device build requires the Android Build Support modules, SDK/NDK/JDK, and target-specific OpenXR setup to be installed in Unity Hub.

## Troubleshooting

- **The Game view is not responding to keyboard controls:** click the Game view to focus it. In Play mode the cursor is intentionally captured; press Escape to release it.
- **Controller rays do not show or interact:** confirm the XR Device Simulator is active, the XR Origin controller objects and their `XRRayInteractor` components are enabled, and the scene's EventSystem uses `XRUIInputModule`. World-space canvases need a `TrackedDeviceGraphicRaycaster`.
- **A ray hits 3D objects but not UI:** confirm the ray's **UI Interaction** setting and its **UI Press Input** action reference are assigned, and that the target canvas has a tracked-device raycaster and raycastable UI graphics.
- **Online finish images do not load:** built-in procedural finishes remain available as a fallback. Online images are loaded at runtime and require network access.
- **Packages fail to resolve:** use the required Unity editor version and check that Unity can access the Git URL declared for the Unity MCP package in `Packages/manifest.json`.

## Dependencies and bundled assets

The project uses Unity URP, uGUI, the Input System (through XR Interaction Toolkit), XR Interaction Toolkit, XR Management, and OpenXR. It also includes bundled project assets/plugins such as DOTween, QuickOutline, TextMesh Pro, and the Another Color Picker package. Review the relevant third-party package documentation and licenses before redistributing those components.

---

**Project name:** Interior Decoration Simulator - VR  
**Unity version:** 6000.6.6f1  
**Primary scene:** `Assets/Scenes/Color Room.unity`

