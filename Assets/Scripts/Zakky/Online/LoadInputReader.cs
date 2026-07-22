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

    public Vector2 Stick { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool StartPressed { get; private set; }

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

        JumpPressed = false;
        AttackPressed = false;
        StartPressed = false;

        return data;
    }
}