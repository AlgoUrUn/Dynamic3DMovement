using UnityEngine;

public sealed class PlayerContext : MonoBehaviour
{
    [SerializeField] private PlayerFrameInput _frameInput;

    public PlayerFrameInput FrameInput => _frameInput;
    public Vector2 MoveInput => _frameInput.MoveInput;
    public Vector2 LookInput => _frameInput.LookInput;
    public Vector3 MoveDirection => new Vector3(_frameInput.MoveInput.x, 0f, _frameInput.MoveInput.y);
    public bool JumpPressed => _frameInput.JumpPressed;
    public bool DashPressed => _frameInput.DashPressed;
    public bool JumpRequested => _frameInput.JumpPressed;
    public bool DashRequested => _frameInput.DashPressed;
    public Vector3 DashDirection => MoveDirection;
    public bool RunHeld => _frameInput.RunHeld;

    /// <summary>
    /// Stores input outside the reader so movement systems do not depend on input device details.
    /// </summary>
    public void SetFrameInput(PlayerFrameInput frameInput)
    {
        _frameInput = frameInput;
    }

    /// <summary>
    /// Drops transient input after consumers have had a frame to react.
    /// </summary>
    public void ClearFrameInput()
    {
        _frameInput = _frameInput.ClearFrameInput();
    }
}
