using UnityEngine;

public sealed class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerCharacterController _controller;
    [SerializeField] private GroundDetector _groundDetector;

    private bool _wasInJumpState;
    private bool _wasDashing;
    private bool _wasWallSliding;

    public static class AnimatorParameters
    {
        public const string LocomotionSpeed = "speed";
        public const string IsGrounded = "grounded";
        public const string VerticalVelocity = "verticalVelocity";
        public const string JumpTrigger = "jump";
        public const string LandTrigger = "land";
        public const string DashTrigger = "dash";
        public const string WallSlideTrigger = "wallSlide";
        public const string WallSlideActive = "wallSlideActive";
    }

    public Animator Animator => _animator;
    public PlayerCharacterController Controller => _controller;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void UpdateAnimation()
    {
        ResolveReferences();

        if (_animator == null || _controller == null)
        {
            return;
        }

        bool isGrounded = _controller.IsStableOnGroundNow();
        bool isInJumpState = _controller.CurrentAirborneSubStateName == nameof(JumpState);
        bool isDashing = _controller.CurrentActionStateName == nameof(DashState);
        bool isWallSliding = _controller.CurrentAirborneSubStateName == nameof(WallSlideState);

        UpdateLocomotion(_controller.LastKnownPlanarSpeed, isGrounded);
        UpdateVerticalVelocity(_controller.LastKnownVerticalVelocity);
        SetWallSlideActive(isWallSliding);

        if (!_wasInJumpState && isInJumpState)
        {
            PlayJump();
        }

        if (_groundDetector != null && _groundDetector.LandedThisFrame)
        {
            PlayLand();
        }

        if (!_wasDashing && isDashing)
        {
            PlayDash();
        }

        if (!_wasWallSliding && isWallSliding)
        {
            PlayWallSlide();
        }

        _wasInJumpState = isInJumpState;
        _wasDashing = isDashing;
        _wasWallSliding = isWallSliding;
    }

    public void UpdateLocomotion(float speed, bool grounded)
    {
        if (_animator == null)
        {
            return;
        }

        _animator.SetFloat(AnimatorParameters.LocomotionSpeed, speed);
        _animator.SetBool(AnimatorParameters.IsGrounded, grounded);
    }

    public void UpdateVerticalVelocity(float verticalVelocity)
    {
        if (_animator == null)
        {
            return;
        }

        _animator.SetFloat(AnimatorParameters.VerticalVelocity, verticalVelocity);
    }

    public void PlayJump()
    {
        SetTrigger(AnimatorParameters.JumpTrigger);
    }

    public void PlayLand()
    {
        SetTrigger(AnimatorParameters.LandTrigger);
    }

    public void PlayDash()
    {
        SetTrigger(AnimatorParameters.DashTrigger);
    }

    public void PlayWallSlide()
    {
        SetTrigger(AnimatorParameters.WallSlideTrigger);
    }

    public void SetWallSlideActive(bool active)
    {
        if (_animator == null)
        {
            return;
        }

        _animator.SetBool(AnimatorParameters.WallSlideActive, active);
    }

    private void ResolveReferences()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_controller == null)
        {
            _controller = GetComponent<PlayerCharacterController>();
        }

        if (_groundDetector == null)
        {
            _groundDetector = GetComponent<GroundDetector>();
        }
    }

    private void SetTrigger(string triggerName)
    {
        if (_animator == null)
        {
            return;
        }

        _animator.SetTrigger(triggerName);
    }
}
