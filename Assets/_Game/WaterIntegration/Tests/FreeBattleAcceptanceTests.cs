using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using GrandNavigation.Gameplay.AI;
namespace GrandNavigation.Gameplay.Water.Tests {
public class FreeBattleAcceptanceTests {
[UnityTest] public IEnumerator PredictionTracksVisibleLockAndHidesOnLoss(){
EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/FreeNavalBattle.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;
foreach(var ai in Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None))ai.enabled=false;
var player=Object.FindFirstObjectByType<PlayerShip>();var target=Object.FindObjectsByType<Ship>(FindObjectsSortMode.None).First(s=>s!=player);
foreach(var ship in Object.FindObjectsByType<Ship>(FindObjectsSortMode.None)){ship.Movement.enabled=false;ship.GetComponent<Rigidbody>().isKinematic=true;}
player.transform.position=new Vector3(0,0,-900);target.transform.position=new Vector3(120,0,-900);Physics.SyncTransforms();
var fc=Object.FindFirstObjectByType<CombatFireControl>();var hud=Object.FindFirstObjectByType<CombatHUD>();fc.Lock(target);yield return new WaitForSeconds(.6f);Assert.IsTrue(hud.PredictionVisible,"Yellow HUD overlay available for valid visible target");Assert.Greater(fc.PredictedIntercept.x,100);
var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=new Vector3(60,8,-900);blocker.transform.localScale=new Vector3(10,40,80);Physics.SyncTransforms();yield return new WaitForSeconds(.25f);Assert.IsFalse(hud.PredictionVisible,"No prediction revealed through island/cover");Object.Destroy(blocker);yield return new WaitForSeconds(.3f);fc.Lock(target);yield return null;Assert.IsTrue(hud.PredictionVisible);fc.Lock(null);yield return null;Assert.IsFalse(hud.PredictionVisible);
}
[UnityTest]public IEnumerator BattleSoakAndRenderBudget(){
EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/FreeNavalBattle.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;
var water=Object.FindFirstObjectByType<WaterSceneContext>();var cam=water.view;var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;var driver=cam.gameObject.AddComponent<BatchWaterCamera>();driver.view=cam;
float deadline=Time.time+45;int frames=0;double total=0;
while(Time.time<deadline){var timer=System.Diagnostics.Stopwatch.StartNew();cam.Render();timer.Stop();total+=timer.Elapsed.TotalMilliseconds;frames++;yield return null;Assert.IsTrue(Object.FindObjectsByType<Ship>(FindObjectsSortMode.None).All(s=>float.IsFinite(s.transform.position.x)&&float.IsFinite(s.transform.position.z)));}
Assert.Greater(water.fft.FrameCount,30);Assert.IsNotNull(water.simulation.StateTexture);Assert.AreEqual(3,water.islands.Length);Assert.Greater(Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Length,30);
System.IO.File.WriteAllText("Artifacts/FreeBattle/acceptance-performance.txt",$"45 seconds real-time AI/GPU soak completed. {frames} explicit 1280x720 renders; CPU Camera.Render mean {total/frames:F2} ms. This measures render submission, not GPU frame time or a standalone FPS guarantee. Allocated managed/native memory: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576} MiB. GPU {SystemInfo.graphicsDeviceName}.");
Object.Destroy(driver);cam.targetTexture=null;rt.Release();Object.Destroy(rt);
}
}}
