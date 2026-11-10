using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using GrandNavigation.Gameplay.AI;
namespace GrandNavigation.Gameplay.Battle.Tests
{
    public class BattleResultTests
    {
        const string Scene = "Assets/_Game/Scenes/BattleLoop.unity";
        BattleManager manager;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale=1;Application.runInBackground=true;
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return null;
            manager=Object.FindFirstObjectByType<BattleManager>();
            foreach(var ai in Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None))ai.enabled=false;
            Assert.AreEqual(3,manager.TotalEnemies);
            Assert.IsFalse(manager.resultUI.panel.activeSelf);
        }
        [TearDown] public void Cleanup(){Time.timeScale=1;}
        void KillEnemies(){foreach(var enemy in manager.participants)enemy.TakeDamage(int.MaxValue);}
        [UnityTest] public IEnumerator PartialThenVictoryAndFrozenDamage()
        {
            int events=0;manager.Ended+=_=>events++;
            manager.participants[0].TakeDamage(int.MaxValue);
            manager.participants[0].TakeDamage(int.MaxValue);
            yield return null;yield return null;
            Assert.AreEqual(BattleState.Playing,manager.State);Assert.AreEqual(2,manager.RemainingEnemies);
            KillEnemies();yield return null;yield return null;
            Assert.AreEqual(BattleState.Victory,manager.State);Assert.AreEqual(0,manager.RemainingEnemies);
            Assert.AreEqual(1,events);Assert.AreEqual(1,manager.resultUI.ShowCount);
            Assert.AreEqual("VICTORY",manager.resultUI.title.text);Assert.AreEqual(0,Time.timeScale);
            int hp=manager.player.Health.CurrentHealth;manager.player.TakeDamage(int.MaxValue);
            Assert.AreEqual(hp,manager.player.Health.CurrentHealth);
            Assert.IsFalse(manager.player.FireWeapon());
            Assert.IsTrue(Object.FindObjectsByType<PlayerCombatInput>(FindObjectsSortMode.None).All(c=>!c.enabled));
            Assert.IsTrue(Object.FindObjectsByType<PlayerShipInput>(FindObjectsSortMode.None).All(c=>!c.enabled));
            Assert.IsTrue(Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None).All(c=>!c.enabled));
            manager.resultUI.Show(BattleState.Defeat);yield return null;
            Assert.AreEqual(BattleState.Victory,manager.State);Assert.AreEqual(1,manager.resultUI.ShowCount);
        }
        [UnityTest] public IEnumerator SameFrameEnemyFirstDefeatWins()
        {
            KillEnemies();manager.player.TakeDamage(int.MaxValue);
            Assert.AreEqual(BattleState.Playing,manager.State);
            yield return null;yield return null;
            Assert.AreEqual(BattleState.Defeat,manager.State);Assert.AreEqual("DEFEAT",manager.resultUI.title.text);
        }
        [UnityTest] public IEnumerator SameFramePlayerFirstDefeatWins()
        {
            manager.player.TakeDamage(int.MaxValue);KillEnemies();yield return null;yield return null;
            Assert.AreEqual(BattleState.Defeat,manager.State);Assert.AreEqual(1,manager.resultUI.ShowCount);
        }
        [UnityTest] public IEnumerator DestroyedAliveEnemyDoesNotCountAsSunk()
        {
            Object.Destroy(manager.participants[0].gameObject);
            manager.participants[1].TakeDamage(int.MaxValue);manager.participants[2].TakeDamage(int.MaxValue);
            yield return null;yield return null;
            Assert.AreEqual(BattleState.Playing,manager.State);Assert.AreEqual(1,manager.RemainingEnemies);
        }
        [UnityTest] public IEnumerator DuplicateRosterAndZeroRoster()
        {
            var player=manager.player;var target=manager.participants[0];Object.Destroy(manager.gameObject);yield return null;
            var duplicate=new GameObject("Duplicate roster test").AddComponent<BattleManager>();duplicate.player=player;duplicate.participants=new[]{target,target};
            yield return null;Assert.AreEqual(1,duplicate.TotalEnemies);Object.Destroy(duplicate.gameObject);yield return null;
            LogAssert.Expect(LogType.Warning,"BattleManager: no participating enemies; automatic victory disabled.");
            var empty=new GameObject("Empty roster test").AddComponent<BattleManager>();empty.player=player;
            yield return null;yield return null;
            Assert.AreEqual(BattleState.Playing,empty.State);Assert.AreEqual(0,empty.TotalEnemies);
            Object.Destroy(empty.gameObject);
        }
        [UnityTest] public IEnumerator ThreeRetriesViaPausedUIButton()
        {
            var initial=manager.player.transform.position;
            for(int i=0;i<3;i++)
            {
                manager.player.Weapon.SetAim(manager.player.WorldPosition+Vector3.forward*300);
                manager.player.FireWeapon();
                manager.player.TakeDamage(int.MaxValue);yield return null;yield return null;
                Assert.AreEqual(BattleState.Defeat,manager.State);
                Assert.IsFalse(manager.resultUI.mainMenuButton.interactable);
                manager.resultUI.ReturnToMainMenu();Assert.AreEqual(0,Time.timeScale);
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(manager.resultUI.retryButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                yield return null;yield return null;yield return null;
                manager=Object.FindFirstObjectByType<BattleManager>();
                Assert.AreEqual(1,Time.timeScale);Assert.AreEqual(BattleState.Playing,manager.State);
                Assert.AreEqual(3,manager.RemainingEnemies);Assert.AreEqual(0,manager.resultUI.ShowCount);
                Assert.AreEqual(manager.player.Health.maximumHealth,manager.player.Health.CurrentHealth);
                Assert.Less(Vector3.Distance(initial,manager.player.transform.position),.1f);
                Assert.AreEqual(0,manager.player.Movement.TargetSpeed);
                Assert.AreEqual(1,Object.FindObjectsByType<PlayerShip>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(3,Object.FindObjectsByType<EnemyShip>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(1,Object.FindObjectsByType<BattleManager>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(1,Object.FindObjectsByType<ResultUI>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(0,Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length);
                Assert.IsNull(Object.FindFirstObjectByType<CombatFireControl>().Target);
                foreach(var ai in Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None)){Assert.AreEqual(EnemyState.Patrol,ai.State);ai.enabled=false;}
            }
        }
    }
}
