public sealed class NoneActionState : ActionState
{
    public NoneActionState(ActionStateMachine stateMachine, PlayerCharacterController controller)
        : base(stateMachine, controller)
    {
    }

    public override ActionState GetTransition()
    {
        if (Controller.TryStartWallJump())
        {
            return StateMachine.WallJumpState;
        }

        if (Controller.TryStartDash())
        {
            return StateMachine.DashState;
        }

        return null;
    }
}
