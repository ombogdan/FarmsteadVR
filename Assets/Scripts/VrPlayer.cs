using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

// Controls in the Quest headset. DesktopPlayer adds this script when a headset is active.
//
// How it works: the player stands inside an invisible "rig".
// - Your real head and hands move the camera and the hands inside the rig (TrackedPoseDriver).
// - The sticks move, turn and teleport the whole rig around the farm.
//
// Left stick: walk where you look (walls stop you).
// Right stick left/right: turn by 45 degrees. Right stick forward: aim teleport, release to jump.
// Right hand laser + trigger / grip / A: use things.
public class VrPlayer : MonoBehaviour
{
    public float walkSpeed = 2f;           // slower than on the Mac: fast movement in VR can make people dizzy
    public float snapTurnAngle = 45f;
    public float standingEyeHeight = 1.6f; // used only if the headset doesn't measure height from the floor

    Transform rig;          // walks around the farm
    Transform cameraOffset; // lifts the camera to eye height when needed
    VrTeleport teleport;
    bool turnReady = true;
    readonly List<XRInputSubsystem> trackers = new List<XRInputSubsystem>();

    void Start()
    {
        Transform head = transform; // this object is the camera

        // Build the rig where the desktop player was standing, looking the same way.
        rig = new GameObject("VR Rig").transform;
        rig.SetParent(head.parent, false);
        rig.position = new Vector3(head.position.x, 0f, head.position.z);
        rig.rotation = Quaternion.Euler(0f, head.eulerAngles.y, 0f);

        cameraOffset = new GameObject("Camera Offset").transform;
        cameraOffset.SetParent(rig, false);

        head.SetParent(cameraOffset, false);
        head.localPosition = Vector3.zero;
        head.localRotation = Quaternion.identity;

        // The headset moves the camera, the controllers move the hands.
        TrackPose(gameObject, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation");
        Transform leftHand = Hand("Left Hand", "{LeftHand}");
        Transform rightHand = Hand("Right Hand", "{RightHand}");

        // Aim with the right hand instead of the head.
        FarmPrototype farm = GetComponentInParent<FarmPrototype>();
        if (farm != null)
        {
            farm.aimSource = rightHand;
            farm.aimDistance = 6f;
        }

        gameObject.AddComponent<VrHands>().Init(leftHand, rightHand, farm);
        teleport = gameObject.AddComponent<VrTeleport>();
        teleport.Init(this, rightHand);

        // Ask the headset to measure head height from the floor, so your real height is used.
        SubsystemManager.GetSubsystems(trackers);
        foreach (XRInputSubsystem tracker in trackers)
            tracker.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);

        // Coins on the left hand, task panel, speech bubbles (OnGUI is not visible inside a headset).
        gameObject.AddComponent<VrHud>().Init(leftHand);
    }

    Transform Hand(string name, string whichHand)
    {
        Transform hand = new GameObject(name).transform;
        hand.SetParent(cameraOffset, false);
        // "pointer" is the direction a controller points at, the same one Quest menus use.
        TrackPose(hand.gameObject, "<XRController>" + whichHand + "/pointerPosition", "<XRController>" + whichHand + "/pointerRotation");
        return hand;
    }

    static void TrackPose(GameObject target, string positionPath, string rotationPath)
    {
        InputAction position = new InputAction(target.name + " Position", binding: positionPath, expectedControlType: "Vector3");
        InputAction rotation = new InputAction(target.name + " Rotation", binding: rotationPath, expectedControlType: "Quaternion");
        position.Enable();
        rotation.Enable();
        TrackedPoseDriver pose = target.AddComponent<TrackedPoseDriver>();
        pose.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        pose.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        pose.positionInput = new InputActionProperty(position);
        pose.rotationInput = new InputActionProperty(rotation);
    }

    public bool IsReady => rig != null;

    // Puts the player somewhere else (used by the intro between fades):
    // turns the rig so you look towards 'yaw' and moves it so your head is above 'feet'.
    public void PlaceAt(Vector3 feet, float yaw)
    {
        if (rig == null) return;
        rig.RotateAround(transform.position, Vector3.up, Mathf.DeltaAngle(transform.eulerAngles.y, yaw));
        Vector3 head = transform.position;
        rig.position += new Vector3(feet.x - head.x, feet.y - rig.position.y, feet.z - head.z);
    }

    // Moves the rig so that your head ends up above the target point.
    public void TeleportTo(Vector3 target)
    {
        Vector3 head = transform.position;
        Vector3 shift = new Vector3(target.x - head.x, 0f, target.z - head.z);
        rig.position = ClampToFarm(rig.position + shift);
    }

    void Update()
    {
        // If the headset measures from the floor, the camera already has the right height.
        // Otherwise lift it to a typical standing eye height.
        bool fromFloor = false;
        foreach (XRInputSubsystem tracker in trackers)
            if (tracker.running && tracker.GetTrackingOriginMode() == TrackingOriginModeFlags.Floor) fromFloor = true;
        cameraOffset.localPosition = new Vector3(0f, fromFloor ? 0f : standingEyeHeight, 0f);

        // During the intro the game moves you (home, bus); sticks do nothing and nothing pulls you back.
        if (FarmInput.MovementLocked) return;

        // Walk where you look (left stick). PlayerBody stops you at walls and slides along them.
        Vector2 stick = FarmInput.Move();
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 wanted = (forward * stick.y + right * stick.x) * walkSpeed * Time.deltaTime;
        Vector3 feet = new Vector3(transform.position.x, 0f, transform.position.z);
        rig.position = ClampToFarm(rig.position + PlayerBody.Move(feet, wanted));

        // Turn in steps (right stick sideways). Smooth turning makes many people feel sick.
        if (teleport.IsAiming)
        {
            turnReady = false; // don't turn by accident right after a teleport
            return;
        }
        Vector2 turn = FarmInput.RightStick();
        if (turnReady && Mathf.Abs(turn.x) > 0.7f && turn.y < 0.5f)
        {
            rig.RotateAround(transform.position, Vector3.up, Mathf.Sign(turn.x) * snapTurnAngle);
            turnReady = false;
        }
        else if (turn.magnitude < 0.3f) turnReady = true;
    }

    static Vector3 ClampToFarm(Vector3 position)
    {
        return PlayerBody.ClampToFarm(new Vector3(position.x, 0f, position.z));
    }
}
