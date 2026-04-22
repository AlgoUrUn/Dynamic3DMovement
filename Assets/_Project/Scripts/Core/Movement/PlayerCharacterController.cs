using KinematicCharacterController;
using UnityEngine;

public sealed class PlayerCharacterController : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor _motor;
    [SerializeField] private PlayerContext _context;
    [SerializeField] private GroundDetector _groundDetector;
    [SerializeField] private WallEnvironmentHandler _wallEnvironmentHandler;
    [SerializeField] private StaminaManager _staminaManager;
    [SerializeField] private float _moveSpeed = 6f;
    [SerializeField] private float _moveAccelerationDuration = 0.2f;
    [SerializeField] private AnimationCurve _moveAccelerationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float _moveDecelerationDuration = 0.2f;
    [SerializeField] private AnimationCurve _moveDecelerationCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [SerializeField] private float _jumpSpeed = 8f;
    [SerializeField] private float _dashSpeed = 14f;
    [SerializeField] private float _dashDuration = 0.18f;
    [SerializeField] private float _wallJumpHorizontalSpeed = 8f;
    [SerializeField] private float _wallJumpVerticalSpeed = 8f;
    [SerializeField] private float _wallJumpDuration = 0.16f;
    [SerializeField] private float _gravity = 30f;
    [SerializeField] private float _upwardGravityMultiplier = 0.75f;
    [SerializeField] private float _fallGravityMultiplier = 1.25f;
    [SerializeField] private float _wallSlideMaxFallSpeed = 3f;
    [SerializeField] private bool _logWallStateDebug;
    [SerializeField] private bool _logMovementHits;
    [SerializeField] private bool _logStateTransitions;

    public KinematicCharacterMotor Motor => _motor;
    public PlayerContext Context => _context;
    public GroundDetector GroundDetector => _groundDetector;
    public WallEnvironmentHandler WallEnvironmentHandler => _wallEnvironmentHandler;
    public StaminaManager StaminaManager => _staminaManager;
    public float MoveSpeed => _moveSpeed;
    public float JumpSpeed => _jumpSpeed;
    public float DashSpeed => _dashSpeed;
    public float DashDuration => _dashDuration;
    public float WallJumpHorizontalSpeed => _wallJumpHorizontalSpeed;
    public float WallJumpVerticalSpeed => _wallJumpVerticalSpeed;
    public float WallJumpDuration => _wallJumpDuration;
    public float Gravity => _gravity;
    public float UpwardGravityMultiplier => _upwardGravityMultiplier;
    public float FallGravityMultiplier => _fallGravityMultiplier;
    public float WallSlideMaxFallSpeed => _wallSlideMaxFallSpeed;
    public string CurrentLocomotionStateName => _locomotionStateMachine?.CurrentStateName;
    public string CurrentGroundedSubStateName => _locomotionStateMachine?.CurrentGroundedSubStateName;
    public string CurrentAirborneSubStateName => _locomotionStateMachine?.CurrentAirborneSubStateName;
    public string CurrentActionStateName => _actionStateMachine?.CurrentStateName;
    public float LastKnownVerticalVelocity { get; private set; }
    public bool HasWallContactNow => _wallEnvironmentHandler != null && _wallEnvironmentHandler.HasWallContact;
    public bool IsAttachedToWallNow => _wallEnvironmentHandler != null && _wallEnvironmentHandler.IsAttachedToWall;
    public Vector3 CurrentWallNormal => _wallEnvironmentHandler != null ? _wallEnvironmentHandler.WallNormal : Vector3.zero;
    public bool CanWallSlideNow => _wallEnvironmentHandler != null && _wallEnvironmentHandler.CanWallSlide;
    public bool CanWallJumpNow => _wallEnvironmentHandler != null && _wallEnvironmentHandler.CanWallJump;
    public float LastKnownPlanarSpeed { get; private set; }

    private LocomotionStateMachine _locomotionStateMachine;
    private ActionStateMachine _actionStateMachine;
    private float _currentMoveSpeedMultiplier;
    private float _moveBlendElapsed;
    private float _moveBlendDuration;
    private float _moveBlendStartMultiplier;
    private float _moveBlendTargetMultiplier;
    private bool _wasMoveInputActive;
    private Vector3 _lastGroundedMoveDirection = Vector3.forward;

    private void Awake()
    {
        // Cache once because the motor asks this controller for movement during its own update loop.
        ResolveReferences();
        RegisterWithMotor();
        InitializeStateMachine();
    }

    /// <summary>
    /// Allows the state machine to provide the context snapshot that should drive motor velocity.
    /// </summary>
    public void SetInputs(PlayerContext context)
    {
        _context = context;
    }

    public void UpdateContext(PlayerContext context)
    {
        SetInputs(context);
    }

    public void UpdateStates()
    {
        _locomotionStateMachine?.UpdateStates();
    }

    public void SendIntentToMotor()
    {
        RegisterWithMotor();
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        _locomotionStateMachine?.UpdateRotation(ref currentRotation, deltaTime);
    }

    /// <summary>
    /// Applies movement in the motor callback so KCC remains the single owner of character velocity.
    /// </summary>
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        UpdateMoveAcceleration(deltaTime);
        _locomotionStateMachine?.UpdateVelocity(ref currentVelocity, deltaTime);
        _actionStateMachine?.UpdateVelocity(ref currentVelocity, deltaTime);
        _wallEnvironmentHandler?.SetKinematicContext(GetMoveDirection(), currentVelocity, IsStableOnGround());
        LastKnownPlanarSpeed = new Vector2(currentVelocity.x, currentVelocity.z).magnitude;
        LastKnownVerticalVelocity = currentVelocity.y;
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _wallEnvironmentHandler?.BeginFrame();
        _locomotionStateMachine?.BeforeCharacterUpdate(deltaTime);
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        if (_groundDetector != null)
        {
            _groundDetector.RefreshFromMotor();
        }

        LogWallDebugState("PostGroundingUpdate");
        _locomotionStateMachine?.PostGroundingUpdate(deltaTime);
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        _wallEnvironmentHandler?.FinalizeFrame(IsStableOnGroundNow());
        _locomotionStateMachine?.AfterCharacterUpdate(deltaTime);
        _actionStateMachine?.AfterCharacterUpdate(deltaTime);
        LogWallDebugState("AfterCharacterUpdate");
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        // Collision filtering stays permissive until layer-based environment rules are defined.
        return true;
    }

    public void OnGroundHit(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        _wallEnvironmentHandler?.RegisterHit(hitCollider, hitNormal, hitPoint);
        _locomotionStateMachine?.OnGroundHit(
            hitCollider,
            hitNormal,
            hitPoint,
            ref hitStabilityReport);
    }

    public void OnMovementHit(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        if (_logMovementHits)
        {
            Debug.Log(
                $"[MovementHit] collider={hitCollider?.name ?? "None"}, layer={GetLayerName(hitCollider)}, normal={hitNormal}, point={hitPoint}, stable={hitStabilityReport.IsStable}",
                this);
        }

        _wallEnvironmentHandler?.RegisterHit(hitCollider, hitNormal, hitPoint);
        _locomotionStateMachine?.OnMovementHit(
            hitCollider,
            hitNormal,
            hitPoint,
            ref hitStabilityReport);
    }

    public void ProcessHitStabilityReport(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        Vector3 atCharacterPosition,
        Quaternion atCharacterRotation,
        ref HitStabilityReport hitStabilityReport)
    {
        _wallEnvironmentHandler?.RegisterHit(hitCollider, hitNormal, hitPoint);

        if (_wallEnvironmentHandler != null &&
            _wallEnvironmentHandler.IsWallHit(hitCollider, hitNormal))
        {
            hitStabilityReport.IsStable = false;
            hitStabilityReport.ValidStepDetected = false;
        }

        _locomotionStateMachine?.ProcessHitStabilityReport(
            hitCollider,
            hitNormal,
            hitPoint,
            atCharacterPosition,
            atCharacterRotation,
            ref hitStabilityReport);
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
        _locomotionStateMachine?.OnDiscreteCollisionDetected(hitCollider);
    }

    public void ApplyPlanarMovement(ref Vector3 currentVelocity)
    {
        Vector3 moveDirection = GetPlanarMovementDirection(currentVelocity);
        float currentMoveSpeed = _moveSpeed * _currentMoveSpeedMultiplier;
        currentVelocity.x = moveDirection.x * currentMoveSpeed;
        currentVelocity.z = moveDirection.z * currentMoveSpeed;
    }

    public bool TryConsumeJump(ref Vector3 currentVelocity)
    {
        if (!CanJump())
        {
            return false;
        }

        _context.ConsumeJumpRequest();
        currentVelocity.y = _jumpSpeed;

        if (_motor != null)
        {
            _motor.ForceUnground();
        }

        return true;
    }

    public bool TryStartDash()
    {
        if (_context == null || !_context.ConsumeDashRequest())
        {
            return false;
        }

        return _staminaManager == null || _staminaManager.ConsumeDashStamina();
    }

    public bool TryStartWallJump()
    {
        if (_context == null || !_context.JumpRequested || !CanWallJumpNow)
        {
            return false;
        }

        bool hasStamina = _staminaManager == null || _staminaManager.ConsumeWallJumpStamina();
        if (!hasStamina)
        {
            _context.ConsumeJumpRequest();
            return false;
        }

        _context.ConsumeJumpRequest();

        if (_motor != null)
        {
            _motor.ForceUnground();
        }

        _locomotionStateMachine?.AirborneState.PrepareForJump();
        _locomotionStateMachine?.RequestTransition(_locomotionStateMachine.AirborneState);
        return true;
    }

    public Vector3 BuildDashVelocity()
    {
        Vector3 dashDirection = GetDashDirection();
        return dashDirection * _dashSpeed;
    }

    public Vector3 BuildWallJumpVelocity()
    {
        Vector3 awayFromWall = CurrentWallNormal.sqrMagnitude > 0.0001f
            ? Vector3.ProjectOnPlane(CurrentWallNormal.normalized, Vector3.up)
            : transform.forward;

        if (awayFromWall.sqrMagnitude <= 0.0001f)
        {
            awayFromWall = transform.forward;
        }

        awayFromWall.Normalize();
        return (awayFromWall * _wallJumpHorizontalSpeed) + (Vector3.up * _wallJumpVerticalSpeed);
    }

    public void ClampVerticalVelocityToGround(ref Vector3 currentVelocity)
    {
        if (IsStableOnGround() && currentVelocity.y < 0f)
        {
            currentVelocity.y = 0f;
        }
    }

    public void ApplyGravity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (IsStableOnGround() && currentVelocity.y <= 0f)
        {
            return;
        }

        currentVelocity.y -= GetGravityScale(currentVelocity.y) * _gravity * deltaTime;
    }

    public void ClampWallSlideFallSpeed(ref Vector3 currentVelocity)
    {
        if (currentVelocity.y < -_wallSlideMaxFallSpeed)
        {
            currentVelocity.y = -_wallSlideMaxFallSpeed;
        }
    }

    public bool IsStableOnGroundNow()
    {
        return IsStableOnGround();
    }

    public bool HasMoveInput()
    {
        return _context != null && _context.MoveInput.sqrMagnitude > 0.0001f;
    }

    public void LogStateTransition(string machineName, string previousStateName, string nextStateName)
    {
        if (!_logStateTransitions)
        {
            return;
        }

        Debug.Log(
            $"[StateTransition] {machineName}: {previousStateName} -> {nextStateName}",
            this);
    }

    private void ResolveReferences()
    {
        if (_motor == null)
        {
            _motor = GetComponent<KinematicCharacterMotor>();
        }

        if (_context == null)
        {
            _context = GetComponent<PlayerContext>();
        }

        if (_groundDetector == null)
        {
            _groundDetector = GetComponent<GroundDetector>();
        }

        if (_wallEnvironmentHandler == null)
        {
            _wallEnvironmentHandler = GetComponent<WallEnvironmentHandler>();
        }

        if (_staminaManager == null)
        {
            _staminaManager = GetComponent<StaminaManager>();
        }
    }

    private void RegisterWithMotor()
    {
        if (_motor != null)
        {
            // Register explicitly so KCC routes its velocity and collision callbacks here.
            _motor.CharacterController = this;
        }
    }

    private void InitializeStateMachine()
    {
        _locomotionStateMachine = new LocomotionStateMachine(this);
        _locomotionStateMachine.Initialize();
        _actionStateMachine = new ActionStateMachine(this);
        _actionStateMachine.Initialize();
    }

    private Vector3 GetMoveDirection()
    {
        if (_context == null)
        {
            return Vector3.zero;
        }

        return Vector3.ClampMagnitude(_context.MoveDirection, 1f);
    }

    private Vector3 GetDashDirection()
    {
        Vector3 dashDirection = GetMoveDirection();
        if (dashDirection.sqrMagnitude > 0.0001f)
        {
            return dashDirection.normalized;
        }

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        return forward.normalized;
    }

    private bool CanJump()
    {
        return _context != null && _context.JumpRequested && IsStableOnGround();
    }

    private bool IsStableOnGround()
    {
        if (_groundDetector != null)
        {
            return _groundDetector.IsStableOnGround;
        }

        return _motor != null && _motor.GroundingStatus.IsStableOnGround;
    }

    private float GetGravityScale(float verticalVelocity)
    {
        if (verticalVelocity > 0f)
        {
            return _upwardGravityMultiplier;
        }

        if (verticalVelocity < 0f)
        {
            return _fallGravityMultiplier;
        }

        return 1f;
    }

    private void UpdateMoveAcceleration(float deltaTime)
    {
        bool hasMoveInput = HasMoveInput();
        bool isGrounded = IsStableOnGround();

        if (hasMoveInput)
        {
            Vector3 moveDirection = GetMoveDirection();
            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                _lastGroundedMoveDirection = moveDirection.normalized;
            }
        }

        if (!isGrounded)
        {
            SetImmediateMoveSpeedMultiplier(hasMoveInput ? 1f : 0f);
            _wasMoveInputActive = hasMoveInput;
            return;
        }

        if (hasMoveInput != _wasMoveInputActive)
        {
            BeginMoveBlend(
                _currentMoveSpeedMultiplier,
                hasMoveInput ? 1f : 0f,
                hasMoveInput ? _moveAccelerationDuration : _moveDecelerationDuration);
        }

        _wasMoveInputActive = hasMoveInput;

        if (_moveBlendDuration <= 0f || deltaTime <= 0f)
        {
            _currentMoveSpeedMultiplier = _moveBlendTargetMultiplier;
            return;
        }

        _moveBlendElapsed = Mathf.Min(_moveBlendElapsed + deltaTime, _moveBlendDuration);
        float normalizedTime = _moveBlendElapsed / _moveBlendDuration;
        AnimationCurve activeCurve = hasMoveInput ? _moveAccelerationCurve : _moveDecelerationCurve;
        float blendFactor = EvaluateMoveCurve(activeCurve, normalizedTime);
        _currentMoveSpeedMultiplier = hasMoveInput
            ? Mathf.Lerp(_moveBlendStartMultiplier, _moveBlendTargetMultiplier, blendFactor)
            : Mathf.Lerp(_moveBlendTargetMultiplier, _moveBlendStartMultiplier, blendFactor);
    }

    private Vector3 GetPlanarMovementDirection(Vector3 currentVelocity)
    {
        Vector3 moveDirection = GetMoveDirection();
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            return moveDirection;
        }

        Vector3 planarVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        if (IsStableOnGround() && planarVelocity.sqrMagnitude > 0.0001f)
        {
            _lastGroundedMoveDirection = planarVelocity.normalized;
        }

        if (IsStableOnGroundNow() && _currentMoveSpeedMultiplier > 0f)
        {
            return _lastGroundedMoveDirection;
        }

        return Vector3.zero;
    }

    private void BeginMoveBlend(float startMultiplier, float targetMultiplier, float duration)
    {
        _moveBlendStartMultiplier = startMultiplier;
        _moveBlendTargetMultiplier = targetMultiplier;
        _moveBlendDuration = duration;
        _moveBlendElapsed = 0f;
    }

    private void SetImmediateMoveSpeedMultiplier(float multiplier)
    {
        _currentMoveSpeedMultiplier = multiplier;
        _moveBlendStartMultiplier = multiplier;
        _moveBlendTargetMultiplier = multiplier;
        _moveBlendDuration = 0f;
        _moveBlendElapsed = 0f;
    }

    private static float EvaluateMoveCurve(AnimationCurve curve, float normalizedTime)
    {
        if (curve == null || curve.length == 0)
        {
            return 1f;
        }

        return Mathf.Clamp01(curve.Evaluate(Mathf.Clamp01(normalizedTime)));
    }

    private void LogWallDebugState(string phase)
    {
        if (!_logWallStateDebug || _wallEnvironmentHandler == null)
        {
            return;
        }

        Debug.Log(
            $"[WallDebug] Phase={phase}, Locomotion={CurrentLocomotionStateName}, AirborneSub={CurrentAirborneSubStateName}, StableGround={IsStableOnGroundNow()}, HasWall={HasWallContactNow}, Attached={IsAttachedToWallNow}, CanSlide={CanWallSlideNow}, CanJump={CanWallJumpNow}, WallNormal={CurrentWallNormal}, VerticalVelocity={LastKnownVerticalVelocity}",
            this);
    }

    private static string GetLayerName(Collider collider)
    {
        if (collider == null)
        {
            return "None";
        }

        return LayerMask.LayerToName(collider.gameObject.layer);
    }
}
