using UnityEngine;

public sealed class PlayerContext : MonoBehaviour
{
    [SerializeField] private PlayerFrameInput _frameInput;
    [SerializeField] private float _jumpBufferDuration = 0.2f;

    private bool _jumpRequested;
    private float _jumpRequestExpiresAt = -1f;

    public PlayerFrameInput FrameInput => _frameInput;
    public Vector2 MoveInput => _frameInput.MoveInput;
    public Vector2 LookInput => _frameInput.LookInput;
    public Vector3 MoveDirection => new Vector3(_frameInput.MoveInput.x, 0f, _frameInput.MoveInput.y);
    public bool JumpPressed => _frameInput.JumpPressed;
    public bool DashPressed => _frameInput.DashPressed;
    public bool JumpRequested => _jumpRequested && Time.time <= _jumpRequestExpiresAt;
    public bool DashRequested => _frameInput.DashPressed;
    public Vector3 DashDirection => MoveDirection;
    public bool RunHeld => _frameInput.RunHeld;
    public float JumpBufferDuration => _jumpBufferDuration;

    /// <summary>
    /// Stores input outside the reader so movement systems do not depend on input device details.
    /// </summary>
    public void SetFrameInput(PlayerFrameInput frameInput)
    {
        _frameInput = frameInput;

        if (frameInput.JumpPressed)
        {
            QueueJumpRequest();
        }
    }

    public bool ConsumeJumpRequest()
    {
        if (!JumpRequested)
        {
            ClearJumpRequest();
            return false;
        }

        ClearJumpRequest();
        return true;
    }

    /// <summary>
    /// Drops transient input after consumers have had a frame to react.
    /// </summary>
    public void ClearFrameInput()
    {
        _frameInput = _frameInput.ClearFrameInput();
    }

    private void QueueJumpRequest()
    {
        _jumpRequested = true;
        _jumpRequestExpiresAt = Time.time + _jumpBufferDuration;
    }

    private void ClearJumpRequest()
    {
        _jumpRequested = false;
        _jumpRequestExpiresAt = -1f;
    }
}
