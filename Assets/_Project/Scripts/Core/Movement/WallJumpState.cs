using UnityEngine;

public sealed class WallJumpState : ActionState
{
    private float _expiresAt;
    private Vector3 _wallJumpVelocity;

    public WallJumpState(ActionStateMachine stateMachine, PlayerCharacterController controller)
        : base(stateMachine, controller)
    {
    }

    public override void OnEnter(ActionState previousState)
    {
        _wallJumpVelocity = Controller.BuildWallJumpVelocity();
        _expiresAt = Time.time + Controller.WallJumpDuration;
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity = _wallJumpVelocity;
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
