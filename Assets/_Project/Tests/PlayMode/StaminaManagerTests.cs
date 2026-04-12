using NUnit.Framework;
using UnityEngine;

public sealed class StaminaManagerTests
{
    private GameObject _playerObject;

    [TearDown]
    public void TearDown()
    {
        if (_playerObject != null)
        {
            Object.DestroyImmediate(_playerObject);
        }
    }

    [Test]
    public void Consume_ReducesStaminaWhenEnoughResourceExists()
    {
        var staminaManager = CreateStaminaManager();

        bool consumed = staminaManager.Consume(30f);

        Assert.That(consumed, Is.True);
        Assert.That(staminaManager.CurrentStamina, Is.EqualTo(70f).Within(0.0001f));
    }

    [Test]
    public void Consume_DoesNotChangeStaminaWhenResourceIsInsufficient()
    {
        var staminaManager = CreateStaminaManager();
        staminaManager.SetCurrentStamina(10f);

        bool consumed = staminaManager.Consume(30f);

        Assert.That(consumed, Is.False);
        Assert.That(staminaManager.CurrentStamina, Is.EqualTo(10f).Within(0.0001f));
    }

    [Test]
    public void Recover_RestoresStaminaWithoutExceedingMaximum()
    {
        var staminaManager = CreateStaminaManager();
        staminaManager.SetCurrentStamina(80f);

        staminaManager.Recover(50f);

        Assert.That(staminaManager.CurrentStamina, Is.EqualTo(staminaManager.MaxStamina).Within(0.0001f));
    }

    [Test]
    public void Tick_RecoversStaminaUsingRecoveryRate()
    {
        var staminaManager = CreateStaminaManager();
        staminaManager.SetCurrentStamina(40f);

        staminaManager.Tick(2f);

        Assert.That(staminaManager.CurrentStamina, Is.EqualTo(70f).Within(0.0001f));
    }

    private StaminaManager CreateStaminaManager()
    {
        _playerObject = new GameObject("Player");
        return _playerObject.AddComponent<StaminaManager>();
    }
}
