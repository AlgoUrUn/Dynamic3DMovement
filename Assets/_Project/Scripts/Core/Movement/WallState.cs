public abstract class WallState : AirborneSubState
{
    protected WallState(AirborneState owner)
        : base(owner)
    {
    }

    protected bool HasWallSlideContact()
    {
        return Owner.CanWallSlide();
    }
}
