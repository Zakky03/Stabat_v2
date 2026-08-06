using UnityEngine;
using UnityEngine.InputSystem;

public class LocalInputReader : MonoBehaviour
{
    public static LocalInputReader Instance { get; private set; }

    [SerializeField] private InputActionAsset actions;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction startAction;
    private InputAction attack1Action;
    private InputAction attack2Action;

    public Vector2 Stick { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool StartPressed { get; private set; }
    public bool Attack1Pressed { get; private set; }
    public bool Attack2Pressed { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        moveAction = actions.FindAction("Stick", true);
        jumpAction = actions.FindAction("B", false);
        attackAction = actions.FindAction("A", false);
        startAction = actions.FindAction("Start", false);
        attack1Action = actions.FindAction("X", false);
        attack2Action = actions.FindAction("Y", false);
    }

    private void OnEnable()
    {
        actions.Enable();
    }

    // Clears any button state latched before this call (e.g. an Enter/Start press made while
    // confirming a browser/page prompt prior to actually reaching the ready-check).
    public void ClearLatchedInput()
    {
        JumpPressed = false;
        AttackPressed = false;
        StartPressed = false;
        Attack1Pressed = false;
        Attack2Pressed = false;
    }

    private void OnDisable()
    {
        actions.Disable();
    }

    private void Update()
    {
        Stick = moveAction.ReadValue<Vector2>();

        JumpPressed |= jumpAction != null && jumpAction.WasPressedThisFrame();
        JumpHeld = jumpAction.IsPressed();

        AttackPressed |= attackAction != null && attackAction.WasPressedThisFrame();
        StartPressed |= startAction != null && startAction.WasPressedThisFrame();

        Attack1Pressed |= attack1Action != null && attack1Action.WasPressedThisFrame();
        Attack2Pressed |= attack2Action != null && attack2Action.WasPressedThisFrame();

        // Fallback via the classic Input Manager (UnityEngine.Input), not the new Input System:
        // gamepad support through the Input System package is unreliable specifically in WebGL
        // builds (a long-standing, still-open Unity issue), but the classic manager is reported to
        // work there. Its default joystick bindings already cover a standard gamepad — see
        // ProjectSettings/InputManager.asset ("Horizontal"/"Vertical" axes and "joystick button 0-3"
        // on Fire1/Fire2/Fire3/Jump — no extra project setup needed. Harmless to read on every
        // platform since it's plain UnityEngine.Input, not a native plugin.
        Vector2 legacyStick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (legacyStick.sqrMagnitude > Stick.sqrMagnitude)
            Stick = legacyStick;

        JumpPressed |= Input.GetKeyDown("joystick button 0");
        JumpHeld |= Input.GetKey("joystick button 0");
        AttackPressed |= Input.GetKeyDown("joystick button 1");
        StartPressed |= Input.GetKeyDown("joystick button 9");
        Attack1Pressed |= Input.GetKeyDown("joystick button 2");
        Attack2Pressed |= Input.GetKeyDown("joystick button 3");
    }

    public NetworkInputData ConsumeFusionInput()
    {
        var data = new NetworkInputData
        {
            Stick = Stick
        };

        data.Buttons.Set(PlayerButtons.Jump, JumpPressed || JumpHeld);
        data.Buttons.Set(PlayerButtons.Action, AttackPressed);
        data.Buttons.Set(PlayerButtons.Ready, StartPressed);
        data.Buttons.Set(PlayerButtons.Attack1, Attack1Pressed);
        data.Buttons.Set(PlayerButtons.Attack2, Attack2Pressed);

        JumpPressed = false;
        AttackPressed = false;
        StartPressed = false;
        Attack1Pressed = false;
        Attack2Pressed = false;

        return data;
    }
}
