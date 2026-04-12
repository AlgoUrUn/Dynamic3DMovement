using KinematicCharacterController;
using UnityEngine;

public sealed class CameraPointer : MonoBehaviour
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private PlayerContext _playerContext;
    [SerializeField] private KinematicCharacterMotor _yawMotor;
    [SerializeField] private Transform _yawTarget;
    [SerializeField] private Transform _pitchTarget;
    [SerializeField] private float _horizontalSensitivity = 0.12f;
    [SerializeField] private float _verticalSensitivity = 0.12f;
    [SerializeField] private float _minPitch = -80f;
    [SerializeField] private float _maxPitch = 80f;
    [SerializeField] private bool _invertY;
    [SerializeField] private bool _lockCursor = true;

    private float _yaw;
    private float _pitch;

    private void Awake()
    {
        ResolveReferences();
        RegisterMovementReference();
        InitializeAnglesFromTargets();
        ApplyRotation();
    }

    private void OnEnable()
    {
        RegisterMovementReference();

        if (_lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        if (_lockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void LateUpdate()
    {
        Vector2 lookInput = ReadLookInput();
        if (lookInput.sqrMagnitude <= 0f)
        {
            return;
        }

        _yaw += lookInput.x * _horizontalSensitivity;

        float verticalInput = _invertY ? lookInput.y : -lookInput.y;
        _pitch = Mathf.Clamp(_pitch + (verticalInput * _verticalSensitivity), _minPitch, _maxPitch);

        ApplyRotation();
    }

    private Vector2 ReadLookInput()
    {
        if (_inputReader != null)
        {
            return _inputReader.GetLookInput();
        }

        if (_playerContext != null)
        {
            return _playerContext.LookInput;
        }

        return Vector2.zero;
    }

    private void ResolveReferences()
    {
        if (_inputReader == null)
        {
            _inputReader = GetComponentInParent<PlayerInputReader>();
        }

        if (_playerContext == null)
        {
            _playerContext = GetComponentInParent<PlayerContext>();
        }

        if (_yawTarget == null)
        {
            _yawTarget = transform;
        }

        if (_yawMotor == null && _yawTarget != null)
        {
            _yawMotor = _yawTarget.GetComponent<KinematicCharacterMotor>();
        }

        if (_pitchTarget == null)
        {
            _pitchTarget = transform;
        }
    }

    private void RegisterMovementReference()
    {
        if (_playerContext == null)
        {
            return;
        }

        _playerContext.SetMovementReference(_pitchTarget != null ? _pitchTarget : transform);
    }

    private void InitializeAnglesFromTargets()
    {
        Vector3 yawAngles = _yawTarget.rotation.eulerAngles;
        _yaw = yawAngles.y;

        Vector3 pitchAngles = _pitchTarget.localEulerAngles;
        _pitch = NormalizeAngle(pitchAngles.x);
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
    }

    private void ApplyRotation()
    {
        Quaternion yawRotation = Quaternion.AngleAxis(_yaw, Vector3.up);
        Quaternion pitchRotation = Quaternion.AngleAxis(_pitch, Vector3.right);

        if (_yawTarget == _pitchTarget)
        {
            SetYawRotation(yawRotation * pitchRotation);
            return;
        }

        SetYawRotation(yawRotation);
        _pitchTarget.localRotation = pitchRotation;
    }

    private void SetYawRotation(Quaternion rotation)
    {
        if (_yawMotor != null && _yawMotor.Transform == _yawTarget)
        {
            _yawMotor.SetRotation(rotation);
            return;
        }

        _yawTarget.rotation = rotation;
    }

    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
