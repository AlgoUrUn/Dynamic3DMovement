using UnityEngine;

public sealed class PlayerContext : MonoBehaviour
{
    [SerializeField] private PlayerFrameInput _frameInput;
    [SerializeField] private float _jumpBufferDuration = 0.2f;
    [SerializeField] private Transform _movementReference;

    private bool _jumpRequested;
    private float _jumpRequestExpiresAt = -1f;

    public PlayerFrameInput FrameInput => _frameInput;
    public Vector2 MoveInput => _frameInput.MoveInput;
    public Vector2 LookInput => _frameInput.LookInput;
    public Vector3 MoveDirection => GetPlanarDirection(_frameInput.MoveInput);
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

    public void SetMovementReference(Transform movementReference)
    {
        _movementReference = movementReference;
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

    private Vector3 GetPlanarDirection(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        if (_movementReference == null)
        {
            return Vector3.ClampMagnitude(new Vector3(moveInput.x, 0f, moveInput.y), 1f);
        }

        Vector3 forward = Vector3.ProjectOnPlane(_movementReference.forward, Vector3.up);
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.forward;
        }

        Vector3 right = Vector3.ProjectOnPlane(_movementReference.right, Vector3.up);
        if (right.sqrMagnitude <= 0.0001f)
        {
            right = Vector3.Cross(Vector3.up, forward);
        }

        forward.Normalize();
        right.Normalize();

        Vector3 worldDirection = (right * moveInput.x) + (forward * moveInput.y);
        return Vector3.ClampMagnitude(worldDirection, 1f);
    }
}
