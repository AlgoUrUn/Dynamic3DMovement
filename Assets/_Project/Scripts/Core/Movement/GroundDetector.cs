using KinematicCharacterController;
using UnityEngine;

public sealed class GroundDetector : MonoBehaviour
{
    [SerializeField] private KinematicCharacterMotor _motor;
    [SerializeField] private bool _logGroundState;
    [SerializeField] private bool _logOnlyWhenChanged = true;
    [SerializeField] private float _logInterval = 0.25f;

    private bool _lastLoggedFoundAnyGround;
    private bool _lastLoggedStableOnGround;
    private Vector3 _lastLoggedGroundNormal;
    private Collider _lastLoggedGroundCollider;
    private float _lastLogTime;
    private bool _hasLoggedGroundState;

    public KinematicCharacterMotor Motor => _motor;
    public bool FoundAnyGround { get; private set; }
    public bool IsStableOnGround { get; private set; }
    public bool WasStableOnGround { get; private set; }
    public bool LandedThisFrame { get; private set; }
    public bool LeftStableGroundThisFrame { get; private set; }
    public bool IsGrounded => IsStableOnGround;
    public bool IsAirborne => !IsStableOnGround;
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    public Collider GroundCollider { get; private set; }
    public Vector3 GroundPoint { get; private set; }

    private void Awake()
    {
        ResolveReferences();
    }

    public void RefreshFromMotor()
    {
        ResolveReferences();

        if (_motor == null)
        {
            Refresh(default);
            return;
        }

        Refresh(_motor.GroundingStatus);
    }

    public void Refresh(CharacterGroundingReport groundingStatus)
    {
        bool wasStableOnGround = IsStableOnGround;

        FoundAnyGround = groundingStatus.FoundAnyGround;
        IsStableOnGround = groundingStatus.IsStableOnGround;
        WasStableOnGround = wasStableOnGround;
        LandedThisFrame = !wasStableOnGround && IsStableOnGround;
        LeftStableGroundThisFrame = wasStableOnGround && !IsStableOnGround;
        GroundNormal = FoundAnyGround && groundingStatus.GroundNormal.sqrMagnitude > 0f
            ? groundingStatus.GroundNormal.normalized
            : Vector3.up;
        GroundCollider = groundingStatus.GroundCollider;
        GroundPoint = groundingStatus.GroundPoint;

        LogGroundState();
    }

    private void ResolveReferences()
    {
        if (_motor == null)
        {
            _motor = GetComponent<KinematicCharacterMotor>();
        }
    }

    private void LogGroundState()
    {
        if (!_logGroundState)
        {
            return;
        }

        if (Time.unscaledTime < _lastLogTime + _logInterval)
        {
            return;
        }

        if (_logOnlyWhenChanged && !HasGroundStateChangedSinceLastLog())
        {
            return;
        }

        Debug.Log(
            $"[GroundDetector] FoundAnyGround={FoundAnyGround}, Stable={IsStableOnGround}, Landed={LandedThisFrame}, LeftStableGround={LeftStableGroundThisFrame}, Normal={GroundNormal}, Collider={GroundCollider}",
            this);

        _lastLoggedFoundAnyGround = FoundAnyGround;
        _lastLoggedStableOnGround = IsStableOnGround;
        _lastLoggedGroundNormal = GroundNormal;
        _lastLoggedGroundCollider = GroundCollider;
        _lastLogTime = Time.unscaledTime;
        _hasLoggedGroundState = true;
    }

    private bool HasGroundStateChangedSinceLastLog()
    {
        if (!_hasLoggedGroundState)
        {
            return true;
        }

        if (_lastLoggedFoundAnyGround != FoundAnyGround)
        {
            return true;
        }

        if (_lastLoggedStableOnGround != IsStableOnGround)
        {
            return true;
        }

        if (_lastLoggedGroundNormal != GroundNormal)
        {
            return true;
        }

        if (_lastLoggedGroundCollider != GroundCollider)
        {
            return true;
        }

        return false;
    }
}
