public abstract class SubState
{
    protected SubState(GroundedState owner)
    {
        Owner = owner;
    }

    protected GroundedState Owner { get; }

    public virtual void OnEnter(SubState previousState)
    {
    }

    public virtual void OnExit(SubState nextState)
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

    public virtual SubState GetTransition()
    {
        return null;
    }
}
