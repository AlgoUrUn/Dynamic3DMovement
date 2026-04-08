using KinematicCharacterController;
using NUnit.Framework;
using UnityEngine;

public sealed class GroundDetectorTests
{
    private GameObject _playerObject;
    private GameObject _groundObject;

    [TearDown]
    public void TearDown()
    {
        if (_playerObject != null)
        {
            Object.DestroyImmediate(_playerObject);
        }

        if (_groundObject != null)
        {
            Object.DestroyImmediate(_groundObject);
        }
    }

    [Test]
    public void Refresh_StoresGroundNormalAndStableGroundState()
    {
        var detector = CreateDetector();
        var groundCollider = CreateGroundCollider();

        detector.Refresh(new CharacterGroundingReport
        {
            FoundAnyGround = true,
            IsStableOnGround = true,
            GroundNormal = new Vector3(0f, 2f, 0f),
            GroundCollider = groundCollider,
            GroundPoint = new Vector3(1f, 0f, 2f),
        });

        Assert.That(detector.FoundAnyGround, Is.True);
        Assert.That(detector.IsStableOnGround, Is.True);
        Assert.That(detector.IsGrounded, Is.True);
        Assert.That(detector.IsAirborne, Is.False);
        Assert.That(detector.GroundNormal, Is.EqualTo(Vector3.up));
        Assert.That(detector.GroundCollider, Is.EqualTo(groundCollider));
        Assert.That(detector.GroundPoint, Is.EqualTo(new Vector3(1f, 0f, 2f)));
    }

    [Test]
    public void Refresh_DetectsLandingWhenStableGroundAppears()
    {
        var detector = CreateDetector();

        detector.Refresh(new CharacterGroundingReport());
        detector.Refresh(new CharacterGroundingReport
        {
            FoundAnyGround = true,
            IsStableOnGround = true,
            GroundNormal = Vector3.up,
        });

        Assert.That(detector.WasStableOnGround, Is.False);
        Assert.That(detector.LandedThisFrame, Is.True);
        Assert.That(detector.LeftStableGroundThisFrame, Is.False);
    }

    [Test]
    public void Refresh_DistinguishesAirborneFromUnstableGround()
    {
        var detector = CreateDetector();

        detector.Refresh(new CharacterGroundingReport
        {
            FoundAnyGround = true,
            IsStableOnGround = false,
            GroundNormal = Vector3.right,
        });

        Assert.That(detector.FoundAnyGround, Is.True);
        Assert.That(detector.IsStableOnGround, Is.False);
        Assert.That(detector.IsGrounded, Is.False);
        Assert.That(detector.IsAirborne, Is.True);
        Assert.That(detector.GroundNormal, Is.EqualTo(Vector3.right));
    }

    private GroundDetector CreateDetector()
    {
        _playerObject = new GameObject("Player");
        return _playerObject.AddComponent<GroundDetector>();
    }

    private Collider CreateGroundCollider()
    {
        _groundObject = new GameObject("Ground");
        return _groundObject.AddComponent<BoxCollider>();
    }
}
