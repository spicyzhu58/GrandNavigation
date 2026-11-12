using System.Collections;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using GrandNavigation.Gameplay.Battle;
namespace GrandNavigation.Battle2D.Tests
{
 public class Battle2DTests
 {
  BattleManager2D manager;
  [UnitySetUp] public IEnumerator Setup(){Time.timeScale=1;Application.runInBackground=true;yield return SceneManager.LoadSceneAsync("Assets/_Game/Scenes/Battle2D.unity");yield return null;yield return null;manager=Object.FindFirstObjectByType<BattleManager2D>();}
  [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;yield return null;}
  IEnumerator Begin(){ExecuteEvents.Execute(manager.startButton.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);yield return null;Assert.IsTrue(manager.Started);foreach(var ai in Object.FindObjectsByType<EnemyAI2D>(FindObjectsSortMode.None))ai.enabled=false;manager.player.GetComponent<PlayerInput2D>().enabled=false;}
  [UnityTest] public IEnumerator ResultsAndThreeRetries()
  {
   for(int round=0;round<3;round++){
    Assert.IsFalse(manager.Started);Assert.AreEqual(4,Object.FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None).Length);Assert.AreEqual(0,Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length);
    yield return Begin();manager.enemies[0].TakeDamage(9999);yield return null;Assert.AreEqual(BattleState.Playing,manager.State);Assert.AreEqual(2,manager.Remaining);
    foreach(var enemy in manager.enemies){enemy.TakeDamage(9999);enemy.TakeDamage(9999);Assert.AreEqual(GrandNavigation.Gameplay.AI.EnemyState.Destroyed,enemy.GetComponent<EnemyAI2D>().State);}
    if(round==1)manager.player.TakeDamage(9999);
    yield return null;yield return null;Assert.AreEqual(round==1?BattleState.Defeat:BattleState.Victory,manager.State);Assert.AreEqual(1,manager.EndCount);Assert.IsTrue(manager.resultPanel.activeSelf);Assert.AreEqual(0,Time.timeScale);
    var old=manager;ExecuteEvents.Execute(manager.retryButton.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
    float timeout=Time.realtimeSinceStartup+10;while(old&&Time.realtimeSinceStartup<timeout)yield return null;Assert.IsFalse(old);yield return null;yield return null;manager=Object.FindFirstObjectByType<BattleManager2D>();Assert.AreEqual(1,Time.timeScale);Assert.AreEqual(600,manager.player.Health.CurrentHealth);Assert.AreEqual(1,Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
   }
  }
  [UnityTest] public IEnumerator RealShellDamageReloadAndIslandOcclusion()
  {
   yield return Begin();var player=manager.player;var target=System.Array.Find(manager.enemies,e=>e.kind==ShipKind2D.Battleship);player.Movement.Stop();target.Movement.Stop();player.Movement.Body.position=new Vector2(0,-130);target.Movement.Body.position=new Vector2(0,-90);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
   var weapon=player.GetComponent<Weapon2D>();weapon.scatterSigma=0;int hp=target.Health.CurrentHealth;
   Assert.IsTrue(weapon.Fire(new Vector2(0,-90)));Assert.AreEqual(6,weapon.LastSalvo);Assert.IsFalse(weapon.Fire(new Vector2(0,-90)));yield return new WaitForSeconds(.8f);Assert.AreEqual(hp-150,target.Health.CurrentHealth);
   yield return new WaitForSeconds(3);player.Movement.Body.position=new Vector2(-65,-60);target.Movement.Body.position=new Vector2(-65,60);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();hp=target.Health.CurrentHealth;Assert.IsTrue(weapon.Fire(new Vector2(-65,60)));yield return new WaitForSeconds(2);Assert.AreEqual(hp,target.Health.CurrentHealth);Assert.AreEqual(0,Object.FindObjectsByType<Projectile2D>(FindObjectsSortMode.None).Length);
  }
  [UnityTest] public IEnumerator MovementReverseTurnAndIslandCollision()
  {
   yield return Begin();var movement=manager.player.Movement;movement.Body.position=new Vector2(-65,-65);movement.Drive(15,0);yield return new WaitForSeconds(1);Assert.Greater(movement.Speed,0);Assert.Less(movement.Speed,15);yield return new WaitForSeconds(9);Assert.Less(movement.Body.position.y,-27);Assert.Less(Mathf.Abs(movement.Speed),1);
   movement.Body.position=new Vector2(0,-130);movement.Stop();movement.Drive(-4,1);yield return new WaitForSeconds(2);Assert.Less(movement.Speed,0);Assert.Greater(movement.Body.angularVelocity,0);movement.Drive(0,0);yield return new WaitForSeconds(3);Assert.Less(Mathf.Abs(movement.Rudder),.1f);
  }

  [UnityTest] public IEnumerator RealInputThrottleSteeringLockFireAndZoom()
  {
   var background=InputSystem.settings.backgroundBehavior;var routing=InputSystem.settings.editorInputBehaviorInPlayMode;
   InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
   var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
   try {
    yield return Begin();var input=manager.player.GetComponent<PlayerInput2D>();input.enabled=true;
    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D));yield return new WaitForSeconds(.5f);Assert.Greater(manager.player.Movement.TargetSpeed,1);Assert.Greater(manager.player.Movement.Rudder,0);
    InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;float held=manager.player.Movement.TargetSpeed;yield return new WaitForSeconds(.5f);Assert.AreEqual(held,manager.player.Movement.TargetSpeed,.01f);Assert.Less(Mathf.Abs(manager.player.Movement.Rudder),.1f);
    manager.player.Movement.Stop();manager.player.Movement.Body.rotation=0;
    var target=manager.enemies[0];target.Movement.Stop();target.Movement.Body.position=manager.player.Movement.Body.position+Vector2.up*40;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
    var point=(Vector2)Camera.main.WorldToScreenPoint(target.transform.position);InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Right));yield return null;yield return null;Assert.AreEqual(target,input.Target);
    InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Left));yield return null;yield return null;Assert.AreEqual(6,manager.player.GetComponent<Weapon2D>().LastSalvo);
    float zoom=Camera.main.orthographicSize;InputSystem.QueueStateEvent(mouse,new MouseState{position=point,scroll=new Vector2(0,1)});yield return new WaitForSeconds(.4f);Assert.Less(Camera.main.orthographicSize,zoom);
   } finally {InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=routing;}
  }
  [UnityTest] public IEnumerator BothEnemyTypesMoveAndFireRealProjectiles()
  {
   manager.enemies=System.Linq.Enumerable.OrderBy(manager.enemies,e=>e.kind==ShipKind2D.Battleship?0:1).ToArray();
   yield return Begin();var p=manager.player;p.Movement.Body.position=new Vector2(0,-130);
   manager.enemies[0].Movement.Body.position=new Vector2(-35,-65);manager.enemies[1].Movement.Body.position=new Vector2(35,-65);manager.enemies[2].gameObject.SetActive(false);
   foreach(var e in new[]{manager.enemies[0],manager.enemies[1]}){e.GetComponent<EnemyAI2D>().enabled=true;e.GetComponent<Weapon2D>().scatterSigma=0;}
   yield return new WaitForSeconds(1);Assert.Greater(manager.enemies[0].Movement.Body.linearVelocity.magnitude,0);Assert.Greater(manager.enemies[1].Movement.Body.linearVelocity.magnitude,0);
   yield return new WaitForSeconds(8);Assert.Greater(manager.enemies[0].GetComponent<Weapon2D>().LastSalvo,0);Assert.Greater(manager.enemies[1].GetComponent<Weapon2D>().LastSalvo,0);Assert.Less(p.Health.CurrentHealth,600);
  }
 }
}


