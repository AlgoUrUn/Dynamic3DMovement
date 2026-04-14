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

    private LocomotionStateMachine _locomotionStateMachine;
    private ActionStateMachine _actionStateMachine;

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
        _locomotionStateMachine?.UpdateVelocity(ref currentVelocity, deltaTime);
        _actionStateMachine?.UpdateVelocity(ref currentVelocity, deltaTime);
        _wallEnvironmentHandler?.SetKinematicContext(GetMoveDirection(), currentVelocity, IsStableOnGround());
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
        Vector3 moveDirection = GetMoveDirection();
        currentVelocity.x = moveDirection.x * _moveSpeed;
        currentVelocity.z = moveDirection.z * _moveSpeed;
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
