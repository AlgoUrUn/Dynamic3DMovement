public sealed class WallSlideState : WallState
{
    public WallSlideState(AirborneState owner)
        : base(owner)
    {
    }

    public override ISubState GetTransition()
    {
        if (!HasWallSlideContact())
        {
            if (Owner.IsMovingUpward())
            {
                return Owner.JumpState;
            }

            return Owner.FallState;
        }

        return null;
    }
}
