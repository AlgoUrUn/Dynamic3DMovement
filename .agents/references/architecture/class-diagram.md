classDiagram

%% =========================
%% Core Input & Context
%% =========================
class PlayerInputReader {
    +Vector2 moveInput
    +Vector2 lookInput
    +bool jumpPressed
    +bool dashPressed
    +bool runHeld
    +ReadInput()
    +GetMoveInput()
    +GetLookInput()
    +IsJumpPressed()
    +IsDashPressed()
    +IsRunHeld()
    +ClearOneShotInputs()
}

class PlayerContext {
    +PlayerFrameInput FrameInput
    +Vector2 MoveInput
    +Vector2 LookInput
    +Vector3 MoveDirection
    +bool JumpPressed
    +bool DashPressed
    +bool JumpRequested
    +bool DashRequested
    +Vector3 DashDirection
    +bool RunHeld
    +SetFrameInput(PlayerFrameInput frameInput)
    +ClearFrameInput()
}

class PlayerFrameInput {
    +Vector2 MoveInput
    +Vector2 LookInput
    +bool JumpPressed
    +bool DashPressed
    +bool RunHeld
    +ClearFrameInput()
}

class PlayerCharacterController {
    +KinematicCharacterMotor Motor
    +PlayerContext Context
    +float MoveSpeed
    +float JumpSpeed
    +float Gravity
    +SetInputs(PlayerContext context)
    +UpdateVelocity(ref Vector3 velocity, float deltaTime)
    +UpdateRotation(ref Quaternion rotation, float deltaTime)
}

%% =========================
%% State Machine Core
%% =========================
class PlayerStateMachine {
    +PlayerInputReader inputReader
    +LocomotionStateMachine locomotionSM
    +ActionStateMachine actionSM
    +KinematicCharacterMotor motor
    +PlayerContext context
    +StaminaManager staminaManager
    +PlayerAnimationController animationController
    +Tick(float deltaTime)
    +FixedTick(float deltaTime)
    +UpdateContext()
    +UpdateStates()
    +SendIntentToMotor()
}

%% =========================
%% Locomotion State Machine
%% =========================
class LocomotionStateMachine {
    +LocomotionState currentState
    +ChangeState()
    +Tick()
    +FixedTick()
}

class RootLocomotionState {
    +SubStateMachine subStateMachine
    +Enter()
    +Exit()
    +Tick()
    +FixedTick()
    +CheckTransition()
}

class GroundedState
class AirborneState
class WallState

class SubStateMachine {
    +LocomotionSubState currentSubState
    +ChangeSubState()
    +Tick()
    +FixedTick()
}

class IdleState
class MoveState
class JumpState
class FallState
class WallSlideState

%% =========================
%% Action State Machine
%% =========================
class ActionStateMachine {
    +ActionState currentState
    +ChangeState()
    +Tick()
    +FixedTick()
}

class NoneActionState
class DashState
class WallJumpState

%% =========================
%% Environment & Systems
%% =========================
class GroundDetector {
    +Transform origin
    +float checkDistance
    +LayerMask groundMask
    +float maxSlopeAngle
    +CheckGround()
    +GetGroundNormal()
    +GetGroundPoint()
    +IsStableGround()
}

class WallEnvironmentHandler {
    +Transform origin
    +float checkDistance
    +LayerMask wallMask
    +float minWallAngle
    +CheckWall()
    +GetWallNormal()
    +CanWallSlide(Vector3 moveDirection)
    +CanWallJump()
}

class StaminaManager {
    +float maxStamina
    +float currentStamina
    +float recoveryRate
    +float dashCost
    +float wallJumpCost
    +Tick(float deltaTime)
    +CanUseDash()
    +CanUseWallJump()
    +ConsumeDash()
    +ConsumeWallJump()
    +Recover(float deltaTime)
    +GetCurrentStamina()
}

class PlayerAnimationController {
    +Animator animator
    +UpdateLocomotion(float speed, bool grounded)
    +UpdateVerticalVelocity(float verticalVelocity)
    +PlayJump()
    +PlayLand()
    +PlayDash()
    +PlayWallSlide(bool active)
}

%% =========================
%% Relationships
%% =========================
PlayerStateMachine --> PlayerInputReader
PlayerStateMachine --> LocomotionStateMachine
PlayerStateMachine --> ActionStateMachine
PlayerStateMachine --> PlayerContext
PlayerStateMachine --> StaminaManager
PlayerStateMachine --> PlayerAnimationController

PlayerInputReader --> PlayerContext
PlayerContext --> PlayerFrameInput
PlayerCharacterController --> PlayerContext

LocomotionStateMachine --> RootLocomotionState
RootLocomotionState --> SubStateMachine

RootLocomotionState <|-- GroundedState
RootLocomotionState <|-- AirborneState
RootLocomotionState <|-- WallState

SubStateMachine --> IdleState
SubStateMachine --> MoveState
SubStateMachine --> JumpState
SubStateMachine --> FallState
SubStateMachine --> WallSlideState

ActionStateMachine --> NoneActionState
ActionStateMachine --> DashState
ActionStateMachine --> WallJumpState

PlayerStateMachine --> GroundDetector
PlayerStateMachine --> WallEnvironmentHandler
