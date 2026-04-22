#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MovementAnimatorControllerBuilder
{
    private const string ControllerPath = "Assets/_Project/Data/Animations/Movement Animator.controller";
    private const float LocomotionThreshold = 0.1f;
    private const string SpeedParameter = "speed";
    private const string GroundedParameter = "grounded";
    private const string VerticalVelocityParameter = "verticalVelocity";
    private const string JumpTrigger = "jump";
    private const string LandTrigger = "land";
    private const string DashTrigger = "dash";
    private const string WallSlideTrigger = "wallSlide";
    private const string WallSlideActiveParameter = "wallSlideActive";

    [MenuItem("Tools/Animation/Rebuild Movement Animator Controller")]
    public static void Rebuild()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        EnsureSingleBaseLayer(controller);
        RebuildParameters(controller);
        RebuildStateMachine(controller.layers[0].stateMachine);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureSingleBaseLayer(AnimatorController controller)
    {
        while (controller.layers.Length > 1)
        {
            controller.RemoveLayer(controller.layers.Length - 1);
        }

        if (controller.layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
        }

        var layer = controller.layers[0];
        layer.name = "Base Layer";
        layer.defaultWeight = 1f;
        controller.layers = new[] { layer };
    }

    private static void RebuildParameters(AnimatorController controller)
    {
        for (int i = controller.parameters.Length - 1; i >= 0; i--)
        {
            controller.RemoveParameter(i);
        }

        controller.AddParameter(SpeedParameter, AnimatorControllerParameterType.Float);
        controller.AddParameter(GroundedParameter, AnimatorControllerParameterType.Bool);
        controller.AddParameter(VerticalVelocityParameter, AnimatorControllerParameterType.Float);
        controller.AddParameter(JumpTrigger, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(LandTrigger, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(DashTrigger, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(WallSlideTrigger, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(WallSlideActiveParameter, AnimatorControllerParameterType.Bool);
    }

    private static void RebuildStateMachine(AnimatorStateMachine stateMachine)
    {
        ClearStateMachine(stateMachine);

        var idle = AddState(stateMachine, "Idle", new Vector3(260f, 40f, 0f), "parkour-idle");
        var locomotion = AddState(stateMachine, "Locomotion", new Vector3(520f, 40f, 0f), "sprint-loop-smooth");
        var jump = AddState(stateMachine, "Jump", new Vector3(260f, 220f, 0f), "midair-lowvel");
        var fall = AddState(stateMachine, "Fall", new Vector3(520f, 220f, 0f), "midair-fallingdown");
        var land = AddState(stateMachine, "Land", new Vector3(780f, 40f, 0f), "landing-hard1-slower-fallfwd-to-idle");
        var dash = AddState(stateMachine, "Dash", new Vector3(780f, 220f, 0f), "stop-lowplatform_sprint-to-idle");
        var wallSlide = AddState(stateMachine, "WallSlide", new Vector3(1040f, 220f, 0f), "sprint-to-wall-plant-front");

        stateMachine.defaultState = idle;

        AddLocomotionTransitions(idle, locomotion);
        AddAirTransitions(stateMachine, idle, locomotion, jump, fall);
        AddLandTransitions(stateMachine, land, idle, locomotion);
        AddDashTransitions(stateMachine, dash, idle, locomotion, fall);
        AddWallSlideTransitions(stateMachine, wallSlide, fall, land);
        AddGroundRecoveryTransitions(jump, idle, locomotion);
    }

    private static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        foreach (var transition in stateMachine.anyStateTransitions.ToArray())
        {
            stateMachine.RemoveAnyStateTransition(transition);
        }

        foreach (var childState in stateMachine.states.ToArray())
        {
            var state = childState.state;
            foreach (var transition in state.transitions.ToArray())
            {
                state.RemoveTransition(transition);
            }

            state.motion = null;
            stateMachine.RemoveState(state);
        }

        foreach (var childMachine in stateMachine.stateMachines.ToArray())
        {
            stateMachine.RemoveStateMachine(childMachine.stateMachine);
        }
    }

    private static AnimatorState AddState(AnimatorStateMachine stateMachine, string name, Vector3 position, string clipName)
    {
        var state = stateMachine.AddState(name, position);
        state.motion = LoadClip(clipName);
        state.writeDefaultValues = true;
        return state;
    }

    private static void AddLocomotionTransitions(AnimatorState idle, AnimatorState locomotion)
    {
        var toLocomotion = idle.AddTransition(locomotion);
        ConfigureInstantTransition(toLocomotion);
        toLocomotion.AddCondition(AnimatorConditionMode.Greater, LocomotionThreshold, SpeedParameter);

        var toIdle = locomotion.AddTransition(idle);
        ConfigureInstantTransition(toIdle);
        toIdle.AddCondition(AnimatorConditionMode.Less, LocomotionThreshold, SpeedParameter);
    }

    private static void AddAirTransitions(AnimatorStateMachine stateMachine, AnimatorState idle, AnimatorState locomotion, AnimatorState jump, AnimatorState fall)
    {
        AddAnyStateTrigger(stateMachine, jump, JumpTrigger);

        var jumpToFall = jump.AddTransition(fall);
        ConfigureInstantTransition(jumpToFall);
        jumpToFall.AddCondition(AnimatorConditionMode.Less, 0f, VerticalVelocityParameter);

        AddUngroundedFallTransition(idle, fall);
        AddUngroundedFallTransition(locomotion, fall);
    }

    private static void AddLandTransitions(AnimatorStateMachine stateMachine, AnimatorState land, AnimatorState idle, AnimatorState locomotion)
    {
        AddAnyStateTrigger(stateMachine, land, LandTrigger);

        var toIdle = land.AddTransition(idle);
        ConfigureExitTransition(toIdle, 0.85f);
        toIdle.AddCondition(AnimatorConditionMode.Less, LocomotionThreshold, SpeedParameter);

        var toLocomotion = land.AddTransition(locomotion);
        ConfigureExitTransition(toLocomotion, 0.85f);
        toLocomotion.AddCondition(AnimatorConditionMode.Greater, LocomotionThreshold, SpeedParameter);
    }

    private static void AddDashTransitions(AnimatorStateMachine stateMachine, AnimatorState dash, AnimatorState idle, AnimatorState locomotion, AnimatorState fall)
    {
        AddAnyStateTrigger(stateMachine, dash, DashTrigger);

        var toLocomotion = dash.AddTransition(locomotion);
        ConfigureExitTransition(toLocomotion, 0.8f);
        toLocomotion.AddCondition(AnimatorConditionMode.If, 0f, GroundedParameter);
        toLocomotion.AddCondition(AnimatorConditionMode.Greater, LocomotionThreshold, SpeedParameter);

        var toIdle = dash.AddTransition(idle);
        ConfigureExitTransition(toIdle, 0.8f);
        toIdle.AddCondition(AnimatorConditionMode.If, 0f, GroundedParameter);
        toIdle.AddCondition(AnimatorConditionMode.Less, LocomotionThreshold, SpeedParameter);

        var toFall = dash.AddTransition(fall);
        ConfigureInstantTransition(toFall);
        toFall.AddCondition(AnimatorConditionMode.IfNot, 0f, GroundedParameter);
    }

    private static void AddWallSlideTransitions(AnimatorStateMachine stateMachine, AnimatorState wallSlide, AnimatorState fall, AnimatorState land)
    {
        AddAnyStateTrigger(stateMachine, wallSlide, WallSlideTrigger);

        var toFall = wallSlide.AddTransition(fall);
        ConfigureInstantTransition(toFall);
        toFall.AddCondition(AnimatorConditionMode.IfNot, 0f, WallSlideActiveParameter);
        toFall.AddCondition(AnimatorConditionMode.IfNot, 0f, GroundedParameter);

        var toLand = wallSlide.AddTransition(land);
        ConfigureInstantTransition(toLand);
        toLand.AddCondition(AnimatorConditionMode.If, 0f, GroundedParameter);
    }

    private static void AddGroundRecoveryTransitions(AnimatorState jump, AnimatorState idle, AnimatorState locomotion)
    {
        var toIdle = jump.AddTransition(idle);
        ConfigureInstantTransition(toIdle);
        toIdle.AddCondition(AnimatorConditionMode.If, 0f, GroundedParameter);
        toIdle.AddCondition(AnimatorConditionMode.Less, LocomotionThreshold, SpeedParameter);

        var toLocomotion = jump.AddTransition(locomotion);
        ConfigureInstantTransition(toLocomotion);
        toLocomotion.AddCondition(AnimatorConditionMode.If, 0f, GroundedParameter);
        toLocomotion.AddCondition(AnimatorConditionMode.Greater, LocomotionThreshold, SpeedParameter);
    }

    private static void AddUngroundedFallTransition(AnimatorState from, AnimatorState fall)
    {
        var transition = from.AddTransition(fall);
        ConfigureInstantTransition(transition);
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, GroundedParameter);
        transition.AddCondition(AnimatorConditionMode.Less, 0f, VerticalVelocityParameter);
    }

    private static void AddAnyStateTrigger(AnimatorStateMachine stateMachine, AnimatorState destination, string triggerName)
    {
        var transition = stateMachine.AddAnyStateTransition(destination);
        ConfigureInstantTransition(transition);
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
    }

    private static void ConfigureInstantTransition(AnimatorStateTransition transition)
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0f;
        transition.offset = 0f;
    }

    private static void ConfigureExitTransition(AnimatorStateTransition transition, float exitTime)
    {
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.hasFixedDuration = true;
        transition.duration = 0.05f;
        transition.offset = 0f;
    }

    private static AnimationClip LoadClip(string clipName)
    {
        var guids = AssetDatabase.FindAssets($"{clipName} t:AnimationClip");
        var path = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(candidate => candidate.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) || candidate.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.Contains("/Plugins/Threepeat/") ? 0 : 1)
            .ThenBy(candidate => candidate.Contains("/IP-") || candidate.Contains("\\IP-") ? 1 : 0)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException($"Could not find animation clip '{clipName}'.");
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            throw new InvalidOperationException($"Could not load animation clip at '{path}'.");
        }

        return clip;
    }
}
#endif
