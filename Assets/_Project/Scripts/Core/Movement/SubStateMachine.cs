public sealed class SubStateMachine
{
    private SubState _currentState;
    private SubState _requestedState;

    public SubState CurrentState => _currentState;

    public void Initialize(SubState initialState)
    {
        TransitionTo(initialState);
    }

    public void BeforeUpdate(float deltaTime)
    {
        _currentState?.BeforeUpdate(deltaTime);
        ResolveRequestedTransition();
    }

    public void Update(float deltaTime)
    {
        _currentState?.Update(deltaTime);
        ResolveRequestedTransition();
    }

    public void AfterUpdate(float deltaTime)
    {
        _currentState?.AfterUpdate(deltaTime);
        ResolveTransition(_currentState?.GetTransition());
    }

    public void RequestTransition(SubState nextState)
    {
        if (nextState == null || nextState == _currentState)
        {
            return;
        }

        _requestedState = nextState;
    }

    private void ResolveRequestedTransition()
    {
        ResolveTransition(_requestedState);
    }

    private void ResolveTransition(SubState nextState)
    {
        if (nextState == null || nextState == _currentState)
        {
            _requestedState = null;
            return;
        }

        TransitionTo(nextState);
        _requestedState = null;
    }

    private void TransitionTo(SubState nextState)
    {
        _currentState?.OnExit(nextState);
        SubState previousState = _currentState;
        _currentState = nextState;
        _currentState.OnEnter(previousState);
    }
}
