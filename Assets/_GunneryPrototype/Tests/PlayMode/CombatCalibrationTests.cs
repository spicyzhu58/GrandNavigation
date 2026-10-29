using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
namespace GrandNavigation.Gunnery.Tests
{
    public class CombatCalibrationTests
    {
        PrototypeSession s;
        [UnitySetUp] public IEnumerator Setup()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/_GunneryPrototype/Scenes/GunneryPrototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null; s=Object.FindFirstObjectByType<PrototypeSession>(); s.ResetBattle();
            Assert.That(s.targets.Length,Is.EqualTo(3)); Assert.IsTrue(s.prediction.useOpticalObservations);
            Assert.That(s.prediction.observationLatency,Is.EqualTo(2)); Assert.That(s.prediction.fireControlUpdateInterval,Is.EqualTo(4.5));
        }
        [UnityTest] public IEnumerator AcquisitionRelockManualCorrectionTwoSalvosAndAllTargetReset()
        {
            var hp=s.targets[0].GetComponent<Health>(); s.selection.Select(hp);
            Assert.IsFalse(s.aim.Begin()); Assert.IsTrue(s.prediction.IsAcquiring);
            yield return new WaitForSeconds(1);
            s.selection.Clear(); s.selection.Select(hp);
            Assert.IsFalse(s.aim.Begin(),"Relock must not bypass pending observation delivery");
            yield return new WaitForSeconds(1.1f); Assert.IsTrue(s.aim.Begin());
            s.aim.Adjust(new Vector2(80,-30)); var first=s.aim.PlayerAimPoint;
            var weapons=s.GetComponent<WeaponManager>(); yield return new WaitForSeconds(.8f);
            Assert.That(weapons.TryFireSalvo(),Is.EqualTo(6));
            Assert.That(weapons.LastSalvoCenter,Is.EqualTo(first));
            Assert.That(Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None).Length,Is.EqualTo(6));
            foreach(var shell in Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None))
                Assert.That(shell.InitialVelocity.magnitude,Is.EqualTo(65).Within(.001));
            Assert.That(weapons.TryFireSalvo(),Is.Zero);
            yield return new WaitForSeconds(5.2f);
            Assert.That(s.prediction.SampleCount,Is.GreaterThanOrEqualTo(2));
            Assert.That(s.aim.PlayerAimPoint,Is.EqualTo(first));
            Assert.That(ProjectileController.HitCount+ProjectileController.SplashCount,Is.EqualTo(6));
            s.aim.Adjust(new Vector2(-40,20)); var second=s.aim.PlayerAimPoint;
            s.ExitAim(); Assert.IsTrue(s.aim.Begin()); Assert.That(s.aim.PlayerAimPoint,Is.EqualTo(second));
            yield return new WaitForSeconds(.6f); Assert.That(weapons.TryFireSalvo(),Is.EqualTo(6));
            Assert.That(weapons.LastSalvoCenter,Is.EqualTo(second)); Assert.That(Vector3.Distance(first,second),Is.GreaterThan(5));
            yield return new WaitForSeconds(4);
            Assert.That(ProjectileController.HitCount+ProjectileController.SplashCount,Is.EqualTo(12));
            s.CycleTarget(); Assert.That(s.ActiveTarget,Is.EqualTo(s.targets[1])); Assert.IsFalse(s.aim.IsAiming);
            foreach(var t in s.targets) t.GetComponent<Health>().Damage(10000,t.transform.position);
            yield return null; yield return null; Assert.IsFalse(s.selection.HasTarget);
            s.ResetBattle(); yield return null;
            Assert.That(s.prediction.CachedTargetCount,Is.Zero);
            foreach(var t in s.targets) Assert.That(t.GetComponent<Health>().CurrentHP,Is.EqualTo(600));
        }
        struct Forecast { public int ship; public float due; public Vector3 point; }
        [UnityTest] public IEnumerator ThreeProfilesRemainBoundedAndCenterOnlyForecastHasNaturalError()
        {
            var buffers=new[]{new FireControlObservationBuffer(),new FireControlObservationBuffer(),new FireControlObservationBuffer()};
            var queue=new List<Forecast>(); float[] sums=new float[3],maxErrors=new float[3];
            int[] count=new int[3]; float[] minSpeed={100,100,100},maxSpeed=new float[3];
            float greatestTurn=0; float started=Time.time, nextForecast=7;
            try
            {
                Time.timeScale=3;
                while(Time.time-started<32)
                {
                    yield return new WaitForFixedUpdate(); float now=Time.time-started;
                    for(int i=0;i<3;i++)
                    {
                        var t=s.targets[i]; var pos=t.transform.position;
                        Assert.That(pos.x,Is.InRange(-95.1f,95.1f)); Assert.That(pos.z,Is.InRange(t.patrolZ.x-.1f,t.patrolZ.y+.1f));
                        minSpeed[i]=Mathf.Min(minSpeed[i],t.CurrentSpeed);maxSpeed[i]=Mathf.Max(maxSpeed[i],t.CurrentSpeed);
                        buffers[i].Update(now,4.5f,2,true,8,2,s.prediction.fireControl.position,()=>pos,
                            ()=>throw new System.Exception("Optical prediction may not read velocity"));
                    }
                    greatestTurn=Mathf.Max(greatestTurn,Mathf.Abs(Mathf.DeltaAngle(90,s.targets[2].transform.eulerAngles.y)));
                    if(now>=nextForecast)
                    {
                        for(int i=0;i<3;i++) if(buffers[i].Track.Predict(now,s.prediction.fireControl.position,65,9.81f,250,out var point,out var flight))
                            queue.Add(new Forecast{ship=i,due=now+flight,point=point});
                        nextForecast+=1;
                    }
                    for(int j=queue.Count-1;j>=0;j--) if(now>=queue[j].due)
                    {
                        var f=queue[j]; float error=Vector3.Distance(f.point,s.targets[f.ship].transform.position);
                        sums[f.ship]+=error; maxErrors[f.ship]=Mathf.Max(maxErrors[f.ship],error);count[f.ship]++;queue.RemoveAt(j);
                    }
                }
            }
            finally { Time.timeScale=1; }
            Assert.That(maxSpeed[0]-minSpeed[0],Is.LessThan(.01));
            Assert.That(maxSpeed[1]-minSpeed[1],Is.GreaterThan(5)); Assert.That(maxSpeed[2]-minSpeed[2],Is.GreaterThan(5));
            Assert.That(greatestTurn,Is.GreaterThan(20));
            for(int i=0;i<3;i++)
            {
                Assert.That(count[i],Is.GreaterThan(10));
                Debug.Log($"CALIBRATION ship={i} forecasts={count[i]} mean-center-error={sums[i]/count[i]:F2}m max={maxErrors[i]:F2}m");
            }
            Assert.That(sums[1]/count[1],Is.GreaterThan(3),"Throttle must produce observable, natural prediction error.");
            Assert.That(sums[2]/count[2],Is.GreaterThan(3),"Maneuvers must produce observable, natural prediction error.");
        }
    }
}
