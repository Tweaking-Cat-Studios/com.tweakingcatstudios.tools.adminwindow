using System;
using System.Collections.Generic;
using UnityEngine;
#if HAS_REFLEX
using Reflex.Injectors;
#endif

// Singleton-like game coordinator registered via Reflex DI (no manual singleton pattern)
public class GameRunner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private EnemyInstance _enemyPrefab;
    [SerializeField] private Transform _enemiesParent;

    private readonly List<EnemyInstance> _enemies = new List<EnemyInstance>();

    /// <summary>Total enemies currently tracked by the runner.</summary>
    public int EnemyCount => _enemies.Count;

    /// <summary>Read-only snapshot of the list. Do not modify from outside.</summary>
    public IReadOnlyList<EnemyInstance> Enemies => _enemies;

    private void Awake()
    {
        ResetGame();
    }

    // Destroys any existing enemies and spawns a fresh one from prefab
    public void ResetGame()
    {
        // Destroy existing
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            EnemyInstance e = _enemies[i];
            if (e != null)
            {
                Destroy(e.gameObject);
            }
        }
        _enemies.Clear();

        // Spawn initial enemy if prefab is set
        if (_enemyPrefab != null)
        {
            SpawnNewEnemy();
        }
        else
        {
            Debug.LogWarning("GameRunner: Enemy prefab is not assigned.");
        }
    }

    // Spawns an enemy at (0,0, 1 * numberOfExistingEnemies)
    public EnemyInstance SpawnNewEnemy()
    {
        if (_enemyPrefab == null)
        {
            Debug.LogError("GameRunner: Cannot spawn enemy, prefab not assigned.");
            return null;
        }

        Vector3 position = new Vector3(0f, 0f, 1f * _enemies.Count);
        Transform parent = _enemiesParent != null ? _enemiesParent : null;
        EnemyInstance instance = Instantiate(_enemyPrefab, position, Quaternion.identity, parent);

        // Ensure DI happens for newly spawned instance so [Inject] fields are resolved
#if HAS_REFLEX
        if (instance != null)
        {
            GameObjectInjector.InjectRecursive(instance.gameObject);
        }
#endif

        if (instance != null)
        {
            instance.InitEnemy();
            _enemies.Add(instance);
        }

        return instance;
    }

    // Removes a specific enemy instance by reference and destroys its GameObject
    public void RemoveEnemy(EnemyInstance enemy)
    {
        if (enemy == null) return;
        int idx = _enemies.IndexOf(enemy);
        if (idx >= 0) _enemies.RemoveAt(idx);
        Destroy(enemy.gameObject);
    }

    // --- 👇 Useful helpers for Admin & tests ---

    /// <summary>Spawn N new enemies in a line. Returns how many were spawned.</summary>
    public int SpawnEnemies(int count, float spacing = 1f, Vector3? startOffset = null)
    {
        if (count <= 0) return 0;
        int spawned = 0;
        Vector3 offset = startOffset ?? Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            EnemyInstance inst = SpawnNewEnemy();
            if (inst != null)
            {
                inst.transform.position = offset + new Vector3(0f, 0f, spacing * (EnemyCount - 1));
                spawned++;
            }
        }
        return spawned;
    }

    /// <summary>Remove all tracked enemies.</summary>
    public void RemoveAllEnemies()
    {
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            EnemyInstance e = _enemies[i];
            if (e) Destroy(e.gameObject);
        }
        _enemies.Clear();
    }

    /// <summary>Apply an action to each tracked enemy (null-safe).</summary>
    public void ForEachEnemy(Action<EnemyInstance> action)
    {
        if (action == null) return;
        for (int i = 0; i < _enemies.Count; i++)
        {
            EnemyInstance e = _enemies[i];
            if (e) action(e);
        }
    }

    /// <summary>Set health for all tracked enemies.</summary>
    public void SetAllEnemiesHealth(int value)
    {
        ForEachEnemy(e => e.SetHealth(value));
    }

    /// <summary>Compute (min, max, avg) health for tracked enemies.</summary>
    public (int min, int max, float avg) GetHealthStats()
    {
        if (_enemies.Count == 0) return (0, 0, 0);
        int min = int.MaxValue, max = int.MinValue, sum = 0, n = 0;
        ForEachEnemy(e =>
        {
            int h = e.GetHealth();
            if (h < min) min = h;
            if (h > max) max = h;
            sum += h; n++;
        });
        return (n == 0 ? 0 : min, n == 0 ? 0 : max, n == 0 ? 0 : (float)sum / n);
    }
}
