using UnityEngine;
using UnityEngine.InputSystem;

// Mac controls: WASD to walk, mouse to look.
// When the game runs in a VR headset, this script hands over to VrPlayer and switches itself off.
public class DesktopPlayer : MonoBehaviour
{
    public float walkSpeed = 4f;
    public float lookSensitivity = 2f;

    FarmPrototype farm;
    float yaw;
    float pitch;

    void Awake()
    {
        if (FarmInput.HeadsetActive)
        {
            gameObject.AddComponent<VrPlayer>();
            enabled = false; // Start and Update of this script will not run
        }
    }

    void Start()
    {
        yaw = transform.eulerAngles.y;
        farm = GetComponentInParent<FarmPrototype>();
        LockCursor(farm == null || !farm.AtMainMenu);
    }

    void Update()
    {
        if (farm != null && farm.AtMainMenu) return;
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) LockCursor(false);
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);

        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = mouse.delta.ReadValue() * 0.1f; // 0.1 keeps the same feel as the old input
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -70f, 70f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // During the intro the game moves you (home, bus); you can still look around.
        if (FarmInput.MovementLocked) return;

        Vector2 input = FarmInput.Move();
        Vector3 move = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
        Vector3 feet = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 next = transform.position + PlayerBody.Move(feet, move * walkSpeed * Time.deltaTime); // walls stop you
        transform.position = PlayerBody.ClampToFarm(new Vector3(next.x, 1.7f, next.z));
    }

    // Puts the player somewhere else, looking towards 'newYaw' (used by the intro between fades).
    public void PlaceAt(Vector3 feet, float newYaw)
    {
        transform.position = new Vector3(feet.x, feet.y + 1.7f, feet.z);
        yaw = newYaw;
        pitch = 0f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    public static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
