using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// One place that knows which buttons do what — on the Mac (keyboard) and in the Quest (controllers).
// Other scripts ask "is the player walking?" or "was the action button pressed?"
// and don't care which device it came from.
public static class FarmInput
{
    static InputAction move;       // WASD / arrows / left stick
    static InputAction interact;   // E / right trigger / right grip / A button
    static InputAction rightStick; // VR only: sideways = turn, forward = teleport
    static InputAction menu;       // Tab / X or ☰ on the left controller: open or close the task panel
    static InputAction inventory;  // I / Y on the left controller: open or close the inventory

    // True during the intro: walking, turning and teleport do nothing (looking around still works).
    public static bool MovementLocked;

    static readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
    static int headsetCheckedFrame = -1;
    static bool headsetActive;

    // True when the game is shown in a VR headset (Quest, or Meta XR Simulator).
    public static bool HeadsetActive
    {
        get
        {
            if (headsetCheckedFrame != Time.frameCount)
            {
                headsetCheckedFrame = Time.frameCount;
                headsetActive = false;
                SubsystemManager.GetSubsystems(displays);
                foreach (XRDisplaySubsystem display in displays)
                    if (display.running) headsetActive = true;
            }
            return headsetActive;
        }
    }

    // Walking direction: x = left/right, y = forward/back. Length is at most 1.
    public static Vector2 Move()
    {
        Setup();
        if (MovementLocked) return Vector2.zero;
        return Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
    }

    // True only on the frame the action button was pressed.
    public static bool InteractPressed()
    {
        Setup();
        return interact.WasPressedThisFrame();
    }

    // The task-panel button (Tab / X): pressed this frame, held down, released this frame.
    // A short press opens the panel; holding it for a few seconds starts a new game.
    public static bool MenuPressed()
    {
        Setup();
        return menu.WasPressedThisFrame();
    }

    public static bool MenuHeld()
    {
        Setup();
        return menu.IsPressed();
    }

    public static bool MenuReleased()
    {
        Setup();
        return menu.WasReleasedThisFrame();
    }

    // True only on the frame the inventory button was pressed.
    public static bool InventoryPressed()
    {
        Setup();
        return inventory.WasPressedThisFrame();
    }

    // Right stick (VR): sideways turns, forward aims the teleport.
    public static Vector2 RightStick()
    {
        Setup();
        if (MovementLocked) return Vector2.zero;
        return rightStick.ReadValue<Vector2>();
    }

    static void Setup()
    {
        if (move != null) return;

        move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<XRController>{LeftHand}/{Primary2DAxis}");

        interact = new InputAction("Interact", InputActionType.Button);
        interact.AddBinding("<Keyboard>/e");
        interact.AddBinding("<XRController>{RightHand}/{TriggerButton}");
        interact.AddBinding("<XRController>{RightHand}/{GripButton}");
        interact.AddBinding("<XRController>{RightHand}/{PrimaryButton}");

        rightStick = new InputAction("Right Stick", InputActionType.Value, expectedControlType: "Vector2");
        rightStick.AddBinding("<XRController>{RightHand}/{Primary2DAxis}");

        menu = new InputAction("Menu", InputActionType.Button);
        menu.AddBinding("<Keyboard>/tab");
        menu.AddBinding("<XRController>{LeftHand}/{PrimaryButton}");   // X
        menu.AddBinding("<XRController>{LeftHand}/{MenuButton}");      // ☰

        inventory = new InputAction("Inventory", InputActionType.Button);
        inventory.AddBinding("<Keyboard>/i");
        inventory.AddBinding("<XRController>{LeftHand}/{SecondaryButton}"); // Y

        move.Enable();
        interact.Enable();
        rightStick.Enable();
        menu.Enable();
        inventory.Enable();
    }

    // Unity can keep static values between Play sessions; start fresh every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetBetweenPlaySessions()
    {
        move = null;
        interact = null;
        rightStick = null;
        menu = null;
        inventory = null;
        MovementLocked = false;
        headsetCheckedFrame = -1;
    }
}
