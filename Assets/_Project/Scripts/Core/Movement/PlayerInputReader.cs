using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    private const string DefaultActionMapName = "Player";
    private const string MoveActionName = "Move";
    private const string LookActionName = "Look";
    private const string JumpActionName = "Jump";
    private const string DashActionName = "Dash Toggle";
    private const string FallbackDashActionName = "Dash";
    private const string RunActionName = "Sprint";

    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField] private PlayerContext _playerContext;
    [SerializeField] private string _actionMapName = DefaultActionMapName;
    [SerializeField] private bool _logInput;
    [SerializeField] private bool _logOnlyWhenChanged = true;
    [SerializeField] private float _logInterval = 0.25f;

    private InputActionMap _playerActionMap;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;
    private InputAction _runAction;
    private Vector2 _lastLoggedMoveInput;
    private Vector2 _lastLoggedLookInput;
    private bool _lastLoggedJumpPressed;
    private bool _lastLoggedDashPressed;
    private bool _lastLoggedRunHeld;
    private float _lastLogTime;
    private bool _hasLoggedInput;

    public Vector2 moveInput { get; private set; }
    public Vector2 lookInput { get; private set; }
    public bool jumpPressed { get; private set; }
    public bool dashPressed { get; private set; }
    public bool runHeld { get; private set; }

    private void Awake()
    {
        ResolvePlayerContext();
        ResolveActions();
    }

    private void OnEnable()
    {
        ResolvePlayerContext();
        ResolveActions();
        EnableActions();
    }

    private void OnDisable()
    {
        DisableActions();
        ClearOneShotInputs();
    }

    private void Update()
    {
        RefreshInputContext();
        LogInputState();
    }

    private void LateUpdate()
    {
        ClearFrameInput();
    }

    public void RefreshInputContext()
    {
        ReadInput();
        ApplyInputToContext();
    }

    public void ReadInput()
    {
        moveInput = ReadVector2(_moveAction, ReadFallbackMoveInput());
        lookInput = ReadVector2(_lookAction, ReadFallbackLookInput());
        jumpPressed = ReadPressedThisFrame(_jumpAction, IsFallbackJumpPressedThisFrame());
        dashPressed = ReadPressedThisFrame(_dashAction, IsFallbackDashPressedThisFrame());
        runHeld = ReadHeld(_runAction, IsFallbackRunHeld());
    }

    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    public Vector2 GetLookInput()
    {
        return lookInput;
    }

    public bool IsJumpPressed()
    {
        return jumpPressed;
    }

    public bool IsDashPressed()
    {
        return dashPressed;
    }

    public bool IsRunHeld()
    {
        return runHeld;
    }

    public void ClearOneShotInputs()
    {
        jumpPressed = false;
        dashPressed = false;
    }

    public void ClearFrameInput()
    {
        lookInput = Vector2.zero;
        ClearOneShotInputs();

        if (_playerContext != null)
        {
            _playerContext.ClearFrameInput();
        }
    }

    private void ApplyInputToContext()
    {
        ResolvePlayerContext();

        if (_playerContext == null)
        {
            return;
        }

        _playerContext.SetFrameInput(new PlayerFrameInput(
            moveInput,
            lookInput,
            jumpPressed,
            dashPressed,
            runHeld));
    }

    private void ResolvePlayerContext()
    {
        if (_playerContext != null)
        {
            return;
        }

        _playerContext = GetComponent<PlayerContext>();
    }

    private void ResolveActions()
    {
        if (_inputActions == null)
        {
            ClearActions();
            return;
        }

        _playerActionMap = _inputActions.FindActionMap(_actionMapName, false);
        if (_playerActionMap == null)
        {
            ClearActions();
            return;
        }

        _moveAction = _playerActionMap.FindAction(MoveActionName, false);
        _lookAction = _playerActionMap.FindAction(LookActionName, false);
        _jumpAction = _playerActionMap.FindAction(JumpActionName, false);
        _dashAction = _playerActionMap.FindAction(DashActionName, false);
        if (_dashAction == null)
        {
            _dashAction = _playerActionMap.FindAction(FallbackDashActionName, false);
        }

        _runAction = _playerActionMap.FindAction(RunActionName, false);
    }

    private void ClearActions()
    {
        _playerActionMap = null;
        _moveAction = null;
        _lookAction = null;
        _jumpAction = null;
        _dashAction = null;
        _runAction = null;
    }

    private void EnableActions()
    {
        if (_playerActionMap != null)
        {
            _playerActionMap.Enable();
        }
    }

    private void DisableActions()
    {
        if (_playerActionMap != null)
        {
            _playerActionMap.Disable();
        }
    }

    private Vector2 ReadVector2(InputAction action, Vector2 fallbackValue)
    {
        if (action != null)
        {
            return action.ReadValue<Vector2>();
        }

        return fallbackValue;
    }

    private bool ReadPressedThisFrame(InputAction action, bool fallbackValue)
    {
        if (action != null)
        {
            return action.WasPressedThisFrame();
        }

        return fallbackValue;
    }

    private bool ReadHeld(InputAction action, bool fallbackValue)
    {
        if (action != null)
        {
            return action.IsPressed();
        }

        return fallbackValue;
    }

    private Vector2 ReadFallbackMoveInput()
    {
        var move = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                move.x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                move.x += 1f;
            }

            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                move.y -= 1f;
            }

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                move.y += 1f;
            }
        }

        if (Gamepad.current != null && Gamepad.current.leftStick.ReadValue().sqrMagnitude > move.sqrMagnitude)
        {
            move = Gamepad.current.leftStick.ReadValue();
        }

        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        return move;
    }

    private Vector2 ReadFallbackLookInput()
    {
        var look = Vector2.zero;

        if (Mouse.current != null)
        {
            look = Mouse.current.delta.ReadValue();
        }

        if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > look.sqrMagnitude)
        {
            look = Gamepad.current.rightStick.ReadValue();
        }

        return look;
    }

    private bool IsFallbackJumpPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }

        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }

    private bool IsFallbackDashPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            return true;
        }

        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }

    private bool IsFallbackRunHeld()
    {
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
        {
            return true;
        }

        if (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed)
        {
            return true;
        }

        return false;
    }

    private void LogInputState()
    {
        if (!_logInput)
        {
            return;
        }

        if (Time.unscaledTime < _lastLogTime + _logInterval)
        {
            return;
        }

        if (_logOnlyWhenChanged && !HasInputChangedSinceLastLog())
        {
            return;
        }

        Debug.Log(
            $"[PlayerInputReader] Move={moveInput}, Look={lookInput}, Jump={jumpPressed}, Dash={dashPressed}, Run={runHeld}",
            this);

        _lastLoggedMoveInput = moveInput;
        _lastLoggedLookInput = lookInput;
        _lastLoggedJumpPressed = jumpPressed;
        _lastLoggedDashPressed = dashPressed;
        _lastLoggedRunHeld = runHeld;
        _lastLogTime = Time.unscaledTime;
        _hasLoggedInput = true;
    }

    private bool HasInputChangedSinceLastLog()
    {
        if (!_hasLoggedInput)
        {
            return true;
        }

        if (_lastLoggedMoveInput != moveInput)
        {
            return true;
        }

        if (_lastLoggedLookInput != lookInput)
        {
            return true;
        }

        if (_lastLoggedJumpPressed != jumpPressed)
        {
            return true;
        }

        if (_lastLoggedDashPressed != dashPressed)
        {
            return true;
        }

        if (_lastLoggedRunHeld != runHeld)
        {
            return true;
        }

        return false;
    }
}
