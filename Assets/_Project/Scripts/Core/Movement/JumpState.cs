public sealed class JumpState : AirborneSubState
{
    public JumpState(AirborneState owner)
        : base(owner)
    {
    }

    public override ISubState GetTransition()
    {
        if (!Owner.IsMovingUpward())
        {
            return Owner.FallState;
        }

        return null;
    }
}
