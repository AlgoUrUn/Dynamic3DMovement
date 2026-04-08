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

    private void Awake()
    {
        // Cache once because the motor asks this controller for movement during its own update loop.
        ResolveReferences();
        RegisterWithMotor();
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
        // Rotation is intentionally unchanged until camera-facing rules are introduced.
    }

    /// <summary>
    /// Applies movement in the motor callback so KCC remains the single owner of character velocity.
    /// </summary>
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        Vector3 moveDirection = GetMoveDirection();

        currentVelocity.x = moveDirection.x * _moveSpeed;
        currentVelocity.z = moveDirection.z * _moveSpeed;

        if (CanJump())
        {
            Debug.Log("Jump!");
            _context.ConsumeJumpRequest();
            currentVelocity.y = _jumpSpeed;

            if (_motor != null)
            {
                _motor.ForceUnground();
            }
        }
        else if (IsStableOnGround() && currentVelocity.y < 0f)
        {
            currentVelocity.y = 0f;
        }

        if (!IsStableOnGround() || currentVelocity.y > 0f)
        {
            currentVelocity.y -= GetGravityScale(currentVelocity.y) * _gravity * deltaTime;
        }
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        // Reserved for state-machine hooks that must run before KCC grounding and movement.
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        if (_groundDetector != null)
        {
            _groundDetector.RefreshFromMotor();
        }
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        // Reserved for cleanup that must happen after KCC finishes resolving movement.
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
        // Ground hit behavior belongs to locomotion states once those are introduced.
    }

    public void OnMovementHit(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        // Movement-hit reactions are left to future wall and obstacle handling.
    }

    public void ProcessHitStabilityReport(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        Vector3 atCharacterPosition,
        Quaternion atCharacterRotation,
        ref HitStabilityReport hitStabilityReport)
    {
        // KCC's default stability report is enough before slope and wall rules exist.
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
        // Discrete collision effects are omitted until gameplay reactions require them.
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
