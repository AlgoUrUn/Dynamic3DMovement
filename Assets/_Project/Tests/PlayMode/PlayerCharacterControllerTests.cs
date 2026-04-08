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

    private PlayerCharacterController CreateControllerWithInput(Vector2 moveInput)
    {
        _playerObject = new GameObject("Player");
        _playerObject.AddComponent<CapsuleCollider>();
        _playerObject.AddComponent<KinematicCharacterController.KinematicCharacterMotor>();
        _playerObject.AddComponent<GroundDetector>();
        var context = _playerObject.AddComponent<PlayerContext>();
        var controller = _playerObject.AddComponent<PlayerCharacterController>();

        context.SetFrameInput(new PlayerFrameInput(moveInput, Vector2.zero, false, false, false));
        controller.SetInputs(context);

        return controller;
    }
}
