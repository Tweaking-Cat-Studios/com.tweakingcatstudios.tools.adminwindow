#if UNITY_EDITOR
using com.tcs.tools.adminwindow.Core.Attributes;
using com.tcs.tools.adminwindow.Core.Binding;
using TCS.AdminWindow.Core.Integration;
using UnityEngine;

// Admin commands for the test system.
// These live in the Editor assembly and call your runtime methods safely.
public static class GameRunnerAdminCommands
{
    // -------- GameRunner (DI instance) --------

    [AdminCommand("game.reset", category:"Game", help:"Reset game: clears enemies and spawns initial one (if prefab set).")]
    public static string ResetGame()
    {
        var runner = ResolveRunnerOrThrow();
        runner.ResetGame();
        return "Game reset.";
    }

    [AdminCommand("game.spawn", category:"Game", help:"Spawn N enemies in a line. Usage: game.spawn 5 --spacing=1.5")]
    public static string Spawn(int count, float spacing = 1f)
    {
        var runner = ResolveRunnerOrThrow();
        var n = runner.SpawnEnemies(count, spacing);
        return $"Spawned {n} enemy(ies). Total={runner.EnemyCount}.";
    }

    [AdminCommand("game.remove-all", category:"Game", help:"Remove all tracked enemies.")]
    public static string RemoveAll()
    {
        var runner = ResolveRunnerOrThrow();
        runner.RemoveAllEnemies();
        return "Removed all enemies.";
    }

    [AdminCommand("game.stats", category:"Game", help:"Print enemy health min/max/avg stats.")]
    public static string Stats()
    {
        var runner = ResolveRunnerOrThrow();
        var (min, max, avg) = runner.GetHealthStats();
        return $"Enemies={runner.EnemyCount} | Health min={min} max={max} avg={avg:0.0}";
    }

    // -------- Enemy bulk ops (selection/scene/project) --------

    [AdminCommand("enemy.set-health", category:"Enemy", help:"Set health on targets (defaults to current selection).")]
    public static string EnemySetHealth(EntitySet<EnemyInstance> targets, int value)
    {
        int n = targets.Apply("Set Enemy Health",
            sceneInstance: e => e.SetHealth(value),
            assetSO: so => so.FindProperty("_health").intValue = value);
        return $"Set health={value} on {n} EnemyInstance ({targets.Via}).";
    }

    [AdminCommand("enemy.damage", category:"Enemy", help:"Damage targets by amount (selection by default).")]
    public static string EnemyDamage(EntitySet<EnemyInstance> targets, int amount)
    {
        int n = targets.Apply("Damage Enemy",
            sceneInstance: e => e.TakeDamage(amount),
            assetSO: so => so.FindProperty("_health").intValue =
                Mathf.Max(0, so.FindProperty("_health").intValue - amount));
        return $"Damaged {n} EnemyInstance by {amount} ({targets.Via}).";
    }

    [AdminCommand("enemy.kill", category:"Enemy", help:"Kill (remove) targets.")]
    public static string EnemyKill(EntitySet<EnemyInstance> targets)
    {
        int n = targets.Apply("Kill Enemy",
            sceneInstance: e => e.Kill(),
            assetSO: so => so.FindProperty("_health").intValue = 0);
        return $"Killed {n} EnemyInstance ({targets.Via}).";
    }

    [AdminCommand("enemy.set-max-health", category:"Enemy", help:"Set max health on targets; clamps current.")]
    public static string EnemySetMaxHealth(EntitySet<EnemyInstance> targets, int newMax)
    {
        int n = targets.Apply("Set Enemy Max Health",
            sceneInstance: e => e.SetMaxHealth(newMax, clampCurrent:true),
            assetSO: so =>
            {
                so.FindProperty("_maxHealth").intValue = Mathf.Max(1, newMax);
                var cur = so.FindProperty("_health");
                cur.intValue = Mathf.Clamp(cur.intValue, 0, Mathf.Max(1, newMax));
            });
        return $"Set maxHealth={newMax} on {n} EnemyInstance ({targets.Via}).";
    }

    // -------- Helpers --------

    private static GameRunner ResolveRunnerOrThrow()
    {
        // Prefer Reflex DI via Admin resolver; fallback to scene search.
        var t = typeof(GameRunner);
        var resolver = ReflexAdapter.TryResolve(t);
        if (resolver != null)
        {
            try
            {
                var obj = resolver();
                if (obj is GameRunner inst) return inst;
            }
            catch (System.Exception ex)
            {
                // Swallow DI errors and fall back to scene search, but surface details in Console.
                Debug.LogError($"Reflex resolve for {t.Name} failed: {(ex.InnerException != null ? ex.InnerException.Message : ex.Message)}\n{ex}");
            }
        }

        var found = Object.FindFirstObjectByType<GameRunner>();
        if (found != null) return found;

        throw new System.Exception("GameRunner instance not found. Ensure a SceneScope is loaded (for Reflex) or a GameRunner exists in the scene.");
    }
}
#endif
