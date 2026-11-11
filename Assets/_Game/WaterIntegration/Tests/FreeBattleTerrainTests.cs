using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using GrandNavigation.Gameplay.AI;
namespace GrandNavigation.Gameplay.Water.Tests {
public class FreeBattleTerrainTests {
[UnityTest]public IEnumerator ShipsNavigateAndResetOnNewTerrain(){
EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/FreeNavalBattle.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;
var ai=Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None);Assert.AreEqual(3,ai.Length);
var starts=ai.Select(a=>a.transform.position).ToArray();
var colliders=Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.name=="Island collision sector").ToArray();Assert.AreEqual(24,colliders.Length);Assert.IsTrue(colliders.All(c=>c.convex));
yield return new WaitForSeconds(8);
for(int i=0;i<ai.Length;i++){Assert.IsTrue(ai[i].Ship.IsAlive);Assert.Greater(Vector3.Distance(starts[i],ai[i].transform.position),3);Assert.IsTrue(float.IsFinite(ai[i].transform.position.x));}
var cam=Camera.main;Object.FindFirstObjectByType<ShipFollowCamera>().enabled=false;Object.FindFirstObjectByType<CombatCamera>().enabled=false;var focus=GameObject.Find("Island_A").transform.position;cam.transform.position=focus+new Vector3(180,110,-180);cam.transform.LookAt(focus+Vector3.up*12);yield return null;var rt=new RenderTexture(1600,900,24);cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();System.IO.File.WriteAllBytes("Artifacts/FreeBattle/play-preview.png",tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;Object.Destroy(tex);rt.Release();Object.Destroy(rt);var debug=Object.FindFirstObjectByType<EnemyAIDebug>();debug.ResetEnemy(0);yield return null;Assert.AreEqual(4,debug.slots[0].instance.waypoints.Length);Assert.Less(Vector3.Distance(debug.slots[0].position,debug.slots[0].instance.transform.position),3);
}
}}

