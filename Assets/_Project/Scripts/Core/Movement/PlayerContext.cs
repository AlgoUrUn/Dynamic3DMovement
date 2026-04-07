using UnityEngine;

public sealed class PlayerContext : MonoBehaviour
{
    [SerializeField] private PlayerFrameInput _frameInput;

    public PlayerFrameInput FrameInput => _frameInput;
    public Vector2 MoveInput => _frameInput.MoveInput;
    public Vector2 LookInput => _frameInput.LookInput;
    public bool JumpPressed => _frameInput.JumpPressed;
    public bool DashPressed => _frameInput.DashPressed;
    public bool RunHeld => _frameInput.RunHeld;

    public void SetFrameInput(PlayerFrameInput frameInput)
    {
        _frameInput = frameInput;
    }

    public void ClearFrameInput()
    {
        _frameInput = _frameInput.ClearFrameInput();
    }
}
