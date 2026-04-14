using System.Reflection;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class PlayerCharacterControllerTests
{
    private GameObject _playerObject;

    [TearDown]
    public void TearDown()
    {
#if UNITY_EDITOR
        Selection.activeObject = null;
#endif

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
    public void UpdateVelocity_UsesCameraForwardAsMovementReference()
    {
        var controller = CreateControllerWithInput(Vector2.up);
        var movementReference = new GameObject("MovementReference");
        movementReference.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        controller.Context.SetMovementReference(movementReference.transform);
        var velocity = Vector3.zero;

        controller.UpdateVelocity(ref velocity, 0f);

        Assert.That(velocity.x, Is.EqualTo(6f).Within(0.0001f));
        Assert.That(velocity.z, Is.EqualTo(0f).Within(0.0001f));

        Object.DestroyImmediate(movementReference);
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

        EnterGroundedState(controller);
        controller.UpdateVelocity(ref velocity, 0f);

        Assert.That(velocity.y, Is.EqualTo(controller.JumpSpeed).Within(0.0001f));
    }

    [Test]
    public void UpdateVelocity_ConsumesBufferedJumpAfterFrameInputWasCleared()
    {
        var controller = CreateControllerWithInput(Vector2.zero, jumpPressed: true);
        var velocity = Vector3.zero;

        controller.Context.ClearFrameInput();
        EnterGroundedState(controller);
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
    public void JumpStart_TransitionsToAirborneJumpState()
    {
        var controller = CreateControllerWithInput(Vector2.zero, jumpPressed: true);
        var velocity = Vector3.zero;

        EnterGroundedState(controller);
        controller.UpdateVelocity(ref velocity, 0f);

        Assert.That(velocity.y, Is.EqualTo(controller.JumpSpeed).Within(0.0001f));
        Assert.That(controller.CurrentLocomotionStateName, Is.EqualTo(nameof(AirborneState)));
        Assert.That(controller.CurrentAirborneSubStateName, Is.EqualTo(nameof(JumpState)));
    }

    [Test]
    public void AirborneState_UsesFallSubStateWhenAirborneWithoutUpwardVelocity()
    {
        var controller = CreateControllerWithInput(Vector2.zero);

        EnterAirborneState(controller);

        Assert.That(controller.CurrentLocomotionStateName, Is.EqualTo(nameof(AirborneState)));
        Assert.That(controller.CurrentAirborneSubStateName, Is.EqualTo(nameof(FallState)));
    }

    [Test]
    public void AirborneState_TransitionsFromJumpToFallWhenVerticalVelocityTurnsDownward()
    {
        var controller = CreateControllerWithInput(Vector2.zero, jumpPressed: true);
        var velocity = Vector3.zero;

        EnterGroundedState(controller);
        controller.UpdateVelocity(ref velocity, 0f);
        Assert.That(controller.CurrentAirborneSubStateName, Is.EqualTo(nameof(JumpState)));

        controller.UpdateVelocity(ref velocity, 1f);
        controller.AfterCharacterUpdate(0f);

        Assert.That(velocity.y, Is.LessThan(0f));
        Assert.That(controller.CurrentLocomotionStateName, Is.EqualTo(nameof(AirborneState)));
        Assert.That(controller.CurrentAirborneSubStateName, Is.EqualTo(nameof(FallState)));
    }

    [Test]
    public void AirborneState_ReturnsToGroundedStateAfterLanding()
    {
        var controller = CreateControllerWithInput(Vector2.zero);

        EnterAirborneState(controller);
        Assert.That(controller.CurrentLocomotionStateName, Is.EqualTo(nameof(AirborneState)));

        SetGrounded(controller, isGrounded: true);
        controller.PostGroundingUpdate(0f);

        Assert.That(controller.CurrentLocomotionStateName, Is.EqualTo(nameof(GroundedState)));
        Assert.That(controller.CurrentGroundedSubStateName, Is.EqualTo(nameof(IdleState)));
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

    [Test]
    public void OnMovementHit_UpdatesWallEnvironmentStateAfterPostGroundingUpdate()
    {
        var controller = CreateControllerWithInput(Vector2.right);
        var velocity = Vector3.down;
        var wall = new GameObject("Wall");
        wall.layer = LayerMask.NameToLayer("Default");
        var wallCollider = wall.AddComponent<BoxCollider>();
        var hitReport = default(KinematicCharacterController.HitStabilityReport);

        EnterAirborneState(controller);
        controller.BeforeCharacterUpdate(0f);
        controller.UpdateVelocity(ref velocity, 0f);
        controller.OnMovementHit(wallCollider, Vector3.left, Vector3.zero, ref hitReport);
        controller.PostGroundingUpdate(0f);
        controller.AfterCharacterUpdate(0f);

        Assert.That(controller.HasWallContactNow, Is.True);
        Assert.That(controller.IsAttachedToWallNow, Is.True);
        Assert.That(controller.CanWallSlideNow, Is.True);
        Assert.That(controller.CanWallJumpNow, Is.True);
        Assert.That(controller.CurrentWallNormal, Is.EqualTo(Vector3.left));

        Object.DestroyImmediate(wall);
    }

    [Test]
    public void ProcessHitStabilityReport_ForcesWallHitsToRemainUnstable()
    {
        var controller = CreateControllerWithInput(Vector2.zero);
        var wall = new GameObject("Wall");
        wall.layer = LayerMask.NameToLayer("Default");
        var wallCollider = wall.AddComponent<BoxCollider>();
        var hitReport = new KinematicCharacterController.HitStabilityReport
        {
            IsStable = true,
            ValidStepDetected = true,
        };

        controller.ProcessHitStabilityReport(
            wallCollider,
            Vector3.left,
            Vector3.zero,
            Vector3.zero,
            Quaternion.identity,
            ref hitReport);

        Assert.That(hitReport.IsStable, Is.False);
        Assert.That(hitReport.ValidStepDetected, Is.False);

        Object.DestroyImmediate(wall);
    }

    [Test]
    public void AirborneState_EntersWallSlideStateWhenAirborneAndTouchingWall()
    {
        var controller = CreateControllerWithInput(Vector2.right);
        var velocity = Vector3.down;
        var wall = new GameObject("Wall");
        wall.layer = LayerMask.NameToLayer("Default");
        var wallCollider = wall.AddComponent<BoxCollider>();
        var hitReport = default(KinematicCharacterController.HitStabilityReport);

        EnterAirborneState(controller);
        controller.BeforeCharacterUpdate(0f);
        controller.UpdateVelocity(ref velocity, 0f);
        controller.OnMovementHit(wallCollider, Vector3.left, Vector3.zero, ref hitReport);
        controller.PostGroundingUpdate(0f);
        controller.AfterCharacterUpdate(0f);

        Assert.That(controller.CurrentLocomotionStateName, Is.EqualTo(nameof(AirborneState)));
        Assert.That(controller.CurrentAirborneSubStateName, Is.EqualTo(nameof(WallSlideState)));

        Object.DestroyImmediate(wall);
    }

    [Test]
    public void WallSlideState_ClampsFallSpeedAndExitsWhenWallContactEnds()
    {
        var controller = CreateControllerWithInput(Vector2.right);
        var velocity = Vector3.down * 10f;
        var wall = new GameObject("Wall");
        wall.layer = LayerMask.NameToLayer("Default");
        var wallCollider = wall.AddComponent<BoxCollider>();
        var hitReport = default(KinematicCharacterController.HitStabilityReport);

        EnterAirborneState(controller);
        controller.BeforeCharacterUpdate(0f);
        controller.UpdateVelocity(ref velocity, 0f);
        controller.OnMovementHit(wallCollider, Vector3.left, Vector3.zero, ref hitReport);
        controller.PostGroundingUpdate(0f);
        controller.AfterCharacterUpdate(0f);

        velocity = Vector3.down * 10f;
        controller.BeforeCharacterUpdate(0f);
        controller.UpdateVelocity(ref velocity, 0.1f);
        controller.PostGroundingUpdate(0f);
        controller.AfterCharacterUpdate(0f);

        Assert.That(controller.CurrentAirborneSubStateName, Is.EqualTo(nameof(FallState)));
        Assert.That(velocity.y, Is.EqualTo(-controller.WallSlideMaxFallSpeed).Within(0.0001f));

        Object.DestroyImmediate(wall);
    }

    [Test]
    public void DashRequest_TransitionsToDashStateAndConsumesDashStamina()
    {
        var controller = CreateControllerWithInput(Vector2.right, dashPressed: true);
        var velocity = Vector3.zero;

        SetGrounded(controller, isGrounded: true);
        controller.UpdateVelocity(ref velocity, 0f);

        Assert.That(controller.CurrentActionStateName, Is.EqualTo(nameof(DashState)));
        Assert.That(velocity.x, Is.EqualTo(controller.DashSpeed).Within(0.0001f));
        Assert.That(controller.StaminaManager.CurrentStamina, Is.EqualTo(75f).Within(0.0001f));
    }

    [Test]
    public void WallJumpRequest_TransitionsToWallJumpStateAndConsumesWallJumpStamina()
    {
        var controller = CreateControllerWithInput(Vector2.right, jumpPressed: true);
        var velocity = Vector3.down;
        var wall = new GameObject("Wall");
        wall.layer = LayerMask.NameToLayer("Default");
        var wallCollider = wall.AddComponent<BoxCollider>();
        var hitReport = default(KinematicCharacterController.HitStabilityReport);

        EnterAirborneState(controller);
        controller.BeforeCharacterUpdate(0f);
        controller.UpdateVelocity(ref velocity, 0f);
        controller.OnMovementHit(wallCollider, Vector3.left, Vector3.zero, ref hitReport);
        controller.PostGroundingUpdate(0f);
        controller.AfterCharacterUpdate(0f);

        controller.Context.SetFrameInput(new PlayerFrameInput(Vector2.right, Vector2.zero, true, false, false));
        controller.UpdateVelocity(ref velocity, 0f);

        Assert.That(controller.CurrentActionStateName, Is.EqualTo(nameof(WallJumpState)));
        Assert.That(velocity.x, Is.EqualTo(-controller.WallJumpHorizontalSpeed).Within(0.0001f));
        Assert.That(velocity.y, Is.EqualTo(controller.WallJumpVerticalSpeed).Within(0.0001f));
        Assert.That(controller.StaminaManager.CurrentStamina, Is.EqualTo(80f).Within(0.0001f));

        Object.DestroyImmediate(wall);
    }

    [Test]
    public void DashRequest_DoesNotStartWhenStaminaIsInsufficient()
    {
        var controller = CreateControllerWithInput(Vector2.right, dashPressed: true);
        var velocity = Vector3.zero;

        controller.StaminaManager.SetCurrentStamina(10f);
        SetGrounded(controller, isGrounded: true);
        controller.UpdateVelocity(ref velocity, 0f);

        Assert.That(controller.CurrentActionStateName, Is.EqualTo(nameof(NoneActionState)));
        Assert.That(controller.StaminaManager.CurrentStamina, Is.EqualTo(10f).Within(0.0001f));
    }

    private PlayerCharacterController CreateControllerWithInput(Vector2 moveInput, bool jumpPressed = false, bool dashPressed = false)
    {
        _playerObject = new GameObject("Player");
        _playerObject.AddComponent<CapsuleCollider>();
        _playerObject.AddComponent<KinematicCharacterController.KinematicCharacterMotor>();
        _playerObject.AddComponent<GroundDetector>();
        var wallEnvironmentHandler = _playerObject.AddComponent<WallEnvironmentHandler>();
        var context = _playerObject.AddComponent<PlayerContext>();
        var staminaManager = _playerObject.AddComponent<StaminaManager>();
        var controller = _playerObject.AddComponent<PlayerCharacterController>();

        SetPrivateField(wallEnvironmentHandler, "_wallLayerName", "Default");

        staminaManager.SetCurrentStamina(staminaManager.MaxStamina);
        context.SetFrameInput(new PlayerFrameInput(moveInput, Vector2.zero, jumpPressed, dashPressed, false));
        controller.SetInputs(context);

        return controller;
    }

    private void SetGrounded(PlayerCharacterController controller, bool isGrounded)
    {
        controller.Motor.GroundingStatus.FoundAnyGround = isGrounded;
        controller.Motor.GroundingStatus.IsStableOnGround = isGrounded;
        controller.Motor.GroundingStatus.GroundNormal = Vector3.up;

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

    private void EnterAirborneState(PlayerCharacterController controller)
    {
        SetGrounded(controller, isGrounded: false);
        controller.PostGroundingUpdate(0f);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }
}
