using System.Reflection;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class WallEnvironmentHandlerTests
{
    private GameObject _playerObject;
    private GameObject _wallObject;

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

        if (_wallObject != null)
        {
            Object.DestroyImmediate(_wallObject);
        }
    }

    [Test]
    public void FinalizeFrame_DetectsWallAndStoresWallNormal()
    {
        var handler = CreateHandler();
        var wallCollider = CreateWallCollider();

        handler.BeginFrame();
        handler.SetKinematicContext(Vector3.zero, Vector3.down, isGrounded: false);
        handler.RegisterHit(wallCollider, new Vector3(-2f, 0f, 0f), new Vector3(4f, 1f, 2f));
        handler.FinalizeFrame(isGrounded: false);

        Assert.That(handler.HasWallContact, Is.True);
        Assert.That(handler.WallNormal, Is.EqualTo(Vector3.left));
        Assert.That(handler.WallCollider, Is.EqualTo(wallCollider));
        Assert.That(handler.WallPoint, Is.EqualTo(new Vector3(4f, 1f, 2f)));
        Assert.That(handler.CanWallJump, Is.True);
    }

    [Test]
    public void FinalizeFrame_DetectsAttachmentWhenAirborneAndMovingIntoWall()
    {
        var handler = CreateHandler();

        handler.BeginFrame();
        handler.SetKinematicContext(Vector3.right, Vector3.down * 2f, isGrounded: false);
        handler.RegisterHit(CreateWallCollider(), Vector3.left, Vector3.zero);
        handler.FinalizeFrame(isGrounded: false);

        Assert.That(handler.HasWallContact, Is.True);
        Assert.That(handler.IsAttachedToWall, Is.True);
        Assert.That(handler.CanWallSlide, Is.True);
        Assert.That(handler.CanWallJump, Is.True);
        Assert.That(handler.TouchedWallThisFrame, Is.True);
        Assert.That(handler.LeftWallThisFrame, Is.False);
    }

    [Test]
    public void FinalizeFrame_ClearsWallContactWhenGrounded()
    {
        var handler = CreateHandler();

        handler.BeginFrame();
        handler.SetKinematicContext(Vector3.right, Vector3.down, isGrounded: false);
        handler.RegisterHit(CreateWallCollider(), Vector3.left, Vector3.zero);
        handler.FinalizeFrame(isGrounded: false);

        handler.BeginFrame();
        handler.SetKinematicContext(Vector3.right, Vector3.down, isGrounded: true);
        handler.FinalizeFrame(isGrounded: true);

        Assert.That(handler.HasWallContact, Is.False);
        Assert.That(handler.IsAttachedToWall, Is.False);
        Assert.That(handler.CanWallSlide, Is.False);
        Assert.That(handler.CanWallJump, Is.False);
        Assert.That(handler.LeftWallThisFrame, Is.True);
        Assert.That(handler.WallNormal, Is.EqualTo(Vector3.zero));
    }

    [Test]
    public void RegisterHit_IgnoresColliderWhenLayerDoesNotMatchWallLayer()
    {
        var handler = CreateHandler();
        var wallCollider = CreateWallCollider("Ignore Raycast");

        handler.BeginFrame();
        handler.SetKinematicContext(Vector3.right, Vector3.down, isGrounded: false);
        handler.RegisterHit(wallCollider, Vector3.left, Vector3.zero);
        handler.FinalizeFrame(isGrounded: false);

        Assert.That(handler.HasWallContact, Is.False);
        Assert.That(handler.CanWallJump, Is.False);
        Assert.That(handler.WallNormal, Is.EqualTo(Vector3.zero));
    }

    private WallEnvironmentHandler CreateHandler()
    {
        _playerObject = new GameObject("Player");
        var handler = _playerObject.AddComponent<WallEnvironmentHandler>();
        SetPrivateField(handler, "_wallLayerName", "Default");
        return handler;
    }

    private Collider CreateWallCollider(string layerName = "Default")
    {
        _wallObject = new GameObject("Wall");
        _wallObject.layer = LayerMask.NameToLayer(layerName);
        return _wallObject.AddComponent<BoxCollider>();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }
}
