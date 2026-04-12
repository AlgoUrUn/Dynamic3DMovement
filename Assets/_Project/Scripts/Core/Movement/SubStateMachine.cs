public sealed class SubStateMachine<TState> where TState : class, ISubState
{
    private readonly string _machineName;
    private readonly System.Action<string, string, string> _transitionLogger;

    private TState _currentState;
    private TState _requestedState;

    public SubStateMachine(string machineName, System.Action<string, string, string> transitionLogger)
    {
        _machineName = machineName;
        _transitionLogger = transitionLogger;
    }

    public TState CurrentState => _currentState;

    public void Initialize(TState initialState)
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
        ResolveTransition(_currentState?.GetTransition() as TState);
    }

    public void RequestTransition(TState nextState)
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

    private void ResolveTransition(TState nextState)
    {
        if (nextState == null || nextState == _currentState)
        {
            _requestedState = null;
            return;
        }

        TransitionTo(nextState);
        _requestedState = null;
    }

    private void TransitionTo(TState nextState)
    {
        TState previousState = _currentState;
        _currentState?.OnExit(nextState);
        _currentState = nextState;
        _transitionLogger?.Invoke(
            _machineName,
            previousState?.GetType().Name ?? "None",
            nextState.GetType().Name);
        _currentState.OnEnter(previousState);
    }
}
