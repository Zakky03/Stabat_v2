using UnityEngine;
using UnityEngine.InputSystem;

public class LocalInputReader : MonoBehaviour
{
    public static LocalInputReader Instance { get; private set; }

    [SerializeField] private InputActionAsset actions;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;

    public Vector2 Stick { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool AttackPressed { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        moveAction = actions.FindAction("Stick", true);
        jumpAction = actions.FindAction("A", false);
        attackAction = actions.FindAction("B", false);
    }

    private void OnEnable()
    {
        actions.Enable();
    }

    private void OnDisable()
    {
        actions.Disable();
    }

    private void Update()
    {
        Stick = moveAction.ReadValue<Vector2>();

        JumpPressed |= jumpAction != null && jumpAction.WasPressedThisFrame();
        AttackPressed |= attackAction != null && attackAction.WasPressedThisFrame();
    }

    public NetworkInputData ConsumeFusionInput()
    {
        var data = new NetworkInputData
        {
            Stick = Stick
        };

        data.Buttons.Set(PlayerButtons.Jump, JumpPressed);
        data.Buttons.Set(PlayerButtons.Action, AttackPressed);

        JumpPressed = false;
        AttackPressed = false;

        return data;
    }
}