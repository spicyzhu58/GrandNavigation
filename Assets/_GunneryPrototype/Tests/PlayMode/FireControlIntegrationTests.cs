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
    public class FireControlIntegrationTests
    {
        PrototypeSession s; WeaponManager weapons; Vector3 aimForward,aimRight;
        [UnitySetUp] public IEnumerator Setup()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_GunneryPrototype/Scenes/GunneryPrototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
            yield return null;
            s=Object.FindFirstObjectByType<PrototypeSession>(); weapons=s.GetComponent<WeaponManager>();
            s.target.SetMode(TargetMotion.MotionMode.Stationary,0); s.ResetBattle();
            s.selection.Select(s.target.GetComponent<Health>()); Assert.IsTrue(s.aim.Begin());
            aimForward=s.aim.PlayerAimPoint-s.prediction.fireControl.position; aimForward.y=0; aimForward.Normalize();
            aimRight=Vector3.Cross(Vector3.up,aimForward); weapons.dispersionEnabled=false;
            yield return new WaitForSeconds(.6f);
        }
        void CorrectTo(Vector3 point)
        {
            var correction=point-s.aim.PlayerAimPoint;
            s.aim.Adjust(new Vector2(Vector3.Dot(correction,aimRight),Vector3.Dot(correction,aimForward))/s.aim.metresPerPixel);
        }
        [UnityTest] public IEnumerator TwoSalvosFollowPlayerCorrectionAcrossSampleRefresh()
        {
            Vector3 first=new Vector3(40,0,150); CorrectTo(first);
            yield return new WaitForSeconds(.8f);
            Assert.That(weapons.TryFireSalvo(),Is.EqualTo(6));
            Assert.That(Vector3.Distance(weapons.LastSalvoCenter,first),Is.LessThan(.01));
            yield return new WaitForSeconds(3.2f);
            Assert.That(ProjectileController.SplashCount,Is.EqualTo(6));
            Assert.That(Vector3.Distance(ProjectileController.LastImpact,first),Is.LessThan(.02));
            Assert.That(Vector3.Distance(s.aim.PlayerAimPoint,first),Is.LessThan(.01));
            Vector3 second=new Vector3(60,0,155); CorrectTo(second);
            yield return new WaitForSeconds(2f);
            Assert.That(weapons.TryFireSalvo(),Is.EqualTo(6));
            yield return new WaitForSeconds(3.2f);
            Assert.That(ProjectileController.SplashCount,Is.EqualTo(12));
            Assert.That(Vector3.Distance(ProjectileController.LastImpact,second),Is.LessThan(.02));
            Assert.That(Vector3.Distance(first,second),Is.GreaterThan(20));
        }
        [UnityTest] public IEnumerator WaterColumnPersistsAndVfxNeverBlocksProjectiles()
        {
            FeedbackEffects.Burst(new Vector3(60,0,155),true,1.5f);
            var column=Object.FindObjectsByType<FeedbackEffects>(FindObjectsSortMode.None).First(x=>x.name=="Water column");
            Assert.That(column.Lifetime,Is.EqualTo(1.5));
            Assert.IsFalse(column.GetComponent<Collider>().enabled,"Collider must be off immediately, not at frame end.");
            yield return new WaitForSeconds(1.1f); Assert.IsTrue(column);
            yield return new WaitForSeconds(.5f); Assert.IsFalse(column);
        }
        [UnityTest] public IEnumerator InvalidTargetPurgesCacheAndHidesRegion()
        {
            yield return null;
            var region=GameObject.Find("Observed fire-control area (no centre marker)");
            Assert.IsNotNull(region); Assert.IsTrue(region.activeSelf);
            s.target.GetComponent<Health>().Damage(10000,s.target.transform.position);
            yield return null; yield return null;
            Assert.IsFalse(s.aim.IsAiming); Assert.IsFalse(s.prediction.Valid);
            Assert.That(s.prediction.CachedTargetCount,Is.Zero); Assert.IsFalse(region.activeSelf);
            Assert.IsTrue(BallisticSolver.Finite(s.prediction.PredictedIntercept));
        }
        [UnityTest] public IEnumerator UnlockRelockAndAimToggleKeepObservationAndCorrection()
        {
            s.aim.Adjust(new Vector2(20,10)); var saved=s.aim.PlayerAimPoint;
            var timestamp=s.prediction.SampleTimestamp; var target=s.target.GetComponent<Health>();
            s.ExitAim(); s.selection.Clear(); yield return null;
            s.selection.Select(target); Assert.IsTrue(s.aim.Begin());
            Assert.That(s.prediction.SampleTimestamp,Is.EqualTo(timestamp));
            Assert.That(s.aim.PlayerAimPoint,Is.EqualTo(saved));
            yield return new WaitForSeconds(2.6f);
            Assert.That(s.prediction.SampleTimestamp,Is.GreaterThan(timestamp));
            Assert.That(s.aim.PlayerAimPoint,Is.EqualTo(saved));
        }
    }
}
