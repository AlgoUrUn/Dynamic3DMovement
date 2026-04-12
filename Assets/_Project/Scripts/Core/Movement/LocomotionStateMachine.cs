using KinematicCharacterController;
using UnityEngine;

public sealed class LocomotionStateMachine
{
    private readonly PlayerCharacterController _controller;
    private readonly GroundedState _groundedState;
    private readonly AirborneState _airborneState;

    private RootLocomotionState _currentState;
    private RootLocomotionState _requestedState;

    public LocomotionStateMachine(PlayerCharacterController controller)
    {
        _controller = controller;
        _groundedState = new GroundedState(this, controller);
        _airborneState = new AirborneState(this, controller);
    }

    public RootLocomotionState CurrentState => _currentState;
    public GroundedState GroundedState => _groundedState;
    public AirborneState AirborneState => _airborneState;
    public string CurrentStateName => _currentState?.GetType().Name;
    public string CurrentGroundedSubStateName => _groundedState.CurrentSubStateName;
    public string CurrentAirborneSubStateName => _airborneState.CurrentSubStateName;

    public void Initialize()
    {
        TransitionTo(_controller.IsStableOnGroundNow() ? _groundedState : _airborneState);
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _currentState?.BeforeCharacterUpdate(deltaTime);
        ResolveRequestedTransition();
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        _currentState?.UpdateRotation(ref currentRotation, deltaTime);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _currentState?.UpdateVelocity(ref currentVelocity, deltaTime);
        ResolveRequestedTransition();
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        _currentState?.PostGroundingUpdate(deltaTime);
        ResolveTransition(_currentState?.GetTransition());
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        _currentState?.AfterCharacterUpdate(deltaTime);
        ResolveRequestedTransition();
    }

    public void OnGroundHit(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        _currentState?.OnGroundHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
    }

    public void OnMovementHit(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        _currentState?.OnMovementHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
    }

    public void ProcessHitStabilityReport(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        Vector3 atCharacterPosition,
        Quaternion atCharacterRotation,
        ref HitStabilityReport hitStabilityReport)
    {
        _currentState?.ProcessHitStabilityReport(
            hitCollider,
            hitNormal,
            hitPoint,
            atCharacterPosition,
            atCharacterRotation,
            ref hitStabilityReport);
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
        _currentState?.OnDiscreteCollisionDetected(hitCollider);
    }

    public void UpdateStates()
    {
        // State progression continues in KCC callbacks; this method exists so the player-level
        // orchestration flow can explicitly include locomotion state evaluation.
    }

    public void RequestTransition(RootLocomotionState nextState)
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

    private void ResolveTransition(RootLocomotionState nextState)
    {
        if (nextState == null || nextState == _currentState)
        {
            _requestedState = null;
            return;
        }

        TransitionTo(nextState);
        _requestedState = null;
    }

    private void TransitionTo(RootLocomotionState nextState)
    {
        RootLocomotionState previousState = _currentState;
        _currentState?.OnExit(nextState);
        _currentState = nextState;
        _controller.LogStateTransition(
            nameof(LocomotionStateMachine),
            previousState?.GetType().Name ?? "None",
            nextState.GetType().Name);
        _currentState.OnEnter(previousState);
    }
}
