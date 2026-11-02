using NUnit.Framework;
using UnityEngine;
namespace GrandNavigation.Gameplay.Tests
{
    public class CombatMathTests
    {
        [Test]public void AimEntryProjectionAndBoundedSeaFallback()
        {
            var pose=new Vector3(0,10,-20);var rotation=Quaternion.LookRotation(-pose);
            Assert.IsTrue(CombatCamera.InsideEntryFrame(Vector3.zero,pose,rotation,36,16f/9,.8f));
            Assert.IsFalse(CombatCamera.InsideEntryFrame(Vector3.right*100,pose,rotation,36,16f/9,.8f));
            Assert.IsFalse(CombatCamera.InsideEntryFrame(pose-rotation*Vector3.forward,pose,rotation,36,16f/9,.8f));
            float edge=10*Mathf.Tan(18*Mathf.Deg2Rad)*.8f;
            Assert.IsTrue(CombatCamera.InsideEntryFrame(new Vector3(edge*.99f,0,10),Vector3.zero,Quaternion.identity,36,1,.8f));
            Assert.IsFalse(CombatCamera.InsideEntryFrame(new Vector3(edge*1.01f,0,10),Vector3.zero,Quaternion.identity,36,1,.8f));
            Assert.That(CombatCamera.EntrySeaPoint(pose,rotation,Vector3.one,5000).magnitude,Is.LessThan(.001f));
            var fallback=new Vector3(25,0,80);
            Assert.AreEqual(fallback,CombatCamera.EntrySeaPoint(pose,Quaternion.identity,fallback,5000));
            Assert.AreEqual(fallback,CombatCamera.EntrySeaPoint(pose,rotation,fallback,1));
        }
        [TestCase(0,0)][TestCase(0,25)][TestCase(0,-8)][TestCase(20,10)]
        public void MovingPlatformRetainsFixedRelativeSpeedAndImpact(float x,float z)
        {
            var origin=new Vector3(0,4,0);var point=new Vector3(30,0,160);var platform=new Vector3(x,0,z);
            Assert.IsTrue(CombatBallistics.Solve(origin,point,platform,65,9.81f,out var relative,out float time));
            Assert.That(relative.magnitude,Is.EqualTo(65).Within(.002));
            Assert.That(Vector3.Distance(point,CombatBallistics.Position(origin,relative+platform,9.81f,time)),Is.LessThan(.005));
            Assert.That(time,Is.GreaterThan(0).And.LessThan(10));
        }
        [Test] public void ActualBarrelSeaImpactIncludesPlatformVelocity()
        {
            Vector3 origin=new Vector3(4,5,2),relative=new Vector3(0,20,60),platform=new Vector3(12,0,-8);
            Assert.IsTrue(CombatTurret.SeaImpact(origin,relative+platform,9.81f,out var point));
            float time=(20+Mathf.Sqrt(400+2*9.81f*5))/9.81f;
            Assert.That(Vector3.Distance(point,CombatBallistics.Position(origin,relative+platform,9.81f,time)),Is.LessThan(.001f));
            Assert.IsFalse(CombatTurret.SeaImpact(origin,relative,0,out _));
            Assert.IsFalse(CombatTurret.SeaImpact(origin,Vector3.one*float.NaN,9.81f,out _));
        }
        [Test] public void InvalidAndUnreachableShotsFail()
        {
            Assert.IsFalse(CombatBallistics.Solve(Vector3.up,Vector3.forward*10000,Vector3.zero,65,9.81f,out _,out _));
            Assert.IsFalse(CombatBallistics.Solve(Vector3.up,Vector3.forward,float.NaN*Vector3.one,65,9.81f,out _,out _));
            Assert.IsFalse(CombatBallistics.Solve(Vector3.up,Vector3.forward,Vector3.zero,-1,9.81f,out _,out _));
        }
        [Test] public void FrontAndRearArcsExcludeOwnSuperstructure()
        {
            Assert.IsTrue(GunneryMath.InArc(129,0,260));Assert.IsFalse(GunneryMath.InArc(131,0,260));
            Assert.IsFalse(GunneryMath.InArc(0,180,260));Assert.IsTrue(GunneryMath.InArc(-179,180,260));
        }
        [Test] public void GaussianIsCentredRadiallyTruncatedAndScalesWithDistance()
        {
            var r=new System.Random(7);Vector2 sum=Vector2.zero;float square=0;
            for(int i=0;i<10000;i++){var p=GunneryMath.Gaussian(r,2);Assert.That(p.magnitude,Is.LessThanOrEqualTo(6.001));sum+=p;square+=p.sqrMagnitude;}
            Assert.That((sum/10000).magnitude,Is.LessThan(.1));Assert.That(square/20000,Is.InRange(3.4f,4.2f));
            Assert.That(GunneryMath.Gaussian(new System.Random(9),3),Is.EqualTo(GunneryMath.Gaussian(new System.Random(9),1)*3));
        }
        [Test] public void ObservationDoesNotReadUnsampledVelocityAndExtrapolates()
        {
            var track=new FireControlObservation();int reads=0;Vector3 v=Vector3.right*8;
            track.Sample(0,.5f,()=>Vector3.forward*160,()=>{reads++;return v;});v=Vector3.left*8;
            track.Sample(.4,.5f,()=>Vector3.zero,()=>{reads++;return v;});
            Assert.That(reads,Is.EqualTo(1));Assert.That(track.Estimate(.4).x,Is.EqualTo(3.2f).Within(.001));
            track.Sample(.5,.5f,()=>new Vector3(4,0,160),()=>{reads++;return v;});Assert.That(reads,Is.EqualTo(2));Assert.That(track.Velocity,Is.EqualTo(v));
        }
        [Test] public void SampledInterceptMatchesMovingTargetAndMovingShooter()
        {
            var origin=new Vector3(0,4,0);var target=new Vector3(20,0,160);var velocity=Vector3.right*8;var platform=new Vector3(4,0,12);
            var observation=new FireControlObservation();observation.Sample(0,.5f,()=>target,()=>velocity);
            Assert.IsTrue(CombatBallistics.Intercept(origin,observation.Estimate(.4),observation.Velocity,platform,65,9.81f,300,out var intercept,out float time));
            Assert.That(Vector3.Distance(intercept,target+velocity*(.4f+time)),Is.LessThan(.01));
            Assert.IsTrue(CombatBallistics.Solve(origin,intercept,platform,65,9.81f,out var relative,out float flight));
            Assert.That(Vector3.Distance(CombatBallistics.Position(origin,relative+platform,9.81f,flight),intercept),Is.LessThan(.01));
        }
        [Test] public void HealthClampsAndDeathOccursOnlyOnce()
        {
            var go=new GameObject("Health test");var h=go.AddComponent<HealthSystem>();h.maximumHealth=150;h.Restore();
            int deaths=0;h.Died+=()=>deaths++;h.TakeDamage(-5);Assert.That(h.CurrentHealth,Is.EqualTo(150));h.TakeDamage(int.MaxValue);h.TakeDamage(25);
            Assert.That(h.CurrentHealth,Is.Zero);Assert.That(deaths,Is.EqualTo(1));Object.DestroyImmediate(go);
        }
    }
}
