public sealed class MoveState : SubState
{
    public MoveState(GroundedState owner)
        : base(owner)
    {
    }

    public override ISubState GetTransition()
    {
        if (!Owner.HasMoveInput())
        {
            return Owner.IdleState;
        }

        return null;
    }
}
