using UnityEngine;

public sealed class GroundedState : RootLocomotionState
{
    private readonly SubStateMachine _subStateMachine;
    private readonly IdleState _idleState;
    private readonly MoveState _moveState;

    public GroundedState(LocomotionStateMachine stateMachine, PlayerCharacterController controller)
        : base(stateMachine, controller)
    {
        _subStateMachine = new SubStateMachine();
        _idleState = new IdleState(this);
        _moveState = new MoveState(this);
    }

    public IdleState IdleState => _idleState;
    public MoveState MoveState => _moveState;
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

        if (Controller.TryConsumeJump(ref currentVelocity))
        {
            StateMachine.RequestTransition(StateMachine.AirborneState);
        }
        else
        {
            Controller.ClampVerticalVelocityToGround(ref currentVelocity);
        }

        Controller.ApplyGravity(ref currentVelocity, deltaTime);
    }

    public override void AfterCharacterUpdate(float deltaTime)
    {
        _subStateMachine.AfterUpdate(deltaTime);
    }

    public override RootLocomotionState GetTransition()
    {
        if (!Controller.IsStableOnGroundNow())
        {
            return StateMachine.AirborneState;
        }

        return null;
    }

    public bool HasMoveInput()
    {
        return Controller.HasMoveInput();
    }

    private SubState GetInitialSubState()
    {
        if (HasMoveInput())
        {
            return _moveState;
        }

        return _idleState;
    }
}
