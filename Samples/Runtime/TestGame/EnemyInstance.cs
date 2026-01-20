using System;
using UnityEngine;
#if HAS_REFLEX
using Reflex.Attributes;
#endif

// Simple enemy component that manages health and visual feedback
public class EnemyInstance : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _health = 100;

    private MeshRenderer _meshRenderer;

#if HAS_REFLEX
    // Injected reference to the GameRunner (singleton via Reflex DI)
    [Inject] private GameRunner _gameRunner;
#endif

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    // Initializes/Resets the enemy state
    public void InitEnemy(int? startingHealth = null)
    {
        _health = Mathf.Clamp(startingHealth ?? _maxHealth, 0, _maxHealth);
        UpdateColor();
    }

    public void SetHealth(int value)
    {
        _health = Mathf.Clamp(value, 0, _maxHealth);
        UpdateColor();
    }

    public int GetHealth() => _health;

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        SetHealth(_health - amount);

        if (_health <= 0)
        {
            // Inform the GameRunner that this enemy should be removed
#if HAS_REFLEX
            if (_gameRunner != null)
            {
                _gameRunner.RemoveEnemy(this);
            }
            else
#endif
            {
                // Fallback: just destroy if no runner is available
                Destroy(gameObject);
            }
        }
    }

    /// <summary>Heal by amount (clamped to max).</summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        SetHealth(_health + amount);
    }

    /// <summary>Kill this enemy immediately (health to zero, destroy via runner if present).</summary>
    public void Kill()
    {
        SetHealth(0);
#if HAS_REFLEX
        if (_gameRunner != null) { _gameRunner.RemoveEnemy(this); return; }
#endif
        Destroy(gameObject);
    }

    /// <summary>Revive enemy to specific health (default max) and ensure visible.</summary>
    public void Revive(int? health = null)
    {
        _health = Mathf.Clamp(health ?? _maxHealth, 1, _maxHealth);
        gameObject.SetActive(true);
        UpdateColor();
    }

    /// <summary>Change max health; optionally clamp current health into new range.</summary>
    public void SetMaxHealth(int newMax, bool clampCurrent = true)
    {
        _maxHealth = Mathf.Max(1, newMax);
        if (clampCurrent) _health = Mathf.Clamp(_health, 0, _maxHealth);
        UpdateColor();
    }

    private void Update()
    {
        // Lerp color from red (0 health) to green (full health)
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (_meshRenderer == null) return;

        float t = _maxHealth > 0 ? Mathf.Clamp01((float)_health / _maxHealth) : 0f;
        // 0 -> red, 1 -> green
        Material mat = _meshRenderer.sharedMaterial != null ? _meshRenderer.sharedMaterial : _meshRenderer.material;
        mat.SetColor("_BaseColor", Color.Lerp(Color.red, Color.green, t));
    }
}
