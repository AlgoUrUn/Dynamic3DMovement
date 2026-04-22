using NUnit.Framework;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;

public sealed class PlayerAnimationControllerTests
{
    private const string ControllerPath = "Assets/_Project/Data/Animations/Movement Animator.controller";

    private static readonly (string Name, AnimatorControllerParameterType Type)[] ExpectedParameters =
    {
        ("speed", AnimatorControllerParameterType.Float),
        ("grounded", AnimatorControllerParameterType.Bool),
        ("verticalVelocity", AnimatorControllerParameterType.Float),
        ("jump", AnimatorControllerParameterType.Trigger),
        ("land", AnimatorControllerParameterType.Trigger),
        ("dash", AnimatorControllerParameterType.Trigger),
        ("wallSlide", AnimatorControllerParameterType.Trigger),
        ("wallSlideActive", AnimatorControllerParameterType.Bool),
    };

    private static readonly string[] ExpectedBaseStateNames =
    {
        "Idle",
        "Locomotion",
        "Jump",
        "Fall",
        "Land",
        "Dash",
        "WallSlide",
    };

    [Test]
    public void MovementAnimatorController_ParametersMatchExpectedSignature()
    {
        var controller = LoadController();

        Assert.That(controller.parameters, Has.Length.EqualTo(ExpectedParameters.Length));

        for (var index = 0; index < ExpectedParameters.Length; index++)
        {
            Assert.That(controller.parameters[index].name, Is.EqualTo(ExpectedParameters[index].Name));
            Assert.That(controller.parameters[index].type, Is.EqualTo(ExpectedParameters[index].Type));
        }
    }

    [Test]
    public void MovementAnimatorController_BaseStatesMatchExpectedNames()
    {
        var controller = LoadController();
        var actualBaseStateNames = GetBaseStateNames(controller);

        Assert.That(actualBaseStateNames, Has.Length.EqualTo(ExpectedBaseStateNames.Length));
        Assert.That(actualBaseStateNames, Is.EquivalentTo(ExpectedBaseStateNames));
    }

    [Test]
    public void MovementAnimatorController_DefaultStateIsIdle()
    {
        var controller = LoadController();

        Assert.That(controller.layers[0].stateMachine.defaultState, Is.Not.Null);
        Assert.That(controller.layers[0].stateMachine.defaultState.name, Is.EqualTo("Idle"));
    }

    [Test]
    public void MovementAnimatorController_AnyStateTransitionsUseExpectedTriggerParameters()
    {
        var controller = LoadController();

        Assert.That(HasAnyStateTriggerTransition(controller, "Jump", "jump"), Is.True);
        Assert.That(HasAnyStateTriggerTransition(controller, "Land", "land"), Is.True);
        Assert.That(HasAnyStateTriggerTransition(controller, "Dash", "dash"), Is.True);
        Assert.That(HasAnyStateTriggerTransition(controller, "WallSlide", "wallSlide"), Is.True);
    }

    private static AnimatorController LoadController()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        Assert.That(controller, Is.Not.Null, $"Missing animator controller at '{ControllerPath}'.");

        return controller;
    }

    private static string[] GetBaseStateNames(AnimatorController controller)
    {
        return controller.layers[0].stateMachine.states
            .Select(state => state.state.name)
            .ToArray();
    }

    private static bool HasAnyStateTriggerTransition(
        AnimatorController controller,
        string expectedStateName,
        string expectedTriggerParameter)
    {
        return controller.layers[0].stateMachine.anyStateTransitions.Any(transition =>
            transition.destinationState != null
            && transition.destinationState.name == expectedStateName
            && transition.conditions.Any(condition =>
                condition.parameter == expectedTriggerParameter
                && condition.mode == AnimatorConditionMode.If));
    }
}
#endif
