using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using GrandNavigation.Gameplay;
using GrandNavigation.Gameplay.AI;
using GrandNavigation.Gameplay.Water;
using GrandNavigation.Seamless;
namespace GrandNavigation.FinalIntegration.Tests
{
 public class FinalCombatTests
 {
  GameFlowController flow;
  [UnitySetUp] public IEnumerator Setup(){Time.timeScale=1;Application.runInBackground=true;yield return SceneManager.LoadSceneAsync("Assets/_Game/Scenes/FinalIntegratedGame.unity");yield return null;yield return null;flow=Object.FindFirstObjectByType<GameFlowController>();flow.rig.duration=.1f;flow.goDuration=.1f;flow.StartPreparation();yield return new WaitForSeconds(.2f);flow.Go();yield return new WaitForSeconds(.3f);Assert.AreEqual(GameStage.Playing,flow.Stage);
   foreach(var ai in Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None))ai.enabled=false;
   flow.battle.player.GetComponent<PlayerShipInput>().enabled=false;Object.FindFirstObjectByType<PlayerCombatInput>().enabled=false;Object.FindFirstObjectByType<CombatAimController>().enabled=false;
   foreach(var ship in Object.FindObjectsByType<Ship>(FindObjectsSortMode.None)){ship.Movement.enabled=false;ship.GetComponent<Rigidbody>().isKinematic=true;ship.Weapon.dispersionEnabled=false;}
  }
  [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;yield return null;}
  static void Place(Ship s,Vector3 p){s.GetComponent<Rigidbody>().position=p;s.transform.SetPositionAndRotation(p,Quaternion.identity);Physics.SyncTransforms();}
  [UnityTest] public IEnumerator ModelBatteriesRealDamagePredictionAndOcclusion()
  {
   var p=flow.battle.player;var target=flow.battle.participants.OfType<Battleship>().First();Place(p,new Vector3(0,0,-900));Place(target,new Vector3(400,1,-900));
   foreach(var s in flow.battle.participants)Assert.AreEqual(s is Battleship?4:3,s.Weapon.turrets.Length);
   p.Weapon.SetAim(target.WorldPosition);foreach(var t in p.Weapon.turrets){for(int i=0;i<4;i++)t.Track(60);Assert.IsTrue(t.Aligned);Assert.AreEqual(2,t.muzzles.Length);foreach(var m in t.muzzles)Assert.Greater(m.position.y,0);}
   int hp=target.Health.CurrentHealth,before=Projectile.SpawnedCount;Assert.IsTrue(p.FireWeapon());Assert.AreEqual(8,Projectile.SpawnedCount-before);Assert.IsFalse(p.FireWeapon());yield return new WaitForSeconds(4);Assert.Less(target.Health.CurrentHealth,hp);
   var fc=Object.FindFirstObjectByType<CombatFireControl>();var region=Object.FindFirstObjectByType<PredictionRegion>();var hud=Object.FindFirstObjectByType<CombatHUD>();fc.Lock(target);yield return new WaitForSeconds(.2f);Assert.IsTrue(hud.PredictionVisible);Assert.AreSame(region,hud.predictionRegion);Assert.IsFalse(region.GetComponent<MeshRenderer>().enabled);
   var aim=Object.FindFirstObjectByType<CombatAimController>();aim.SetAiming(true);yield return null;Assert.IsTrue(aim.IsAiming);
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(200,50,-900);wall.transform.localScale=new Vector3(20,100,120);Physics.SyncTransforms();yield return new WaitForSeconds(.2f);Assert.AreEqual(CombatFireControl.LockState.Occluded,fc.State);Assert.IsFalse(hud.PredictionVisible);
   wall.SetActive(false);Physics.SyncTransforms();yield return new WaitForSeconds(.2f);Assert.IsTrue(hud.PredictionVisible);
   wall.SetActive(true);Physics.SyncTransforms();yield return new WaitForSeconds(1.8f);Assert.IsNull(fc.Target);Assert.IsFalse(hud.PredictionVisible);Object.Destroy(wall);
  }
  [UnityTest] public IEnumerator WaterIslandsAndOriginalPatrol()
  {
   var water=Object.FindFirstObjectByType<WaterSceneContext>();Assert.IsTrue(water.enabled);Assert.IsTrue(water.fft.Ready);Assert.Greater(water.fft.FrameCount,0);Assert.IsTrue(water.waterMaterial.shader.isSupported);
   var sectors=Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.name=="Island collision sector").ToArray();Assert.AreEqual(24,sectors.Length);foreach(var c in sectors)Assert.IsTrue(c.convex);
   foreach(string name in new[]{"Island_A","Island_B","Island_C"}){var root=GameObject.Find(name);var cs=root.GetComponentsInChildren<MeshCollider>();var start=root.transform.position+new Vector3(-400,2,0);Assert.IsTrue(cs.Any(c=>c.Raycast(new Ray(start,Vector3.right),out _,800)),name+" blocks near-waterline rays");}
   int impacts=water.WaterImpactCount;CombatEffects.Splash(flow.battle.player.WorldPosition+Vector3.right*30);Assert.AreEqual(impacts+1,water.WaterImpactCount);
   var ai=Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None).First();ai.enabled=true;ai.Ship.Movement.enabled=true;ai.GetComponent<Rigidbody>().isKinematic=false;var pos=ai.transform.position;yield return new WaitForSeconds(3);Assert.Greater(Vector3.Distance(pos,ai.transform.position),.1f);Assert.IsTrue(ai.Ship.IsAlive);
  }
 }
}
