using UnityEngine;

public sealed class AirborneState : RootLocomotionState
{
    private readonly SubStateMachine<AirborneSubState> _subStateMachine;
    private readonly JumpState _jumpState;
    private readonly FallState _fallState;
    private readonly WallSlideState _wallSlideState;

    private AirborneSubState _entryStateOverride;

    public AirborneState(LocomotionStateMachine stateMachine, PlayerCharacterController controller)
        : base(stateMachine, controller)
    {
        _subStateMachine = new SubStateMachine<AirborneSubState>(
            "AirborneSubStateMachine",
            controller.LogStateTransition);
        _jumpState = new JumpState(this);
        _fallState = new FallState(this);
        _wallSlideState = new WallSlideState(this);
    }

    public JumpState JumpState => _jumpState;
    public FallState FallState => _fallState;
    public WallSlideState WallSlideState => _wallSlideState;
    public string CurrentSubStateName => _subStateMachine.CurrentState?.GetType().Name;

    public override void OnEnter(RootLocomotionState previousState)
    {
        _subStateMachine.Initialize(GetInitialSubState());
    }

    public override void BeforeCharacterUpdate(float deltaTime)
    {
        _subStateMachine.BeforeUpdate(deltaTime);
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _subStateMachine.Update(deltaTime);
        Controller.ApplyPlanarMovement(ref currentVelocity);
        Controller.ApplyGravity(ref currentVelocity, deltaTime);

        if (_subStateMachine.CurrentState == _wallSlideState)
        {
            Controller.ClampWallSlideFallSpeed(ref currentVelocity);
        }
    }

    public override void AfterCharacterUpdate(float deltaTime)
    {
        _subStateMachine.AfterUpdate(deltaTime);
    }

    public override RootLocomotionState GetTransition()
    {
        if (Controller.IsStableOnGroundNow())
        {
            return StateMachine.GroundedState;
        }

        return null;
    }

    public void PrepareForJump()
    {
        _entryStateOverride = _jumpState;
    }

    public bool IsMovingUpward()
    {
        return Controller.LastKnownVerticalVelocity > 0f;
    }

    public bool CanWallSlide()
    {
        return Controller.CanWallSlideNow;
    }

    private AirborneSubState GetInitialSubState()
    {
        AirborneSubState initialState = _entryStateOverride;
        _entryStateOverride = null;

        if (initialState != null)
        {
            return initialState;
        }

        if (CanWallSlide())
        {
            return _wallSlideState;
        }

        if (IsMovingUpward())
        {
            return _jumpState;
        }

        return _fallState;
    }
}
