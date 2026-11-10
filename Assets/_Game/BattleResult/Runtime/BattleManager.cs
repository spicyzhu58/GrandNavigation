using System;
using System.Collections.Generic;
using UnityEngine;
namespace GrandNavigation.Gameplay.Battle
{
    public enum BattleState { Playing, Victory, Defeat }
    [DefaultExecutionOrder(10000)]
    public sealed class BattleManager : MonoBehaviour
    {
        public PlayerShip player;
        [Tooltip("Fixed roster for this battle. Duplicates, friendly and inactive ships are ignored.")]
        public EnemyShip[] participants = Array.Empty<EnemyShip>();
        public ResultUI resultUI;
        public BattleState State { get; private set; }
        public int TotalEnemies => roster.Count;
        public int RemainingEnemies => roster.Count - dead.Count;
        public event Action<BattleState> Ended;
        readonly Dictionary<HealthSystem, Action> roster = new();
        readonly HashSet<HealthSystem> dead = new();
        bool initialized, ownsPause;
        void Start()
        {
            if (!player || !player.Health) { Debug.LogError("BattleManager requires a player with HealthSystem.", this); return; }
            foreach (var enemy in participants)
            {
                if (!enemy || !enemy.isActiveAndEnabled || enemy.team == player.team || !enemy.Health || roster.ContainsKey(enemy.Health)) continue;
                var health = enemy.Health;
                Action handler = () => dead.Add(health);
                roster.Add(health, handler); health.Died += handler;
                if (!health.IsAlive) dead.Add(health);
            }
            if (roster.Count == 0) Debug.LogWarning("BattleManager: no participating enemies; automatic victory disabled.", this);
            initialized = true;
        }
        void LateUpdate()
        {
            if (!initialized || State != BattleState.Playing) return;
            // Aggregate the frame's deaths. Never choose a result in a death callback.
            if (!player || !player.Health || !player.IsAlive) Finish(BattleState.Defeat);
            else if (roster.Count > 0 && dead.Count == roster.Count) Finish(BattleState.Victory);
        }
        void Finish(BattleState state)
        {
            if (State != BattleState.Playing) return;
            State = state;
            // Freeze Update-driven input/AI too: timeScale alone only stops scaled physics.
            foreach (var component in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (component.gameObject.scene != gameObject.scene) continue;
                if (component is HealthSystem health) health.DamageBlocked = true;
                var ns = component.GetType().Namespace;
                if (ns != null && ns.StartsWith("GrandNavigation.Gameplay") && !ns.StartsWith("GrandNavigation.Gameplay.Battle")) component.enabled = false;
            }
            Time.timeScale = 0; ownsPause = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (resultUI) resultUI.Show(state);
            Ended?.Invoke(state);
        }
        void OnDestroy()
        {
            foreach (var entry in roster) if (entry.Key) entry.Key.Died -= entry.Value;
            if (ownsPause) Time.timeScale = 1;
        }
    }
}
