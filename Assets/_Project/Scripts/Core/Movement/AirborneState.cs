using UnityEngine;

public sealed class AirborneState : RootLocomotionState
{
    public AirborneState(LocomotionStateMachine stateMachine, PlayerCharacterController controller)
        : base(stateMachine, controller)
    {
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        Controller.ApplyPlanarMovement(ref currentVelocity);
        Controller.ApplyGravity(ref currentVelocity, deltaTime);
    }

    public override RootLocomotionState GetTransition()
    {
        if (Controller.IsStableOnGroundNow())
        {
            return StateMachine.GroundedState;
        }

        return null;
    }
}
