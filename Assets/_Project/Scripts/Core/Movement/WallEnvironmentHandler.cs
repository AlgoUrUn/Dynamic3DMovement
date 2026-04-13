using UnityEngine;

public sealed class WallEnvironmentHandler : MonoBehaviour
{
    // Only colliders on this layer are considered wall candidates.
    [SerializeField] private string _wallLayerName = "Wall";
    // Normals with too much upward component are treated as ground or slopes instead of walls.
    [SerializeField] private float _maxWallUpDot = 0.2f;
    // Minimum alignment between input/planar velocity and the wall-facing direction to count as "attached".
    [SerializeField] private float _minAttachAlignment = 0.1f;
    [SerializeField] private bool _logWallState;
    [SerializeField] private bool _logWallHitChecks;
    [SerializeField] private bool _logOnlyWhenChanged = true;
    [SerializeField] private float _logInterval = 0.25f;

    private bool _detectedWallThisFrame;
    private bool _lastLoggedHasWallContact;
    private bool _lastLoggedIsAttachedToWall;
    private Vector3 _lastLoggedWallNormal;
    private Collider _lastLoggedWallCollider;
    private float _lastLogTime;
    private bool _hasLoggedWallState;
    private Vector3 _moveDirection;
    private Vector3 _currentVelocity;

    public bool HasWallContact { get; private set; }
    public bool WasTouchingWall { get; private set; }
    public bool TouchedWallThisFrame { get; private set; }
    public bool LeftWallThisFrame { get; private set; }
    public bool IsAttachedToWall { get; private set; }
    public bool CanWallSlide { get; private set; }
    public bool CanWallJump { get; private set; }
    public Vector3 WallNormal { get; private set; } = Vector3.zero;
    public Collider WallCollider { get; private set; }
    public Vector3 WallPoint { get; private set; }

    public void BeginFrame()
    {
        // Rebuild wall contact from the current frame's collision callbacks.
        _detectedWallThisFrame = false;
    }

    public void SetKinematicContext(Vector3 moveDirection, Vector3 currentVelocity, bool isGrounded)
    {
        // Keep only planar intent for wall attachment checks.
        _moveDirection = Vector3.ProjectOnPlane(moveDirection, Vector3.up);
        _currentVelocity = currentVelocity;

        if (isGrounded)
        {
            // Grounded characters should not accumulate wall-attachment context.
            _moveDirection = Vector3.zero;
            _currentVelocity = Vector3.zero;
        }
    }

