using NUnit.Framework;
using UnityEngine;
namespace GrandNavigation.Gunnery.Tests
{
    public class FireControlSamplingTests
    {
        [Test] public void ConstantVelocityExtrapolatesWithoutReadingLiveState()
        {
            var track=new FireControlTrack(); int reads=0;
            Vector3 start=new Vector3(0,0,160),velocity=Vector3.right*8,origin=Vector3.up*4;
            track.UpdateObservation(0,2.5f,()=>{reads++;return start;},()=>velocity);
            for(int i=1;i<25;i++)
            {
                double now=i*.1;
                track.UpdateObservation(now,2.5f,()=>{reads++;return Vector3.one*999;},()=>Vector3.zero);
                Assert.That(track.EstimatePosition(now),Is.EqualTo(start+velocity*(float)now));
                Assert.IsTrue(track.Predict(now,origin,65,9.81f,250,out var point,out var t));
                Assert.That(Vector3.Distance(point,start+velocity*((float)now+t)),Is.LessThan(.01));
                Assert.IsTrue(BallisticSolver.TrySolve(origin,point,65,9.81f,out var v,out var exactTime));
                Assert.That(Vector3.Distance(BallisticSolver.Position(origin,v,9.81f,exactTime),
                    start+velocity*((float)now+exactTime)),Is.LessThan(.02));
            }
            Assert.That(reads,Is.EqualTo(1));
        }
        [Test] public void TurnIsUnknownUntilDueObservation()
        {
            var track=new FireControlTrack(); var initial=new Vector3(0,0,160); var old=Vector3.right*10;
            track.UpdateObservation(0,2.5f,()=>initial,()=>old);
            Vector3 turned=Quaternion.Euler(0,30,0)*old;
            Vector3 actual=initial+old+turned;
            track.UpdateObservation(2,2.5f,()=>actual,()=>turned);
            Assert.That(track.Velocity,Is.EqualTo(old));
            track.Predict(2,Vector3.up*4,65,9.81f,250,out var stale,out var t);
            Assert.That(Vector3.Distance(stale,actual+turned*t),Is.GreaterThan(8));
            actual+=turned*.5f;
            track.UpdateObservation(2.5,2.5f,()=>actual,()=>turned);
            Assert.That(track.ObservedTurnDegrees,Is.EqualTo(30).Within(.01));
            Assert.That(track.Velocity,Is.EqualTo(turned));
            Assert.IsTrue(track.Predict(2.5,Vector3.up*4,65,9.81f,250,out var fresh,out t));
            Assert.That(Vector3.Distance(fresh,actual+turned*t),Is.LessThan(.01));
        }
        [Test] public void PausedClockDoesNotAgeOrResample()
        {
            var track=new FireControlTrack();
            track.UpdateObservation(10,2.5f,()=>Vector3.forward*160,()=>Vector3.zero);
            for(int i=0;i<100;i++) track.UpdateObservation(10,2.5f,()=>Vector3.one,()=>Vector3.one);
            Assert.That(track.ObservationCount,Is.EqualTo(1)); Assert.That(track.Age(10),Is.Zero);
            Assert.IsFalse(track.Predict(double.NaN,Vector3.up,65,9.81f,250,out _,out _));
            track.UpdateObservation(1,2.5f,()=>Vector3.forward*100,()=>Vector3.zero);
            Assert.That(track.Timestamp,Is.EqualTo(1)); Assert.That(track.Heading,Is.EqualTo(Vector3.forward));
        }
        [Test] public void TargetSwitchDiscardsPreviousObservationAndDeadTargetsAreRemoved()
        {
            var root=new GameObject(); var a=new GameObject(); var b=new GameObject(); var muzzle=new GameObject();
            try
            {
                var ha=a.AddComponent<Health>(); ha.Restore(); var hb=b.AddComponent<Health>(); hb.Restore();
                a.transform.position=new Vector3(0,0,160); b.transform.position=new Vector3(30,0,160); muzzle.transform.position=Vector3.up*4;
                var selection=root.AddComponent<TargetSelectionController>();
                var lead=root.AddComponent<LeadPredictionController>(); lead.selection=selection; lead.fireControl=muzzle.transform;
                selection.Select(ha); lead.RefreshAt(0);
                a.transform.position+=Vector3.right*40;
                selection.Clear(); lead.RefreshAt(.2); selection.Select(ha); lead.RefreshAt(.3);
                Assert.That(lead.SampledTargetPosition.x,Is.EqualTo(40)); Assert.That(lead.SampleCount,Is.EqualTo(1));
                selection.Select(hb); lead.RefreshAt(.4); Assert.That(lead.SampledTargetPosition.x,Is.EqualTo(30));
                selection.Select(ha); lead.RefreshAt(.5); Assert.That(lead.SampledTargetPosition.x,Is.EqualTo(40));
                lead.RefreshAt(2.5); Assert.That(lead.SampledTargetPosition.x,Is.EqualTo(40));
                Object.DestroyImmediate(a); lead.RefreshAt(3);
                Assert.IsFalse(lead.Valid); Assert.That(lead.CachedTargetCount,Is.Zero);
                Object.DestroyImmediate(b); lead.RefreshAt(3.1); Assert.That(lead.CachedTargetCount,Is.Zero);
            }
            finally { Object.DestroyImmediate(root); if(a) Object.DestroyImmediate(a); if(b) Object.DestroyImmediate(b); Object.DestroyImmediate(muzzle); }
        }
        [Test] public void ReenterAimPreservesCorrectionAndTimestamp()
        {
            var root=new GameObject(); var target=new GameObject(); var muzzle=new GameObject();
            try
            {
                var hp=target.AddComponent<Health>(); hp.Restore(); target.transform.position=Vector3.forward*160; muzzle.transform.position=Vector3.up*4;
                var selection=root.AddComponent<TargetSelectionController>(); selection.Select(hp);
                var lead=root.AddComponent<LeadPredictionController>(); lead.selection=selection; lead.fireControl=muzzle.transform;
                var aim=root.AddComponent<ManualAimController>(); aim.prediction=lead;
                Assert.IsTrue(aim.Begin()); aim.Adjust(new Vector2(30,20));
                Vector3 saved=aim.PlayerAimPoint; double timestamp=lead.SampleTimestamp;
                aim.End(); target.transform.position+=Vector3.right*20; Assert.IsTrue(aim.Begin());
                Assert.That(aim.PlayerAimPoint,Is.EqualTo(saved)); Assert.That(lead.SampleTimestamp,Is.EqualTo(timestamp));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(target); Object.DestroyImmediate(muzzle); }
        }
    }
}
