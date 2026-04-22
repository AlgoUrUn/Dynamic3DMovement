using UnityEngine;

public sealed class PlayerStateMachine : MonoBehaviour
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private PlayerContext _context;
    [SerializeField] private PlayerCharacterController _controller;
    [SerializeField] private PlayerAnimationController _animationController;
    [SerializeField] private bool _runAutomatically = true;
    [SerializeField] private bool _useExternalInputDriver = true;

    public PlayerInputReader InputReader => _inputReader;
    public PlayerContext Context => _context;
    public PlayerCharacterController Controller => _controller;
    public PlayerAnimationController AnimationController => _animationController;

    private void Awake()
    {
        ResolveReferences();
        ConfigureInputDriver();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (!_runAutomatically)
        {
            return;
        }

        UpdateContext();
        UpdateStates();
        SendIntentToMotor();
    }

    private void LateUpdate()
    {
        if (!_runAutomatically)
        {
            return;
        }

        UpdateAnimation();
        _inputReader?.ClearFrameInput();
    }

    public void UpdateContext()
    {
        ResolveReferences();
        _inputReader?.RefreshInputContext();
    }

    public void UpdateStates()
    {
        ResolveReferences();

        if (_controller == null || _context == null)
        {
            return;
        }

        _controller.UpdateContext(_context);
        _controller.UpdateStates();
    }

    public void SendIntentToMotor()
    {
        ResolveReferences();
        _controller?.SendIntentToMotor();
    }

    public void UpdateAnimation()
    {
        ResolveReferences();
        _animationController?.UpdateAnimation();
    }

    private void ResolveReferences()
    {
        if (_inputReader == null)
        {
            _inputReader = GetComponent<PlayerInputReader>();
        }

        if (_context == null)
        {
            _context = GetComponent<PlayerContext>();
        }

        if (_controller == null)
        {
            _controller = GetComponent<PlayerCharacterController>();
        }

        if (_animationController == null)
        {
            _animationController = GetComponent<PlayerAnimationController>();
        }
    }

    private void ConfigureInputDriver()
    {
        if (_inputReader == null)
        {
            return;
        }

        _inputReader.SetUseInternalUpdateLoop(!_useExternalInputDriver);
    }
}
