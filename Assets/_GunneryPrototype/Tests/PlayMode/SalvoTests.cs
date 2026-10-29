using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace GrandNavigation.Gunnery.Tests
{
    public class SalvoTests
    {
        PrototypeSession session; WeaponManager weapons; Health hp;
        [UnitySetUp] public IEnumerator Setup()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_GunneryPrototype/Scenes/GunneryPrototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
            yield return null;
            session=Object.FindFirstObjectByType<PrototypeSession>();
            Assert.IsNotNull(session); weapons=session.GetComponent<WeaponManager>(); hp=session.target.GetComponent<Health>();
            session.target.SetMode(TargetMotion.MotionMode.Stationary,0); session.ResetBattle();
            session.selection.Select(hp); Assert.IsTrue(session.aim.Begin());
            weapons.dispersionEnabled=false;
            yield return new WaitForSeconds(.6f);
        }
        [UnityTest] public IEnumerator SixRealMuzzlesFixedSpeedCooldownAndDamage()
        {
            Assert.That(weapons.TryFireSalvo(),Is.EqualTo(6));
            var shells=Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None);
            Assert.That(shells.Length,Is.EqualTo(6));
            var muzzles=weapons.turrets.SelectMany(t=>t.muzzles).ToArray();
            foreach(var shell in shells)
            {
                Assert.That(shell.InitialVelocity.magnitude,Is.EqualTo(65).Within(.001));
                Assert.IsTrue(muzzles.Any(m=>Vector3.Distance(m.position,shell.Origin)<.001f));
            }
            Assert.That(weapons.TryFireSalvo(),Is.Zero);
            yield return new WaitForSeconds(3.2f);
            Assert.That(hp.HitCount,Is.EqualTo(6)); Assert.That(hp.CurrentHP,Is.EqualTo(360));
            Assert.That(weapons.TryFireSalvo(),Is.Zero);
            yield return new WaitForSeconds(2f);
            Assert.That(weapons.TryFireSalvo(),Is.EqualTo(6));
        }
        [UnityTest] public IEnumerator MovingLeadHitsAndCameraCannotMoveAim()
        {
            session.target.SetMode(TargetMotion.MotionMode.Diagonal,12);
            session.ResetBattle(); session.selection.Select(hp); Assert.IsTrue(session.aim.Begin());
            yield return new WaitForSeconds(.5f);
            // V0.2: explicit player correction, not the old re-entry snap-to-answer shortcut.
            Vector3 forward=session.aim.PlayerAimPoint-session.prediction.fireControl.position; forward.y=0; forward.Normalize();
            Vector3 right=Vector3.Cross(Vector3.up,forward);
            Vector3 correction=session.prediction.PredictedIntercept-session.aim.PlayerAimPoint;
            session.aim.Adjust(new Vector2(Vector3.Dot(correction,right),Vector3.Dot(correction,forward))/session.aim.metresPerPixel);
            yield return new WaitForSeconds(.08f);
            Assert.That(weapons.TryFireSalvo(),Is.EqualTo(6));
            var point=session.aim.PlayerAimPoint;
            session.cameraRig.Zoom(240);
            yield return new WaitForSeconds(3.3f);
            Assert.That(session.aim.PlayerAimPoint,Is.EqualTo(point));
            Assert.That(Vector3.Distance(session.prediction.PredictedIntercept,point),Is.GreaterThan(10));
            Assert.That(hp.HitCount,Is.GreaterThanOrEqualTo(4));
        }
        [UnityTest] public IEnumerator TraversingAndOutOfRangePreventAbnormalLaunch()
        {
            foreach(var t in weapons.turrets) t.yawBase.rotation=Quaternion.Euler(0,180,0);
            Assert.That(weapons.TryFireSalvo(),Is.Zero);
            session.aim.Adjust(new Vector2(100000,100000));
            Assert.IsFalse(session.aim.HasBallisticSolution());
            Assert.That(weapons.TryFireSalvo(),Is.Zero);
            Assert.That(Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None).Length,Is.Zero);
            yield return null;
        }
        [UnityTest] public IEnumerator DispersionUsesCachedCenterAndFixedSpeed()
        {
            weapons.dispersionEnabled=true; weapons.dispersionRadius=3;
            Assert.That(weapons.TryFireSalvo(),Is.EqualTo(6));
            Vector3 center=weapons.LastSalvoCenter;
            foreach(var shell in Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None))
            {
                Vector3 v=shell.InitialVelocity; float g=session.prediction.gravity;
                float t=(v.y+Mathf.Sqrt(v.y*v.y+2*g*shell.Origin.y))/g;
                Vector3 landing=BallisticSolver.Position(shell.Origin,v,g,t);
                Assert.That(Vector3.Distance(landing,center),Is.LessThanOrEqualTo(3.01));
                Assert.That(v.magnitude,Is.EqualTo(65).Within(.001));
            }
            session.aim.Adjust(new Vector2(30,40));
            Assert.That(weapons.LastSalvoCenter,Is.EqualTo(center));
            yield return null;
        }
        [UnityTest] public IEnumerator DeathExitsAimAndResetRestoresTargetAndReload()
        {
            Assert.That(weapons.TryFireSalvo(),Is.EqualTo(6));
            hp.Damage(10000,hp.transform.position);
            yield return null; yield return null;
            Assert.IsFalse(session.aim.IsAiming); Assert.IsFalse(session.selection.HasTarget);
            session.ResetBattle();
            yield return null;
            Assert.That(hp.CurrentHP,Is.EqualTo(600));
            Assert.That(session.target.transform.position.y,Is.EqualTo(0).Within(.01));
            Assert.IsTrue(session.target.GetComponentsInChildren<Collider>().All(c=>c.enabled));
            Assert.IsTrue(weapons.turrets.All(t=>t.ReloadRemaining==0));
            Assert.That(Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None).Length,Is.Zero);
        }
    }
}