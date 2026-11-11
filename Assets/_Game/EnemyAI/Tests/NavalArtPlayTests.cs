using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace GrandNavigation.Gameplay.AI.Tests
{
    public class NavalArtPlayTests
    {
        Ship[] ships;
        [UnitySetUp] public IEnumerator Setup()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/TropicalNavalBattle.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null;Application.runInBackground=true;
            foreach(var ai in Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None))ai.enabled=false;
            Object.FindFirstObjectByType<PlayerCombatInput>().enabled=false;
            Object.FindFirstObjectByType<PlayerShipInput>().enabled=false;
            Object.FindFirstObjectByType<CombatAimController>().enabled=false;
            ships=Object.FindObjectsByType<Ship>(FindObjectsSortMode.None);
            foreach(var ship in ships){ship.Movement.enabled=false;ship.GetComponent<Rigidbody>().isKinematic=true;ship.Weapon.dispersionEnabled=false;}
            yield return null;
        }
        void Aim(Ship ship,Vector3 point)
        {ship.Weapon.SetAim(point);for(int i=0;i<4;i++)foreach(var t in ship.Weapon.turrets)t.Track(60);}
        [UnityTest] public IEnumerator EveryMainTurretTraversesElevatesAndFiresBothBroadsides()
        {
            Assert.AreEqual(4,ships.Length);
            foreach(var ship in ships)
            {
                foreach(var s in ships)s.gameObject.SetActive(s==ship);
                ship.transform.SetPositionAndRotation(new Vector3(0,0,-900),Quaternion.identity);
                int count=ship is Destroyer?3:4;Assert.AreEqual(count,ship.Weapon.turrets.Length);
                foreach(int side in new[]{1,-1})
                {
                    Aim(ship,ship.transform.position+Vector3.right*side*400);
                    foreach(var t in ship.Weapon.turrets)
                    {
                        Assert.IsTrue(t.Aligned,ship.name+" "+t.name+" aligns");
                        Assert.Less(Mathf.Abs(Mathf.DeltaAngle(t.LocalYaw,side*90)),4);
                        foreach(var b in t.barrels)
                        {
                            Assert.Greater(-Mathf.DeltaAngle(0,b.localEulerAngles.x),1,"Barrel elevation changes");
                            Assert.Greater(b.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount,20);
                        }
                        foreach(var m in t.muzzles)Assert.Greater(m.position.y,0,"Muzzle is above sea level");
                    }
                    int before=Projectile.SpawnedCount;
                    Assert.IsTrue(ship is PlayerShip?ship.Weapon.TryFire():ship.Weapon.TryFireAimed(t=>t.Aligned));
                    Assert.AreEqual(count*2,Projectile.SpawnedCount-before,"Every barrel emits a real projectile");
                    Assert.IsFalse(ship.Weapon.TryFire(),"Reload gate must prevent immediate repeat shots");
                    foreach(var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                    {Assert.Greater(Vector3.Dot(p.InitialVelocity,Vector3.right*side),100);Object.Destroy(p.gameObject);}
                    yield return new WaitForSeconds(ship.Weapon.turrets.Max(t=>t.reloadSeconds)+.05f);
                }
            }
        }
        [UnityTest] public IEnumerator ImportedGunsCauseDamageAndResetRetainsModels()
        {
            var player=ships.OfType<PlayerShip>().Single();var target=ships.OfType<Destroyer>().First();
            foreach(var s in ships)s.gameObject.SetActive(s==player||s==target);
            player.transform.SetPositionAndRotation(new Vector3(0,0,-900),Quaternion.identity);
            target.transform.SetPositionAndRotation(new Vector3(400,1,-900),Quaternion.identity);Physics.SyncTransforms();
            Aim(player,target.transform.position);int health=target.Health.CurrentHealth;
            Assert.IsTrue(player.Weapon.TryFire());yield return new WaitForSeconds(4);
            Assert.Less(target.Health.CurrentHealth,health,"Real shells from imported muzzles hit and damage target");
            var debug=Object.FindFirstObjectByType<EnemyAIDebug>();debug.ResetEnemy(0);yield return null;
            Assert.AreEqual(4,debug.slots[0].instance.GetComponent<Weapon>().turrets.Length);
            Assert.IsTrue(debug.slots[0].instance.transform.Find("VisualModel/Hull"));
            Assert.IsTrue(debug.slots[0].instance.waypoints.Length>0);
        }
        [UnityTest] public IEnumerator IslandsSupportExistingNavigationAndBlockShells()
        {
            var islands=Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.name=="SandyCoastAndHighland").ToArray();
            Assert.AreEqual(3,islands.Length);
            foreach(var island in islands)
            {
                Assert.IsTrue(island.convex,"Existing EnemyNavigation.ClosestPoint requires convex obstacles");
                var centre=island.bounds.center;var start=centre+Vector3.left*300;start.y=2;
                Assert.IsTrue(island.Raycast(new Ray(start,Vector3.right),out var hit,600),"Island blocks a near-waterline projectile path");
                Assert.That(island.ClosestPoint(start).x,Is.InRange(island.bounds.min.x-.01f,island.bounds.max.x+.01f));
            }
            var ai=Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None).First();
            ai.enabled=true;ai.Ship.Movement.enabled=true;ai.GetComponent<Rigidbody>().isKinematic=false;
            yield return new WaitForSeconds(2);
            Assert.IsTrue(ai.Ship.IsAlive);
        }
    }
}
