using UnityEngine;

[System.Serializable]
public struct PlayerFrameInput
{
    [field: SerializeField] public Vector2 MoveInput { get; private set; }
    [field: SerializeField] public Vector2 LookInput { get; private set; }
    [field: SerializeField] public bool JumpPressed { get; private set; }
    [field: SerializeField] public bool DashPressed { get; private set; }
    [field: SerializeField] public bool RunHeld { get; private set; }

    public PlayerFrameInput(
        Vector2 moveInput,
        Vector2 lookInput,
        bool jumpPressed,
        bool dashPressed,
        bool runHeld)
    {
        MoveInput = moveInput;
        LookInput = lookInput;
        JumpPressed = jumpPressed;
        DashPressed = dashPressed;
        RunHeld = runHeld;
    }

    /// <summary>
    /// Preserves held input while preventing transient input from being consumed twice.
    /// </summary>
    public PlayerFrameInput ClearFrameInput()
    {
        return new PlayerFrameInput(MoveInput, Vector2.zero, false, false, RunHeld);
    }
}
