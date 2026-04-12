using KinematicCharacterController;
using UnityEngine;

public abstract class RootLocomotionState
{
    protected RootLocomotionState(LocomotionStateMachine stateMachine, PlayerCharacterController controller)
    {
        StateMachine = stateMachine;
        Controller = controller;
    }

    protected LocomotionStateMachine StateMachine { get; }
    protected PlayerCharacterController Controller { get; }

    public virtual void OnEnter(RootLocomotionState previousState)
    {
    }

    public virtual void OnExit(RootLocomotionState nextState)
    {
    }

    public virtual void BeforeCharacterUpdate(float deltaTime)
    {
    }

    public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        // Rotation is intentionally unchanged until camera-facing rules are introduced.
    }

    public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
    }

    public virtual void PostGroundingUpdate(float deltaTime)
    {
    }

    public virtual void AfterCharacterUpdate(float deltaTime)
    {
    }

    public virtual RootLocomotionState GetTransition()
    {
        return null;
    }

    public virtual void OnGroundHit(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public virtual void OnMovementHit(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public virtual void ProcessHitStabilityReport(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        Vector3 atCharacterPosition,
        Quaternion atCharacterRotation,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public virtual void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
}
