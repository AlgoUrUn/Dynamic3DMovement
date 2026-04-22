# Player Movement Animator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Update `Assets/_Project/Data/Animations/Movement Animator.controller` so it matches `PlayerAnimationController.cs` and provides a stable `Idle/Locomotion/Jump/Fall/Land/Dash/WallSlide` structure using currently available plugin clips.

**Architecture:** Keep one `Base Layer` with fixed gameplay-facing state names and parameter names, then attach the best available motions from `Assets/Plugins/Threepeat/ParkourAnimations`. Guard the work with editor/playmode tests so the Animator contract stays stable even if motions are swapped later.

**Tech Stack:** Unity 6.0.3, Animator Controller YAML assets, UnityEditor.Animations, NUnit PlayMode tests

---

## File Structure

- Modify: `Assets/_Project/Data/Animations/Movement Animator.controller`
  Responsibility: project-owned runtime Animator Controller asset with stable states, parameters, and transitions.
- Modify: `Assets/_Project/Tests/PlayMode/PlayerAnimationControllerTests.cs`
  Responsibility: verify the runtime animation contract and editor-created controller structure.
- Create: `Assets/_Project/Editor/Animation/MovementAnimatorControllerBuilder.cs`
  Responsibility: deterministic editor utility that rebuilds `Movement Animator.controller` using exact parameters, states, motions, and transitions.

### Task 1: Lock The Contract With Tests

**Files:**
- Modify: `Assets/_Project/Tests/PlayMode/PlayerAnimationControllerTests.cs`
- Test: `Assets/_Project/Tests/PlayMode/PlayerAnimationControllerTests.cs`

- [ ] **Step 1: Write the failing structure tests**

Add editor-only tests near the end of `PlayerAnimationControllerTests.cs`:

```csharp
#if UNITY_EDITOR
    [Test]
    public void MovementAnimatorController_DefinesExpectedParameters()
    {
        var controller = LoadProjectMovementAnimator();

        CollectionAssert.AreEquivalent(
            new[]
            {
                (PlayerAnimationController.AnimatorParameters.LocomotionSpeed, AnimatorControllerParameterType.Float),
                (PlayerAnimationController.AnimatorParameters.IsGrounded, AnimatorControllerParameterType.Bool),
                (PlayerAnimationController.AnimatorParameters.VerticalVelocity, AnimatorControllerParameterType.Float),
                (PlayerAnimationController.AnimatorParameters.JumpTrigger, AnimatorControllerParameterType.Trigger),
                (PlayerAnimationController.AnimatorParameters.LandTrigger, AnimatorControllerParameterType.Trigger),
                (PlayerAnimationController.AnimatorParameters.DashTrigger, AnimatorControllerParameterType.Trigger),
                (PlayerAnimationController.AnimatorParameters.WallSlideTrigger, AnimatorControllerParameterType.Trigger),
                (PlayerAnimationController.AnimatorParameters.WallSlideActive, AnimatorControllerParameterType.Bool),
            },
            controller.parameters.Select(parameter => (parameter.name, parameter.type)).ToArray());
    }

    [Test]
    public void MovementAnimatorController_ContainsExpectedBaseStates()
    {
        var controller = LoadProjectMovementAnimator();
        var stateMachine = controller.layers[0].stateMachine;
        var stateNames = stateMachine.states.Select(child => child.state.name).ToArray();

        CollectionAssert.AreEquivalent(
            new[] { "Idle", "Locomotion", "Jump", "Fall", "Land", "Dash", "WallSlide" },
            stateNames);

        Assert.That(stateMachine.defaultState.name, Is.EqualTo("Idle"));
    }

    [Test]
    public void MovementAnimatorController_ContainsExpectedTransitionEntrypoints()
    {
        var controller = LoadProjectMovementAnimator();
        var stateMachine = controller.layers[0].stateMachine;

        Assert.That(HasAnyStateTriggerTransition(stateMachine, "Jump", PlayerAnimationController.AnimatorParameters.JumpTrigger), Is.True);
        Assert.That(HasAnyStateTriggerTransition(stateMachine, "Land", PlayerAnimationController.AnimatorParameters.LandTrigger), Is.True);
        Assert.That(HasAnyStateTriggerTransition(stateMachine, "Dash", PlayerAnimationController.AnimatorParameters.DashTrigger), Is.True);
        Assert.That(HasAnyStateTriggerTransition(stateMachine, "WallSlide", PlayerAnimationController.AnimatorParameters.WallSlideTrigger), Is.True);
    }
#endif
```

- [ ] **Step 2: Add the editor-only helpers used by the new tests**

Add these helpers inside `PlayerAnimationControllerTests.cs`:

