using UnityEngine;
using UnityEngine.UI;
using System;

public sealed class StaminaManager : MonoBehaviour
{
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _startingStamina = 100f;
    [SerializeField] private float _recoveryPerSecond = 15f;
    [SerializeField] private float _dashStaminaCost = 25f;
    [SerializeField] private float _wallJumpStaminaCost = 20f;
    [SerializeField] private Slider _staminaBar;

    public event Action<float, float> StaminaChanged;

    public float MaxStamina => _maxStamina;
    public float CurrentStamina { get; private set; }
    public float RecoveryPerSecond => _recoveryPerSecond;
    public float DashStaminaCost => _dashStaminaCost;
    public float WallJumpStaminaCost => _wallJumpStaminaCost;
    public float NormalizedStamina => _maxStamina <= 0f ? 0f : CurrentStamina / _maxStamina;

    private void Awake()
    {
        SetStaminaInternal(_startingStamina, true);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f || CurrentStamina >= _maxStamina)
        {
            return;
        }

        Recover(_recoveryPerSecond * deltaTime);
    }

    public void Recover(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SetStaminaInternal(CurrentStamina + amount);
    }

    public bool Consume(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (CurrentStamina < amount)
        {
            return false;
        }

        SetStaminaInternal(CurrentStamina - amount);
        return true;
    }

    public bool ConsumeDashStamina()
    {
        return Consume(_dashStaminaCost);
    }

    public bool ConsumeWallJumpStamina()
    {
        return Consume(_wallJumpStaminaCost);
    }

    public void SetCurrentStamina(float stamina)
    {
        SetStaminaInternal(stamina);
    }

    public void UpdateStaminaBar()
    {
        if (_staminaBar != null)
        {
            _staminaBar.minValue = 0f;
            _staminaBar.maxValue = 1f;
            _staminaBar.value = NormalizedStamina;
        }
    }

    private void SetStaminaInternal(float stamina, bool notifyIfUnchanged = false)
    {
        float clampedStamina = Mathf.Clamp(stamina, 0f, _maxStamina);
        if (!notifyIfUnchanged && Mathf.Approximately(CurrentStamina, clampedStamina))
        {
            return;
        }

        CurrentStamina = clampedStamina;
        UpdateStaminaBar();
        StaminaChanged?.Invoke(CurrentStamina, NormalizedStamina);
    }
}
