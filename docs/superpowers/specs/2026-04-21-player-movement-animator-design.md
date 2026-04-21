# Player Movement Animator Design

## Goal

Create a Unity Animator Controller that matches the contract in `PlayerAnimationController.cs` while keeping the controller structure stable enough to swap animation clips later without redesigning the state machine.

## Scope

This design covers a single base movement controller for the player character with these gameplay-facing states:

- `Idle`
- `Locomotion`
- `Jump`
- `Fall`
- `Land`
- `Dash`
- `WallSlide`

This design does not add new gameplay code, new animator parameters, or new bespoke animation assets under `_Project`.

## Constraints

- The controller must match the existing parameter names defined in `PlayerAnimationController.AnimatorParameters`.
- The first implementation should use currently available clips from `Assets/Plugins/Threepeat/ParkourAnimations`.
- The controller structure should remain readable and reusable even if those plugin clips are replaced later.
- The implementation should update the existing `Assets/_Project/Data/Animations/Movement Animator.controller` asset instead of introducing a parallel controller.

## Recommended Approach

Use plugin clips now, but organize the controller around stable gameplay state names rather than clip names.

This is the middle-ground option:

- Faster than creating new project-owned clips
- Immediately playable in-editor
- Easier to migrate later because the logical state machine remains constant

## Animator Parameters

The controller must define exactly these parameters:

- `speed` as `float`
- `grounded` as `bool`
- `verticalVelocity` as `float`
- `jump` as `trigger`
- `land` as `trigger`
- `dash` as `trigger`
- `wallSlide` as `trigger`
- `wallSlideActive` as `bool`

## State Design

### Base Layer

Use a single `Base Layer`.

### States

- `Idle`
  - Default grounded resting state
  - Uses a plugin idle clip such as `parkour-idle`
- `Locomotion`
  - Ground movement loop
  - Uses a plugin run or sprint loop such as `sprint-loop-smooth`
- `Jump`
  - Rising or jump-start air state
  - Uses a low-velocity airborne clip such as `midair-lowvel`
- `Fall`
  - Descending air state
  - Uses a falling clip such as `midair-fallingdown`
- `Land`
  - Short landing recovery state
  - Uses a landing clip such as `landing-hard1-slower-fallfwd-to-idle`
- `Dash`
  - Short dash burst state
  - Uses the closest matching forward explosive movement clip available from the plugin set
  - If no perfect dash clip exists, prefer a short forward sprint-to-action clip over leaving the state empty
- `WallSlide`
  - Wall-contact airborne state
  - Uses the closest matching wall-contact clip such as `sprint-to-wall-plant-front` or another wall plant clip that visually reads as contact with a wall

## Transition Design

### Grounded Loop

- `Idle -> Locomotion`
  - Condition: `speed` greater than a small threshold
- `Locomotion -> Idle`
  - Condition: `speed` less than or equal to a small threshold

### Air Entry

- `Any State -> Jump`
  - Condition: `jump` trigger
  - Purpose: respond immediately when jump begins
- `Jump -> Fall`
  - Condition: `verticalVelocity` below zero
- `Idle -> Fall`
  - Condition: `grounded` is false and `verticalVelocity` below zero
- `Locomotion -> Fall`
  - Condition: `grounded` is false and `verticalVelocity` below zero

### Landing

- `Any State -> Land`
  - Condition: `land` trigger
  - Transition should not allow self-transition noise
- `Land -> Idle`
  - Condition: exit time completes and `speed` is near zero
- `Land -> Locomotion`
  - Condition: exit time completes and `speed` is above the locomotion threshold

### Dash

- `Any State -> Dash`
  - Condition: `dash` trigger
  - Dash should have higher priority than normal locomotion transitions
- `Dash -> Locomotion`
  - Condition: exit time completes and `grounded` is true and `speed` is above threshold
- `Dash -> Idle`
  - Condition: exit time completes and `grounded` is true and `speed` is near zero
- `Dash -> Fall`
  - Condition: `grounded` is false after dash

### Wall Slide

- `Any State -> WallSlide`
  - Condition: `wallSlide` trigger
- `WallSlide -> Fall`
  - Condition: `wallSlideActive` is false and `grounded` is false
- `WallSlide -> Land`
  - Condition: `land` trigger

### Ground Recovery

- `Jump -> Idle`
  - Condition: `grounded` is true and `speed` is near zero
- `Jump -> Locomotion`
  - Condition: `grounded` is true and `speed` is above threshold
- `Fall -> Land`
  - Condition: `land` trigger

## Clip Mapping Policy

The first controller version may use approximate plugin clips when there is no perfect semantic match for dash or wall slide.

The important contract is:

- parameter names stay fixed
- state names stay fixed
- transition logic stays gameplay-oriented

That allows later replacement of only the motions without redesigning the controller graph.

## Error Handling And Fallbacks

- If a chosen clip turns out to import incorrectly, replace only that state's motion and keep the same state and transitions.
- If a dash-specific clip does not exist, keep the `Dash` state with the best available placeholder motion instead of removing the state.
- If the wall clip feels too action-heavy, keep the `WallSlide` logic and swap only the motion later.

## Verification

The implementation is considered correct when:

- `Movement Animator.controller` contains all required parameters
- the base layer has the seven planned states
- the default state is `Idle`
- the controller contains transitions for jump, fall, land, dash, and wall slide behaviors
- the asset remains valid Unity YAML and imports without controller corruption

## Out Of Scope

- Blend trees
- upper-body layers
- animation events
- root-motion tuning
- project-specific replacement clips under `_Project`
- gameplay code changes in `PlayerAnimationController.cs`