```csharp
#if UNITY_EDITOR
    private const string ProjectMovementAnimatorPath = "Assets/_Project/Data/Animations/Movement Animator.controller";

    private static AnimatorController LoadProjectMovementAnimator()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ProjectMovementAnimatorPath);
        Assert.That(controller, Is.Not.Null, $"Missing animator controller at {ProjectMovementAnimatorPath}");
        return controller;
    }

    private static bool HasAnyStateTriggerTransition(AnimatorStateMachine stateMachine, string destinationStateName, string triggerParameter)
    {
        return stateMachine.anyStateTransitions.Any(transition =>
            transition.destinationState != null &&
            transition.destinationState.name == destinationStateName &&
            transition.conditions.Any(condition =>
                condition.mode == AnimatorConditionMode.If &&
                condition.parameter == triggerParameter));
    }
#endif
```

- [ ] **Step 3: Add required namespace imports**

Update the file header so the tests compile:

```csharp
using System.Linq;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif
```

- [ ] **Step 4: Run the new tests to verify RED**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement' -runTests -testPlatform PlayMode -testFilter PlayerAnimationControllerTests.MovementAnimatorController_ -testResults 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement\playmode-test-results.xml' -logFile 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement\playmode-test.log' -quit
```

Expected: FAIL because the current controller is empty and does not define the required parameters, states, or transitions.

- [ ] **Step 5: Commit the red test checkpoint**

```bash
git add Assets/_Project/Tests/PlayMode/PlayerAnimationControllerTests.cs
git commit -m "test: lock movement animator controller contract"
```

### Task 2: Add A Deterministic Controller Builder

**Files:**
- Create: `Assets/_Project/Editor/Animation/MovementAnimatorControllerBuilder.cs`
- Test: `Assets/_Project/Tests/PlayMode/PlayerAnimationControllerTests.cs`

- [ ] **Step 1: Write the editor builder utility**

Create `Assets/_Project/Editor/Animation/MovementAnimatorControllerBuilder.cs`:

```csharp
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MovementAnimatorControllerBuilder
{
    public const string ControllerPath = "Assets/_Project/Data/Animations/Movement Animator.controller";

    [MenuItem("Tools/Animation/Rebuild Movement Animator Controller")]
    public static void Rebuild()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        controller.RemoveLayer(0);
        controller.AddLayer("Base Layer");

        var layer = controller.layers[0];
        layer.defaultWeight = 1f;
        var stateMachine = layer.stateMachine;

        ClearParameters(controller);
        AddParameters(controller);

        var idle = stateMachine.AddState("Idle");
        var locomotion = stateMachine.AddState("Locomotion");
        var jump = stateMachine.AddState("Jump");
        var fall = stateMachine.AddState("Fall");
        var land = stateMachine.AddState("Land");
        var dash = stateMachine.AddState("Dash");
        var wallSlide = stateMachine.AddState("WallSlide");
        stateMachine.defaultState = idle;

        AssignMotions(idle, locomotion, jump, fall, land, dash, wallSlide);
        AddTransitions(stateMachine, idle, locomotion, jump, fall, land, dash, wallSlide);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ClearParameters(AnimatorController controller)
    {
        foreach (var parameter in controller.parameters.ToArray())
        {
            controller.RemoveParameter(parameter);
        }
    }

    private static void AddParameters(AnimatorController controller)
    {
        controller.AddParameter(PlayerAnimationController.AnimatorParameters.LocomotionSpeed, AnimatorControllerParameterType.Float);
        controller.AddParameter(PlayerAnimationController.AnimatorParameters.IsGrounded, AnimatorControllerParameterType.Bool);
        controller.AddParameter(PlayerAnimationController.AnimatorParameters.VerticalVelocity, AnimatorControllerParameterType.Float);
        controller.AddParameter(PlayerAnimationController.AnimatorParameters.JumpTrigger, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(PlayerAnimationController.AnimatorParameters.LandTrigger, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(PlayerAnimationController.AnimatorParameters.DashTrigger, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(PlayerAnimationController.AnimatorParameters.WallSlideTrigger, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(PlayerAnimationController.AnimatorParameters.WallSlideActive, AnimatorControllerParameterType.Bool);
    }
}
#endif
```

- [ ] **Step 2: Implement motion lookup with stable semantic mapping**

Extend the same file with clip lookup and assignment helpers:

```csharp
    private static void AssignMotions(AnimatorState idle, AnimatorState locomotion, AnimatorState jump, AnimatorState fall, AnimatorState land, AnimatorState dash, AnimatorState wallSlide)
    {
        idle.motion = LoadClip("parkour-idle");
        locomotion.motion = LoadClip("sprint-loop-smooth");
        jump.motion = LoadClip("midair-lowvel");
        fall.motion = LoadClip("midair-fallingdown");
        land.motion = LoadClip("landing-hard1-slower-fallfwd-to-idle");
        dash.motion = LoadClip("stop-lowplatform_sprint-to-idle");
        wallSlide.motion = LoadClip("sprint-to-wall-plant-front");
    }

    private static Motion LoadClip(string clipName)
    {
        var guids = AssetDatabase.FindAssets($"{clipName} t:AnimationClip");
        var path = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(candidate => candidate.Contains("/Plugins/Threepeat/") ? 0 : 1)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(path))
        {
            throw new System.InvalidOperationException($"Could not find animation clip named '{clipName}'.");
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            throw new System.InvalidOperationException($"Could not load animation clip at '{path}'.");
        }

        return clip;
    }
```

- [ ] **Step 3: Implement transitions for the approved state graph**

Extend the same file with transition helpers:

```csharp
    private static void AddTransitions(AnimatorStateMachine stateMachine, AnimatorState idle, AnimatorState locomotion, AnimatorState jump, AnimatorState fall, AnimatorState land, AnimatorState dash, AnimatorState wallSlide)
    {
        AddSpeedTransition(idle, locomotion, AnimatorConditionMode.Greater, 0.1f);
        AddSpeedTransition(locomotion, idle, AnimatorConditionMode.Less, 0.1f);

        AddAnyStateTrigger(stateMachine, jump, PlayerAnimationController.AnimatorParameters.JumpTrigger);
        AddAnyStateTrigger(stateMachine, land, PlayerAnimationController.AnimatorParameters.LandTrigger);
        AddAnyStateTrigger(stateMachine, dash, PlayerAnimationController.AnimatorParameters.DashTrigger);
        AddAnyStateTrigger(stateMachine, wallSlide, PlayerAnimationController.AnimatorParameters.WallSlideTrigger);

        AddGroundExit(jump, idle, locomotion);
        AddGroundExit(land, idle, locomotion, true);
        AddGroundExit(dash, idle, locomotion, true);

        AddFallTransition(jump, fall);
        AddUngroundedFall(idle, fall);
        AddUngroundedFall(locomotion, fall);
        AddDashFall(dash, fall);
        AddWallSlideFall(wallSlide, fall);
    }
```

Then add helper methods matching those calls, keeping trigger transitions at `duration = 0f` and allowing exit time only for `Land` and `Dash` recovery.

- [ ] **Step 4: Run the builder once inside the editor**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement' -executeMethod MovementAnimatorControllerBuilder.Rebuild -logFile 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement\movement-animator-build.log' -quit
```

Expected: exit code `0`, and `Assets/_Project/Data/Animations/Movement Animator.controller` is rewritten with the approved parameter and state structure.

- [ ] **Step 5: Commit the builder utility**

```bash
git add Assets/_Project/Editor/Animation/MovementAnimatorControllerBuilder.cs Assets/_Project/Data/Animations/Movement\ Animator.controller
git commit -m "feat: add movement animator controller builder"
```

### Task 3: Verify Controller Integration End-To-End

**Files:**
- Modify: `Assets/_Project/Tests/PlayMode/PlayerAnimationControllerTests.cs`
- Modify: `Assets/_Project/Data/Animations/Movement Animator.controller`
- Test: `Assets/_Project/Tests/PlayMode/PlayerAnimationControllerTests.cs`

- [ ] **Step 1: Make the structure tests point at the rebuilt asset**

Keep the structure tests from Task 1 and ensure they load the project controller rather than a temp controller.

```csharp
    private static AnimatorController LoadProjectMovementAnimator()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ProjectMovementAnimatorPath);
        Assert.That(controller, Is.Not.Null);
        return controller;
    }
```

- [ ] **Step 2: Run the targeted structure tests to verify GREEN**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement' -runTests -testPlatform PlayMode -testFilter PlayerAnimationControllerTests.MovementAnimatorController_ -testResults 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement\playmode-test-results.xml' -logFile 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement\playmode-test.log' -quit
```

Expected: PASS for the structure tests added in Task 1.

- [ ] **Step 3: Run the existing animation behavior tests to verify no regression**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement' -runTests -testPlatform PlayMode -testFilter PlayerAnimationControllerTests.UpdateAnimation_ -testResults 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement\playmode-test-results.xml' -logFile 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement\playmode-test.log' -quit
```

Expected: PASS for the existing `UpdateAnimation_*` tests.

- [ ] **Step 4: Run the full playmode test suite**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement' -runTests -testPlatform PlayMode -testResults 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement\playmode-test-results.xml' -logFile 'C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement\playmode-test.log' -quit
```

Expected: PASS with zero failed PlayMode tests.

- [ ] **Step 5: Commit the verified controller integration**

```bash
git add Assets/_Project/Tests/PlayMode/PlayerAnimationControllerTests.cs Assets/_Project/Data/Animations/Movement\ Animator.controller
git commit -m "feat: build player movement animator controller"
```

## Self-Review

- Spec coverage:
  - Required parameters are covered in Task 1.
  - Stable seven-state base layer is covered in Tasks 1 and 2.
  - Plugin clip mapping is covered in Task 2.
  - Transition behavior is covered in Tasks 1 and 2.
  - Verification against current tests and full suite is covered in Task 3.
- Placeholder scan:
  - Removed vague steps and named exact files, tests, commands, and code snippets.
- Type consistency:
  - Parameter names match `PlayerAnimationController.AnimatorParameters`.
  - Controller path is consistent across tasks.
  - State names match the approved design doc.
