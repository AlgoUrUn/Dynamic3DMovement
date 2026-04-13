using System.Reflection;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class CameraPointerTests
{
    private GameObject _playerObject;
    private GameObject _bodyObject;
    private GameObject _cameraPivotObject;
    private GameObject _headObject;

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

    [Test]
    public void ApplyRotation_RotatesBodyOnYawOnly()
    {
        var pointer = CreatePointer();

        SetPrivateField(pointer, "_yaw", 45f);
        SetPrivateField(pointer, "_pitch", 20f);
        InvokePrivateMethod(pointer, "ApplyRotation");

        Assert.That(_bodyObject.transform.eulerAngles.y, Is.EqualTo(45f).Within(0.0001f));
        Assert.That(NormalizeAngle(_bodyObject.transform.eulerAngles.x), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(NormalizeAngle(_cameraPivotObject.transform.localEulerAngles.x), Is.EqualTo(20f).Within(0.0001f));
    }

    [Test]
    public void ApplyRotation_ClampsHeadLookToThirtyDegreesPerAxis()
    {
        var pointer = CreatePointer();

        SetPrivateField(pointer, "_yaw", 90f);
        SetPrivateField(pointer, "_pitch", 80f);
        InvokePrivateMethod(pointer, "ApplyRotation");

        Vector3 headAngles = _headObject.transform.localEulerAngles;

        Assert.That(NormalizeAngle(headAngles.x), Is.EqualTo(30f).Within(0.0001f));
        Assert.That(NormalizeAngle(headAngles.y), Is.EqualTo(0f).Within(0.0001f));
    }

    private CameraPointer CreatePointer()
    {
        _playerObject = new GameObject("Player");
        _bodyObject = new GameObject("Body");
        _cameraPivotObject = new GameObject("CameraPivot");
        _headObject = new GameObject("Head");

        _bodyObject.transform.SetParent(_playerObject.transform);
        _cameraPivotObject.transform.SetParent(_bodyObject.transform);
        _headObject.transform.SetParent(_bodyObject.transform);

        var context = _playerObject.AddComponent<PlayerContext>();
        var pointer = _playerObject.AddComponent<CameraPointer>();

        SetPrivateField(pointer, "_playerContext", context);
        SetPrivateField(pointer, "_yawTarget", _bodyObject.transform);
        SetPrivateField(pointer, "_pitchTarget", _cameraPivotObject.transform);
        SetPrivateField(pointer, "_headTarget", _headObject.transform);
        SetPrivateField(pointer, "_lockCursor", false);

        InvokePrivateMethod(pointer, "ResolveReferences");

        return pointer;
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(target, null);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }

    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
