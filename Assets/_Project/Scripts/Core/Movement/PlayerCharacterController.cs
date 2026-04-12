using KinematicCharacterController;
using UnityEngine;

public sealed class PlayerCharacterController : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor _motor;
    [SerializeField] private PlayerContext _context;
    [SerializeField] private GroundDetector _groundDetector;
    [SerializeField] private float _moveSpeed = 6f;
    [SerializeField] private float _jumpSpeed = 8f;
    [SerializeField] private float _gravity = 30f;
    [SerializeField] private float _upwardGravityMultiplier = 0.75f;
    [SerializeField] private float _fallGravityMultiplier = 1.25f;

    public KinematicCharacterMotor Motor => _motor;
    public PlayerContext Context => _context;
    public GroundDetector GroundDetector => _groundDetector;
    public float MoveSpeed => _moveSpeed;
    public float JumpSpeed => _jumpSpeed;
    public float Gravity => _gravity;
    public float UpwardGravityMultiplier => _upwardGravityMultiplier;
    public float FallGravityMultiplier => _fallGravityMultiplier;
    public string CurrentLocomotionStateName => _locomotionStateMachine?.CurrentStateName;
    public string CurrentGroundedSubStateName => _locomotionStateMachine?.CurrentGroundedSubStateName;

    private LocomotionStateMachine _locomotionStateMachine;

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
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _locomotionStateMachine?.BeforeCharacterUpdate(deltaTime);
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        if (_groundDetector != null)
        {
            _groundDetector.RefreshFromMotor();
        }

        _locomotionStateMachine?.PostGroundingUpdate(deltaTime);
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        _locomotionStateMachine?.AfterCharacterUpdate(deltaTime);
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

    public bool IsStableOnGroundNow()
    {
        return IsStableOnGround();
    }

    public bool HasMoveInput()
    {
        return _context != null && _context.MoveInput.sqrMagnitude > 0.0001f;
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
    }

    private Vector3 GetMoveDirection()
    {
        if (_context == null)
        {
            return Vector3.zero;
        }

        return Vector3.ClampMagnitude(_context.MoveDirection, 1f);
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
}
