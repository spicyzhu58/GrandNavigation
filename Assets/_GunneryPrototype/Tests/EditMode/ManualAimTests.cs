using NUnit.Framework;
using UnityEngine;
namespace GrandNavigation.Gunnery.Tests
{
    public class ManualAimTests
    {
        [Test] public void PredictionMotionDoesNotOverwriteManualPoint()
        {
            var root=new GameObject(); var target=new GameObject(); var muzzle=new GameObject();
            try
            {
                var hp=target.AddComponent<Health>(); hp.Restore(); target.transform.position=new Vector3(0,0,160);
                muzzle.transform.position=Vector3.up*4;
                var select=root.AddComponent<TargetSelectionController>(); select.Select(hp);
                var prediction=root.AddComponent<LeadPredictionController>(); prediction.selection=select; prediction.fireControl=muzzle.transform;
                var aim=root.AddComponent<ManualAimController>(); aim.prediction=prediction;
                Assert.IsTrue(aim.Begin()); aim.Adjust(new Vector2(10,20)); var point=aim.PlayerAimPoint;
                target.transform.position+=Vector3.right*20; prediction.Refresh();
                Assert.That(aim.PlayerAimPoint,Is.EqualTo(point)); Assert.That(point.y,Is.Zero);
                Assert.That(prediction.PredictedIntercept,Is.Not.EqualTo(point));
                aim.End(); Assert.That(aim.PlayerAimPoint,Is.EqualTo(point));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(target); Object.DestroyImmediate(muzzle); }
        }
    }
}