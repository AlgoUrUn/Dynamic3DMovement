using UnityEngine;

public sealed class DashState : ActionState
{
    private float _expiresAt;
    private Vector3 _dashVelocity;

    public DashState(ActionStateMachine stateMachine, PlayerCharacterController controller)
        : base(stateMachine, controller)
    {
    }

    public override void OnEnter(ActionState previousState)
    {
        _dashVelocity = Controller.BuildDashVelocity();
        _expiresAt = Time.time + Controller.DashDuration;
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity = _dashVelocity;
    }

    public override ActionState GetTransition()
    {
        if (Time.time >= _expiresAt)
        {
            return StateMachine.NoneActionState;
        }

        return null;
    }
}
