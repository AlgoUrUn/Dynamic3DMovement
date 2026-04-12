public sealed class IdleState : SubState
{
    public IdleState(GroundedState owner)
        : base(owner)
    {
    }

    public override ISubState GetTransition()
    {
        if (Owner.HasMoveInput())
        {
            return Owner.MoveState;
        }

        return null;
    }
}