    public void RegisterHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint)
    {
        bool isWallCollider = IsWallCollider(hitCollider);
        bool isWallNormal = IsWallNormal(hitNormal);

        if (_logWallHitChecks)
        {
            Debug.Log(
                $"[WallHitCheck] collider={hitCollider?.name ?? "None"}, layer={GetLayerName(hitCollider)}, normal={hitNormal}, isWallCollider={isWallCollider}, isWallNormal={isWallNormal}",
                this);
        }

        // Ignore anything that is not explicitly tagged as a wall candidate.
        if (!isWallCollider || !isWallNormal)
        {
            return;
        }

        Vector3 normalizedWallNormal = hitNormal.normalized;
        float candidateScore = GetWallScore(normalizedWallNormal);

        // Prefer the most vertical wall that also best matches the current attachment direction.
        if (!_detectedWallThisFrame || candidateScore > GetWallScore(WallNormal))
        {
            WallNormal = normalizedWallNormal;
            WallCollider = hitCollider;
            WallPoint = hitPoint;
        }

        _detectedWallThisFrame = true;
    }

    public bool IsWallHit(Collider hitCollider, Vector3 hitNormal)
    {
        return IsWallCollider(hitCollider) && IsWallNormal(hitNormal);
    }

    public void FinalizeFrame(bool isGrounded)
    {
        // Convert the frame-local hit cache into a stable public wall state snapshot.
        WasTouchingWall = HasWallContact;
        HasWallContact = !isGrounded && _detectedWallThisFrame;
        IsAttachedToWall = HasWallContact && GetAttachmentAlignment(WallNormal) >= _minAttachAlignment;
        CanWallJump = HasWallContact;
        CanWallSlide = IsAttachedToWall && _currentVelocity.y <= 0f;
        TouchedWallThisFrame = !WasTouchingWall && HasWallContact;
        LeftWallThisFrame = WasTouchingWall && !HasWallContact;
        

        if (!HasWallContact)
        {
            // Clear cached wall data when contact ends so downstream systems do not use stale normals.
            WallNormal = Vector3.zero;
            WallCollider = null;
            WallPoint = Vector3.zero;
            IsAttachedToWall = false;
            CanWallSlide = false;
            CanWallJump = false;
        }

        LogWallState();
    }

    private bool IsWallNormal(Vector3 hitNormal)
    {
        if (hitNormal.sqrMagnitude <= 0f)
        {
            return false;
        }

        // Walls are expected to be close to vertical, so large up/down components are rejected.
        float upDot = Mathf.Abs(Vector3.Dot(hitNormal.normalized, Vector3.up));
        return upDot <= _maxWallUpDot;
    }

    private bool IsWallCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        int wallLayer = LayerMask.NameToLayer(_wallLayerName);
        if (wallLayer < 0)
        {
            return false;
        }

        return hitCollider.gameObject.layer == wallLayer;
    }

    private static string GetLayerName(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return "None";
        }

        return LayerMask.LayerToName(hitCollider.gameObject.layer);
    }

    private float GetWallScore(Vector3 wallNormal)
    {
        if (wallNormal.sqrMagnitude <= 0f)
        {
            return float.MinValue;
        }

        // Higher score means "more wall-like" and better aligned with the current attachment direction.
        float verticalityScore = 1f - Mathf.Abs(Vector3.Dot(wallNormal.normalized, Vector3.up));
        return verticalityScore + GetAttachmentAlignment(wallNormal);
    }

    private float GetAttachmentAlignment(Vector3 wallNormal)
    {
        if (wallNormal.sqrMagnitude <= 0f)
        {
            return 0f;
        }

        Vector3 attachmentDirection = GetAttachmentDirection();
        if (attachmentDirection.sqrMagnitude <= 0.0001f)
        {
            return 0f;
        }

        // Alignment is strongest when movement points into the wall's opposite normal.
        return Mathf.Max(0f, Vector3.Dot(attachmentDirection.normalized, -wallNormal.normalized));
    }

    private Vector3 GetAttachmentDirection()
    {
        // Prefer live input; fall back to planar velocity when no input is available.
        if (_moveDirection.sqrMagnitude > 0.0001f)
        {
            return _moveDirection;
        }

        Vector3 planarVelocity = Vector3.ProjectOnPlane(_currentVelocity, Vector3.up);
        if (planarVelocity.sqrMagnitude > 0.0001f)
        {
            return planarVelocity;
        }

        return Vector3.zero;
    }

    private void LogWallState()
    {
        if (!_logWallState)
        {
            return;
        }

        if (Time.unscaledTime < _lastLogTime + _logInterval)
        {
            return;
        }

        if (_logOnlyWhenChanged && !HasWallStateChangedSinceLastLog())
        {
            return;
        }

        Debug.Log(
            $"[WallEnvironmentHandler] Contact={HasWallContact}, Attached={IsAttachedToWall}, Slide={CanWallSlide}, Jump={CanWallJump}, Normal={WallNormal}, Collider={WallCollider}",
            this);

        _lastLoggedHasWallContact = HasWallContact;
        _lastLoggedIsAttachedToWall = IsAttachedToWall;
        _lastLoggedWallNormal = WallNormal;
        _lastLoggedWallCollider = WallCollider;
        _lastLogTime = Time.unscaledTime;
        _hasLoggedWallState = true;
    }

    private bool HasWallStateChangedSinceLastLog()
    {
        if (!_hasLoggedWallState)
        {
            return true;
        }

        if (_lastLoggedHasWallContact != HasWallContact)
        {
            return true;
        }

        if (_lastLoggedIsAttachedToWall != IsAttachedToWall)
        {
            return true;
        }

        if (_lastLoggedWallNormal != WallNormal)
        {
            return true;
        }

        if (_lastLoggedWallCollider != WallCollider)
        {
            return true;
        }

        return false;
    }
}
