using UnityEngine;

public sealed class ActionStateMachine
{
    private readonly PlayerCharacterController _controller;
    private readonly NoneActionState _noneActionState;
    private readonly DashState _dashState;
    private readonly WallJumpState _wallJumpState;

    private ActionState _currentState;

    public ActionStateMachine(PlayerCharacterController controller)
    {
        _controller = controller;
        _noneActionState = new NoneActionState(this, controller);
        _dashState = new DashState(this, controller);
        _wallJumpState = new WallJumpState(this, controller);
    }

    public NoneActionState NoneActionState => _noneActionState;
    public DashState DashState => _dashState;
    public WallJumpState WallJumpState => _wallJumpState;
    public ActionState CurrentState => _currentState;
    public string CurrentStateName => _currentState?.GetType().Name;

    public void Initialize()
    {
        TransitionTo(_noneActionState);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        ResolveTransition();
        _currentState?.UpdateVelocity(ref currentVelocity, deltaTime);
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        _currentState?.AfterCharacterUpdate(deltaTime);
        ResolveTransition();
    }

    private void ResolveTransition()
    {
        ActionState nextState = _currentState?.GetTransition();
        if (nextState == null || nextState == _currentState)
        {
            return;
        }

        TransitionTo(nextState);
    }

    private void TransitionTo(ActionState nextState)
    {
        ActionState previousState = _currentState;
        _currentState?.OnExit(nextState);
        _currentState = nextState;
        _controller.LogStateTransition(
            nameof(ActionStateMachine),
            previousState?.GetType().Name ?? "None",
            nextState.GetType().Name);
        _currentState.OnEnter(previousState);
    }
}

public abstract class ActionState
{
    protected ActionState(ActionStateMachine stateMachine, PlayerCharacterController controller)
    {
        StateMachine = stateMachine;
        Controller = controller;
    }

    protected ActionStateMachine StateMachine { get; }
    protected PlayerCharacterController Controller { get; }

    public virtual void OnEnter(ActionState previousState)
    {
    }

    public virtual void OnExit(ActionState nextState)
    {
    }

    public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
    }

    public virtual void AfterCharacterUpdate(float deltaTime)
    {
    }

    public virtual ActionState GetTransition()
    {
        return null;
    }
}
