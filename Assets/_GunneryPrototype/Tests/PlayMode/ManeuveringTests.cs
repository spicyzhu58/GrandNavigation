using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace GrandNavigation.Gunnery.Tests
{
    public class ManeuveringTests
    {
        [UnityTest] public IEnumerator DefaultTurnProducesStaleErrorThenNewObservation()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_GunneryPrototype/Scenes/GunneryPrototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
            yield return null;
            var s=Object.FindFirstObjectByType<PrototypeSession>(); LaboratoryProfile.Apply(s); var target=s.target;
            target.SetMode(TargetMotion.MotionMode.Maneuvering,10); s.ResetBattle();
            s.selection.Select(target.GetComponent<Health>()); s.prediction.Refresh();
            Assert.That(s.prediction.SampledTargetVelocity.x,Is.EqualTo(10).Within(.02));
            yield return new WaitForSeconds(5.15f);
            double stamp=s.prediction.SampleTimestamp; Vector3 oldVelocity=s.prediction.SampledTargetVelocity;
            Vector3 previous=target.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(target.Velocity.magnitude,Is.EqualTo(10).Within(.02));
            Assert.That(Vector3.Distance(previous,target.transform.position),Is.GreaterThan(.1));
            yield return new WaitForSeconds(1.05f);
            Assert.IsTrue(target.IsTurning);
            Assert.That(s.prediction.SampleTimestamp,Is.EqualTo(stamp));
            Assert.That(s.prediction.SampledTargetVelocity,Is.EqualTo(oldVelocity));
            Assert.That(Vector3.Angle(oldVelocity,target.Velocity),Is.GreaterThan(8));
            Assert.IsTrue(BallisticSolver.TryIntercept(s.prediction.fireControl.position,target.transform.position,
                target.Velocity,65,9.81f,250,out var live,out _));
            Assert.That(Vector3.Distance(live,s.prediction.PredictedIntercept),Is.GreaterThan(4));
            yield return new WaitForSeconds(1.6f);
            Assert.That(s.prediction.SampleTimestamp,Is.GreaterThan(stamp));
            Assert.That(Vector3.Angle(s.prediction.SampledTargetVelocity,target.Velocity),Is.LessThan(.1));
            Assert.IsFalse(target.IsTurning);
        }
        [UnityTest] public IEnumerator PauseSlowMotionResetAndBoundaryRemainConsistent()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_GunneryPrototype/Scenes/GunneryPrototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
            yield return null;
            var s=Object.FindFirstObjectByType<PrototypeSession>(); LaboratoryProfile.Apply(s);
            s.target.SetMode(TargetMotion.MotionMode.Maneuvering,10); s.ResetBattle();
            s.selection.Select(s.target.GetComponent<Health>()); s.prediction.Refresh();
            double stamp=s.prediction.SampleTimestamp; Vector3 point=s.target.transform.position;
            try
            {
                Time.timeScale=0; yield return new WaitForSecondsRealtime(.15f);
                Assert.That(s.prediction.SampleTimestamp,Is.EqualTo(stamp)); Assert.That(s.target.transform.position,Is.EqualTo(point));
                Time.timeScale=.25f; yield return new WaitForSecondsRealtime(.4f);
                Assert.That(s.prediction.SampleAge,Is.LessThan(.2)); Assert.That(s.prediction.SampleTimestamp,Is.EqualTo(stamp));
                Time.timeScale=1;
                s.target.spawnPosition=new Vector3(184.7f,0,160); s.ResetBattle();
                yield return new WaitForSeconds(.2f);
                Assert.That(s.target.transform.position.x,Is.LessThan(185));
                Assert.That(s.target.Velocity.x,Is.LessThan(0));
                var before=s.target.transform.position;
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                Assert.That((s.target.transform.position-before).normalized.x,Is.LessThan(-.99));
            }
            finally { Time.timeScale=1; }
        }
    }
}
