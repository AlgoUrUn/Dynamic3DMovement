public sealed class FallState : AirborneSubState
{
    public FallState(AirborneState owner)
        : base(owner)
    {
    }

    public override ISubState GetTransition()
    {
        if (Owner.IsMovingUpward())
        {
            return Owner.JumpState;
        }

        return null;
    }
}
