public sealed class IdleState : SubState
{
    public IdleState(GroundedState owner)
        : base(owner)
    {
    }

    public override SubState GetTransition()
    {
        if (Owner.HasMoveInput())
        {
            return Owner.MoveState;
        }

        return null;
    }
}
