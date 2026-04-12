public abstract class AirborneSubState : ISubState
{
    protected AirborneSubState(AirborneState owner)
    {
        Owner = owner;
    }

    protected AirborneState Owner { get; }

    public virtual void OnEnter(ISubState previousState)
    {
    }

    public virtual void OnExit(ISubState nextState)
    {
    }

    public virtual void BeforeUpdate(float deltaTime)
    {
    }

    public virtual void Update(float deltaTime)
    {
    }

    public virtual void AfterUpdate(float deltaTime)
    {
    }

    public virtual ISubState GetTransition()
    {
        return null;
    }
}
