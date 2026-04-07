using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public sealed class PlayerInputReaderTests : InputTestFixture
{
    private GameObject _playerObject;

    public override void TearDown()
    {
        if (_playerObject != null)
        {
            Object.DestroyImmediate(_playerObject);
        }

        base.TearDown();
    }

    [Test]
    public void RefreshInputContext_CopiesCurrentInputToContext()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        PressInput(keyboard.wKey);
        PressInput(keyboard.spaceKey);

        var context = CreatePlayerWithInputReader(out var reader);

        reader.RefreshInputContext();

        Assert.That(context.MoveInput, Is.EqualTo(Vector2.up));
        Assert.That(context.JumpPressed, Is.True);
    }

    [Test]
    public void ClearFrameInput_ClearsFrameScopedInputAndKeepsHeldInput()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        PressInput(keyboard.wKey);
        PressInput(keyboard.leftShiftKey);
        PressInput(keyboard.spaceKey);

        var context = CreatePlayerWithInputReader(out var reader);

        reader.RefreshInputContext();
        reader.ClearFrameInput();

        Assert.That(context.MoveInput, Is.EqualTo(Vector2.up));
        Assert.That(context.RunHeld, Is.True);
        Assert.That(context.LookInput, Is.EqualTo(Vector2.zero));
        Assert.That(context.JumpPressed, Is.False);
        Assert.That(context.DashPressed, Is.False);
    }

    private PlayerContext CreatePlayerWithInputReader(out PlayerInputReader reader)
    {
        _playerObject = new GameObject("Player");
        var context = _playerObject.AddComponent<PlayerContext>();
        reader = _playerObject.AddComponent<PlayerInputReader>();
        return context;
    }

    private void PressInput(ButtonControl control)
    {
        // Avoid a second InputSystem.Update because it would clear wasPressedThisFrame.
        Press(control);
    }
}
