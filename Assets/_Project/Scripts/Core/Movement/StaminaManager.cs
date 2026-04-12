using UnityEngine;

public sealed class StaminaManager : MonoBehaviour
{
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _startingStamina = 100f;
    [SerializeField] private float _recoveryPerSecond = 15f;
    [SerializeField] private float _dashStaminaCost = 25f;
    [SerializeField] private float _wallJumpStaminaCost = 20f;

    public float MaxStamina => _maxStamina;
    public float CurrentStamina { get; private set; }
    public float RecoveryPerSecond => _recoveryPerSecond;
    public float DashStaminaCost => _dashStaminaCost;
    public float WallJumpStaminaCost => _wallJumpStaminaCost;
    public float NormalizedStamina => _maxStamina <= 0f ? 0f : CurrentStamina / _maxStamina;

    private void Awake()
    {
        CurrentStamina = Mathf.Clamp(_startingStamina, 0f, _maxStamina);
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

        CurrentStamina = Mathf.Min(CurrentStamina + amount, _maxStamina);
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

        CurrentStamina -= amount;
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
        CurrentStamina = Mathf.Clamp(stamina, 0f, _maxStamina);
    }
}
