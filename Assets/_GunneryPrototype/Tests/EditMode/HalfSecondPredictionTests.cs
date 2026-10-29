using NUnit.Framework;
using UnityEngine;
namespace GrandNavigation.Gunnery.Tests
{
    public class HalfSecondPredictionTests
    {
        [Test] public void ConstantTargetIsAccurateOnEveryFrameBetweenHalfSecondSamples()
        {
            var track=new FireControlTrack(); int reads=0;
            for(int i=0;i<=200;i++)
            {
                double now=i*.01; Vector3 position=new Vector3((float)now*8,0,160);
                track.UpdateObservation(now,.5f,()=>{reads++;return position;},()=>Vector3.right*8);
                Assert.IsTrue(track.Predict(now,Vector3.up*4,65,9.81f,250,out var point,out var flight));
                Assert.That(Vector3.Distance(point,position+Vector3.right*8*flight),Is.LessThan(.02));
            }
            Assert.That(reads,Is.EqualTo(5));
        }
        [Test] public void TurnAndSpeedChangesAreUnknownUntilNextHalfSecond()
        {
            var track=new FireControlTrack();
            track.UpdateObservation(0,.5f,()=>Vector3.forward*160,()=>Vector3.right*8);
            track.UpdateObservation(.49,.5f,()=>throw new System.Exception("Premature position read"),()=>throw new System.Exception("Premature velocity read"));
            Assert.That(track.Velocity,Is.EqualTo(Vector3.right*8));
            track.UpdateObservation(.5,.5f,()=>new Vector3(2,0,158),()=>Vector3.back*12);
            Assert.That(track.Timestamp,Is.EqualTo(.5)); Assert.That(track.Velocity,Is.EqualTo(Vector3.back*12));
        }
        [Test] public void DisplaySmoothingCannotChangePredictionOrManualAimAndHidesOnInvalidTarget()
        {
            var root=new GameObject();var ship=new GameObject();var muzzle=new GameObject();
            try
            {
                var hp=ship.AddComponent<Health>();hp.Restore();ship.transform.position=Vector3.forward*160;muzzle.transform.position=Vector3.up*4;
                var selection=root.AddComponent<TargetSelectionController>();selection.Select(hp);
                var lead=root.AddComponent<LeadPredictionController>();lead.selection=selection;lead.fireControl=muzzle.transform;
                var aim=root.AddComponent<ManualAimController>();aim.prediction=lead;
                var session=root.AddComponent<PrototypeSession>();session.selection=selection;session.prediction=lead;session.aim=aim;
                var hud=root.AddComponent<PrototypeHUD>();hud.session=session;
                Assert.That(lead.fireControlUpdateInterval,Is.EqualTo(.5f));Assert.That(hud.predictionSmoothTime,Is.EqualTo(.12f));
                Assert.IsTrue(aim.Begin());aim.Adjust(new Vector2(20,10));var manual=aim.PlayerAimPoint;
                lead.RefreshAt(0);hud.UpdatePredictionDisplay(.016f);var old=hud.DisplayedPrediction;
                ship.transform.position+=Vector3.right*20;lead.RefreshAt(.5);var raw=lead.PredictedIntercept;
                hud.UpdatePredictionDisplay(.016f);
                Assert.That(hud.DisplayedPrediction.x,Is.GreaterThan(old.x).And.LessThan(raw.x));
                Assert.That(lead.PredictedIntercept,Is.EqualTo(raw));Assert.That(aim.PlayerAimPoint,Is.EqualTo(manual));
                for(int i=0;i<60;i++)hud.UpdatePredictionDisplay(.016f);
                Assert.That(Vector3.Distance(hud.DisplayedPrediction,raw),Is.LessThan(.01));
                selection.Clear();lead.RefreshAt(.6);hud.UpdatePredictionDisplay(.016f);Assert.IsFalse(hud.PredictionVisible);
            }
            finally { Object.DestroyImmediate(root);Object.DestroyImmediate(ship);Object.DestroyImmediate(muzzle); }
        }
    }
}
