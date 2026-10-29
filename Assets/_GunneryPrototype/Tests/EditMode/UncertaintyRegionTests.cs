using NUnit.Framework;
using UnityEngine;
namespace GrandNavigation.Gunnery.Tests
{
    public class UncertaintyRegionTests
    {
        [Test] public void RegionGrowsWithAgeAndObservedTurnAndStaysBounded()
        {
            var go=new GameObject();
            try
            {
                var lead=go.AddComponent<LeadPredictionController>();
                var fresh=lead.ComputeRegionRadii(160,10,0,0);
                var stale=lead.ComputeRegionRadii(160,10,2,0);
                var turning=lead.ComputeRegionRadii(160,10,2,30);
                Assert.That(stale.x,Is.GreaterThan(fresh.x)); Assert.That(stale.y,Is.GreaterThan(fresh.y));
                Assert.That(turning.y,Is.GreaterThan(stale.y));
                Assert.That(lead.ComputeRegionRadii(100000,1000,1000,180),Is.EqualTo(new Vector2(18,18)));
                Assert.That(lead.ComputeRegionRadii(float.NaN,0,0,0).x,Is.InRange(3,18));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void StoppedTrackKeepsStableHeading()
        {
            var track=new FireControlTrack();
            track.UpdateObservation(0,1,()=>Vector3.forward*160,()=>Vector3.right*10);
            track.UpdateObservation(1,1,()=>Vector3.forward*160,()=>Vector3.zero);
            track.UpdateObservation(2,1,()=>Vector3.forward*160,()=>Vector3.zero);
            Assert.That(track.Heading,Is.EqualTo(Vector3.right));
        }
        [Test] public void RadiiAndVisualCenterShrinkSmoothlyWithoutChangingPlayerPoint()
        {
            var go=new GameObject(); var target=new GameObject(); var muzzle=new GameObject();
            try
            {
                var hp=target.AddComponent<Health>(); hp.Restore(); target.transform.position=Vector3.forward*160;
                muzzle.transform.position=Vector3.up*4;
                var selection=go.AddComponent<TargetSelectionController>(); selection.Select(hp);
                var lead=go.AddComponent<LeadPredictionController>(); lead.selection=selection; lead.fireControl=muzzle.transform;
                lead.RefreshAt(0); lead.UpdateRegion(.1f);
                lead.RefreshAt(2.4); lead.UpdateRegion(1); var old=lead.RegionRadii;
                lead.RefreshAt(2.5); lead.UpdateRegion(.016f);
                Assert.That(Vector2.Distance(old,lead.RegionRadii),Is.LessThan(.2));
                Assert.That(Vector3.Distance(lead.InitialAimNearRegion(),lead.PredictedIntercept),Is.GreaterThan(1));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(target); Object.DestroyImmediate(muzzle); }
        }
    }
}
