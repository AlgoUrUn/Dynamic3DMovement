using NUnit.Framework;
using UnityEngine;

public sealed class PlayerCharacterControllerTests
{
    private GameObject _playerObject;

    [TearDown]
    public void TearDown()
    {
        if (_playerObject != null)
        {
            Object.DestroyImmediate(_playerObject);
        }
    }

    [TestCase(-1f, 0f, -6f, 0f)]
    [TestCase(1f, 0f, 6f, 0f)]
    [TestCase(0f, 1f, 0f, 6f)]
    [TestCase(0f, -1f, 0f, -6f)]
    public void UpdateVelocity_AppliesHorizontalMovement(float inputX, float inputY, float expectedX, float expectedZ)
    {
        var controller = CreateControllerWithInput(new Vector2(inputX, inputY));
        var velocity = Vector3.zero;

        controller.UpdateVelocity(ref velocity, 0f);

        Assert.That(velocity.x, Is.EqualTo(expectedX).Within(0.0001f));
        Assert.That(velocity.z, Is.EqualTo(expectedZ).Within(0.0001f));
    }

    [Test]
    public void UpdateVelocity_AppliesGravityWhileAirborne()
    {
        var controller = CreateControllerWithInput(Vector2.zero);
        var velocity = Vector3.zero;

        controller.UpdateVelocity(ref velocity, 0.5f);

        Assert.That(velocity.y, Is.EqualTo(-15f).Within(0.0001f));
    }

    [Test]
    public void UpdateVelocity_JumpsOnlyWhenGrounded()
    {
        var controller = CreateControllerWithInput(Vector2.zero, jumpPressed: true);
        var velocity = Vector3.zero;

        SetGrounded(controller, isGrounded: true);
        controller.UpdateVelocity(ref velocity, 0f);

        Assert.That(velocity.y, Is.EqualTo(controller.JumpSpeed).Within(0.0001f));
    }

    [Test]
    public void UpdateVelocity_ConsumesBufferedJumpAfterFrameInputWasCleared()
    {
        var controller = CreateControllerWithInput(Vector2.zero, jumpPressed: true);
        var velocity = Vector3.zero;

        controller.Context.ClearFrameInput();
        SetGrounded(controller, isGrounded: true);
        controller.UpdateVelocity(ref velocity, 0f);

        Assert.That(velocity.y, Is.EqualTo(controller.JumpSpeed).Within(0.0001f));
        Assert.That(controller.Context.JumpRequested, Is.False);
    }

    [Test]
    public void UpdateVelocity_DoesNotJumpWhileAirborne()
    {
        var controller = CreateControllerWithInput(Vector2.zero, jumpPressed: true);
        var velocity = Vector3.zero;

        SetGrounded(controller, isGrounded: false);
        controller.UpdateVelocity(ref velocity, 0.5f);

        Assert.That(velocity.y, Is.EqualTo(-15f).Within(0.0001f));
    }

    [Test]
    public void UpdateVelocity_UsesSofterGravityWhileRisingAndStrongerGravityWhileFalling()
    {
        var controller = CreateControllerWithInput(Vector2.zero);
        var risingVelocity = Vector3.up * controller.JumpSpeed;
        var fallingVelocity = Vector3.down * 10f;

        controller.UpdateVelocity(ref risingVelocity, 0.1f);
        controller.UpdateVelocity(ref fallingVelocity, 0.1f);

        Assert.That(risingVelocity.y, Is.EqualTo(5.75f).Within(0.0001f));
        Assert.That(fallingVelocity.y, Is.EqualTo(-13.75f).Within(0.0001f));
    }

    [Test]
    public void GroundedState_UsesIdleSubStateWhenMoveInputIsMissing()
    {
        var controller = CreateControllerWithInput(Vector2.zero);

        EnterGroundedState(controller);

        Assert.That(controller.CurrentLocomotionStateName, Is.EqualTo(nameof(GroundedState)));
        Assert.That(controller.CurrentGroundedSubStateName, Is.EqualTo(nameof(IdleState)));
    }

    [Test]
    public void GroundedState_UsesMoveSubStateWhenMoveInputExists()
    {
        var controller = CreateControllerWithInput(Vector2.right);

        EnterGroundedState(controller);

        Assert.That(controller.CurrentLocomotionStateName, Is.EqualTo(nameof(GroundedState)));
        Assert.That(controller.CurrentGroundedSubStateName, Is.EqualTo(nameof(MoveState)));
    }

    [Test]
    public void GroundedState_TransitionsBetweenIdleAndMoveBasedOnInput()
    {
        var controller = CreateControllerWithInput(Vector2.zero);

        EnterGroundedState(controller);
        Assert.That(controller.CurrentGroundedSubStateName, Is.EqualTo(nameof(IdleState)));

        controller.Context.SetFrameInput(new PlayerFrameInput(Vector2.up, Vector2.zero, false, false, false));
        controller.AfterCharacterUpdate(0f);
        Assert.That(controller.CurrentGroundedSubStateName, Is.EqualTo(nameof(MoveState)));

        controller.Context.SetFrameInput(new PlayerFrameInput(Vector2.zero, Vector2.zero, false, false, false));
        controller.AfterCharacterUpdate(0f);
        Assert.That(controller.CurrentGroundedSubStateName, Is.EqualTo(nameof(IdleState)));
    }

    [Test]
    public void PostGroundingUpdate_RefreshesGroundDetectorFromMotor()
    {
        var controller = CreateControllerWithInput(Vector2.zero);

        controller.Motor.GroundingStatus.FoundAnyGround = true;
        controller.Motor.GroundingStatus.IsStableOnGround = true;
        controller.Motor.GroundingStatus.GroundNormal = Vector3.up;
        controller.PostGroundingUpdate(0f);

        Assert.That(controller.GroundDetector, Is.Not.Null);
        Assert.That(controller.GroundDetector.IsGrounded, Is.True);
        Assert.That(controller.GroundDetector.LandedThisFrame, Is.True);
    }

    private PlayerCharacterController CreateControllerWithInput(Vector2 moveInput, bool jumpPressed = false)
    {
        _playerObject = new GameObject("Player");
        _playerObject.AddComponent<CapsuleCollider>();
        _playerObject.AddComponent<KinematicCharacterController.KinematicCharacterMotor>();
        _playerObject.AddComponent<GroundDetector>();
        var context = _playerObject.AddComponent<PlayerContext>();
        var controller = _playerObject.AddComponent<PlayerCharacterController>();

        context.SetFrameInput(new PlayerFrameInput(moveInput, Vector2.zero, jumpPressed, false, false));
        controller.SetInputs(context);

        return controller;
    }

    private void SetGrounded(PlayerCharacterController controller, bool isGrounded)
    {
        controller.GroundDetector.Refresh(new KinematicCharacterController.CharacterGroundingReport
        {
            FoundAnyGround = isGrounded,
            IsStableOnGround = isGrounded,
            GroundNormal = Vector3.up,
        });
    }

    private void EnterGroundedState(PlayerCharacterController controller)
    {
        SetGrounded(controller, isGrounded: true);
        controller.PostGroundingUpdate(0f);
    }
}
