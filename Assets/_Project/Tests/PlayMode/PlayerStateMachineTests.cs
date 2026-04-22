using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public sealed class PlayerStateMachineTests : InputTestFixture
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
    public void UpdateContext_CopiesReaderInputIntoContext()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        PressInput(keyboard.dKey);
        PressInput(keyboard.spaceKey);

        var stateMachine = CreatePlayerStateMachine(out var context, out _, out _);

        stateMachine.UpdateContext();

        Assert.That(context.MoveInput, Is.EqualTo(Vector2.right));
        Assert.That(context.JumpPressed, Is.True);
    }

    [Test]
    public void UpdateStates_BindsContextToController()
    {
        var stateMachine = CreatePlayerStateMachine(out var context, out _, out var controller);

        context.SetFrameInput(new PlayerFrameInput(Vector2.left, Vector2.zero, false, false, false));
        stateMachine.UpdateStates();

        Assert.That(controller.Context, Is.EqualTo(context));
        Assert.That(controller.HasMoveInput(), Is.True);
    }

    [Test]
    public void SendIntentToMotor_RegistersControllerWithMotor()
    {
        var stateMachine = CreatePlayerStateMachine(out _, out _, out var controller);

        controller.Motor.CharacterController = null;
        stateMachine.SendIntentToMotor();

        Assert.That(controller.Motor.CharacterController, Is.EqualTo(controller));
    }

    [Test]
    public void ResolveReferences_FindsPlayerAnimationController()
    {
        var stateMachine = CreatePlayerStateMachine(out _, out _, out _);

        Assert.That(stateMachine.AnimationController, Is.Not.Null);
    }

    private PlayerStateMachine CreatePlayerStateMachine(
        out PlayerContext context,
        out PlayerInputReader inputReader,
        out PlayerCharacterController controller)
    {
        _playerObject = new GameObject("Player");
        _playerObject.AddComponent<CapsuleCollider>();
        _playerObject.AddComponent<KinematicCharacterController.KinematicCharacterMotor>();
        _playerObject.AddComponent<GroundDetector>();
        context = _playerObject.AddComponent<PlayerContext>();
        inputReader = _playerObject.AddComponent<PlayerInputReader>();
        controller = _playerObject.AddComponent<PlayerCharacterController>();
        _playerObject.AddComponent<PlayerAnimationController>();
        return _playerObject.AddComponent<PlayerStateMachine>();
    }

    private void PressInput(ButtonControl control)
    {
        Press(control);
    }
}
