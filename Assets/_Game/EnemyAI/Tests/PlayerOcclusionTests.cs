using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace GrandNavigation.Gameplay.AI.Tests
{
    public class PlayerOcclusionTests
    {
        PlayerShip player;Ship target;CombatFireControl fire;CombatCamera rig;CombatAimController aim;PlayerCombatInput input;GameObject wall;
        [UnitySetUp]public IEnumerator Setup()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/EnemyAIGreybox.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
            yield return null;Application.runInBackground=true;
            player=Object.FindFirstObjectByType<PlayerShip>();fire=Object.FindFirstObjectByType<CombatFireControl>();rig=Object.FindFirstObjectByType<CombatCamera>();aim=Object.FindFirstObjectByType<CombatAimController>();input=Object.FindFirstObjectByType<PlayerCombatInput>();
            foreach(var ai in Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.InstanceID)){ai.enabled=false;ai.Ship.Movement.enabled=false;if(!target)target=ai.Ship;else ai.gameObject.SetActive(false);}
            player.GetComponent<PlayerShipInput>().enabled=false;player.Movement.enabled=false;input.enabled=false;rig.orbit.enabled=false;
            Place(player,new Vector3(0,0,-900));Place(target,new Vector3(0,1,-500));
            rig.view.transform.SetPositionAndRotation(new Vector3(0,150,-900),Quaternion.LookRotation(target.WorldPosition-new Vector3(0,150,-900)));
            fire.Lock(null);yield return null;
        }
        static void Place(Ship s,Vector3 p){var b=s.GetComponent<Rigidbody>();b.linearVelocity=Vector3.zero;b.angularVelocity=Vector3.zero;b.position=p;b.rotation=Quaternion.identity;s.transform.SetPositionAndRotation(p,Quaternion.identity);Physics.SyncTransforms();}
        void Block(float width=100)
        {wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,20,-700);wall.transform.localScale=new Vector3(width,40,15);Physics.SyncTransforms();}
        [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;if(wall)Object.Destroy(wall);yield return null;}
        [UnityTest]public IEnumerator ObserverBlocksHighCameraSelectionAndPartialHullRemainsVisible()
        {
            Assert.AreSame(target,input.SelectTargetAt(rig.view.WorldToScreenPoint(target.WorldPosition)));
            fire.Lock(null);Block();Assert.IsFalse(fire.CanObserve(target));
            Assert.IsNull(input.SelectTargetAt(rig.view.WorldToScreenPoint(target.WorldPosition)),"High camera cannot bypass bridge LOS, even for exact ray hit");
            fire.Lock(target);Assert.IsNull(fire.Target);
            wall.transform.localScale=new Vector3(1,40,15);Physics.SyncTransforms();Assert.IsTrue(fire.CanObserve(target));
            fire.partialVisibilityAllowed=false;Assert.IsFalse(fire.CanObserve(target));fire.partialVisibilityAllowed=true;
            fire.Lock(target);Assert.AreEqual(CombatFireControl.LockState.Locked,fire.State);yield return new WaitForSeconds(.2f);Assert.IsTrue(fire.HasVisibleTarget);
        }
        [UnityTest]public IEnumerator ContinuousLossGraceResetsAndPausedTimeDoesNotExpire()
        {
            fire.Lock(target);Block();yield return new WaitForSeconds(.15f);
            Assert.AreEqual(CombatFireControl.LockState.Occluded,fire.State);Assert.AreSame(target,fire.Target);Assert.IsFalse(fire.Valid);Assert.IsFalse(fire.observation.Valid);
            yield return new WaitForSeconds(.5f);wall.SetActive(false);yield return new WaitForSeconds(.15f);
            Assert.IsTrue(fire.HasVisibleTarget);Assert.AreEqual(0,fire.OcclusionAge);Assert.IsTrue(fire.observation.Valid);
            wall.SetActive(true);Physics.SyncTransforms();yield return new WaitForSeconds(.15f);
            float age=fire.OcclusionAge;Time.timeScale=0;yield return new WaitForSecondsRealtime(.25f);Assert.That(fire.OcclusionAge,Is.EqualTo(age).Within(.001f));Time.timeScale=1;
            yield return new WaitForSeconds(1.1f);Assert.AreSame(target,fire.Target);
            yield return new WaitForSeconds(.5f);Assert.IsNull(fire.Target);Assert.AreEqual(CombatFireControl.LockState.Unlocked,fire.State);
        }
        [UnityTest]public IEnumerator OccludedCameraAndPredictionDoNotTrackButManualFireWorks()
        {
            fire.Lock(target);aim.SetAiming(true);rig.SetAiming(true);yield return new WaitForSeconds(.8f);
            var chosen=aim.PlayerAimPoint;Block();yield return new WaitForSeconds(.15f);
            Assert.IsFalse(rig.TrackingTarget);Assert.AreEqual(chosen,aim.PlayerAimPoint);
            Vector3 memory=fire.LastVisiblePosition;Place(target,new Vector3(30,1,-500));
            yield return new WaitForSeconds(.2f);Assert.AreEqual(memory,fire.LastVisiblePosition);Assert.IsFalse(fire.observation.Valid);
            Assert.IsFalse(Object.FindFirstObjectByType<PredictionRegion>().GetComponent<MeshRenderer>().enabled);
            Assert.AreEqual(chosen,aim.PlayerAimPoint);
            rig.AdjustFreeAim(new Vector2(25,10));Assert.AreNotEqual(chosen,aim.PlayerAimPoint);
            Assert.IsTrue(player.FireWeapon(),"Blind manual salvo remains available");
            yield return new WaitForSeconds(1.5f);Assert.IsNull(fire.Target);Assert.IsTrue(aim.IsAiming);Assert.IsFalse(rig.TrackingTarget);
            var cameraPosition=rig.view.transform.position;var cameraRotation=rig.view.transform.rotation;yield return new WaitForSeconds(1);
            Place(target,new Vector3(-100,1,-400));yield return new WaitForSeconds(.3f);
            Assert.That(Vector3.Distance(rig.view.transform.position,cameraPosition),Is.LessThan(2));Assert.That(Quaternion.Angle(cameraRotation,rig.view.transform.rotation),Is.LessThan(2));
            wall.SetActive(false);fire.Lock(target);yield return null;Assert.IsTrue(rig.TrackingTarget);Assert.IsTrue(fire.observation.Valid);
        }
        [UnityTest]public IEnumerator CameraCannotBlendThroughCoverAndDestroyedReferenceClears()
        {
            Block(1);wall.transform.localScale=new Vector3(1,100,15);wall.transform.position=new Vector3(0,50,-700);Physics.SyncTransforms();
            Assert.IsTrue(fire.CanObserve(target),"Partial hull is visible from bridge");
            rig.view.transform.SetPositionAndRotation(new Vector3(0,10,-950),Quaternion.LookRotation(target.WorldPosition-new Vector3(0,10,-950)));
            fire.Lock(target);aim.SetAiming(true);rig.SetAiming(true);
            float until=Time.time+2;
            while(Time.time<until){Assert.Less(rig.view.transform.position.z,wall.GetComponent<Collider>().bounds.min.z,"Camera must stay on near side of cover");yield return null;}
            wall.transform.localScale=new Vector3(100,100,15);Physics.SyncTransforms();yield return new WaitForSeconds(.15f);
            Assert.AreEqual(CombatFireControl.LockState.Occluded,fire.State);
            Object.Destroy(target.gameObject);yield return null;yield return null;
            Assert.IsNull(fire.Target);Assert.AreEqual(CombatFireControl.LockState.Unlocked,fire.State);Assert.IsTrue(aim.IsAiming);
        }
        [UnityTest]public IEnumerator DeathCancelDestroyAndEachSavedIsland()
        {
            foreach(var island in Object.FindObjectsByType<GrandNavigation.Gameplay.Levels.LevelIsland>(FindObjectsSortMode.None))
            {
                var b=island.solid.GetComponent<Collider>().bounds;
                Place(player,new Vector3(b.center.x,0,b.min.z-100));Place(target,new Vector3(b.center.x,1,b.max.z+100));
                Assert.IsFalse(fire.CanObserve(target),island.name+" must block ship-level vision");
            }
            Place(player,new Vector3(0,0,-900));Place(target,new Vector3(0,1,-500));
            fire.Lock(target);Assert.IsTrue(fire.HasVisibleTarget);fire.Lock(null);Assert.IsNull(fire.Target);
            fire.Lock(target);Block();yield return new WaitForSeconds(.15f);target.TakeDamage(100000);Assert.IsNull(fire.Target,"Death clears lock synchronously, without grace");
            Object.Destroy(target.gameObject);yield return null;Assert.AreEqual(CombatFireControl.LockState.Unlocked,fire.State);
            aim.SetAiming(true);rig.SetAiming(true);Assert.IsTrue(aim.IsAiming);Assert.IsFalse(rig.TrackingTarget);
        }
    }
}
