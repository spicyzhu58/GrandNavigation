using NUnit.Framework;
using UnityEngine;
namespace GrandNavigation.Gunnery.Tests
{
    public class BallisticSolverTests
    {
        [TestCase(100f)] [TestCase(160f)] [TestCase(220f)] [TestCase(250f)]
        public void LowArcArrivesWithFixedSpeed(float range)
        {
            var origin=new Vector3(2,4,-8); var point=new Vector3(2,0,range-8);
            Assert.IsTrue(BallisticSolver.TrySolve(origin,point,65,9.81f,out var v,out var t));
            Assert.That(v.magnitude,Is.EqualTo(65).Within(.001));
            Assert.That(Vector3.Distance(BallisticSolver.Position(origin,v,9.81f,t),point),Is.LessThan(.002));
            Assert.That(t,Is.InRange(1f,4.2f));
        }
        [TestCase(0f,9.81f)] [TestCase(-1f,9.81f)] [TestCase(65f,0f)]
        public void InvalidParametersFail(float speed,float gravity) =>
            Assert.IsFalse(BallisticSolver.TrySolve(Vector3.up*4,Vector3.forward*160,speed,gravity,out _,out _));
        [Test] public void OutOfRangeFails() =>
            Assert.IsFalse(BallisticSolver.TrySolve(Vector3.up*4,Vector3.forward*1000,65,9.81f,out _,out _));
        [Test] public void NonFiniteFails() =>
            Assert.IsFalse(BallisticSolver.TrySolve(Vector3.up,new Vector3(float.NaN,0,10),65,9.81f,out _,out _));
        [Test] public void VerticalDegenerateFails() =>
            Assert.IsFalse(BallisticSolver.TrySolve(Vector3.up,Vector3.zero,65,9.81f,out _,out _));
        [TestCase(0f)] [TestCase(8f)] [TestCase(12f)]
        public void MovingTargetInterceptConverges(float speed)
        {
            var origin=new Vector3(0,4,0); var target=new Vector3(0,0,160); var motion=Vector3.right*speed;
            Assert.IsTrue(BallisticSolver.TryIntercept(origin,target,motion,65,9.81f,250,out var point,out var time));
            Assert.That(Vector3.Distance(point,target+motion*time),Is.LessThan(.01));
            Assert.IsTrue(BallisticSolver.TrySolve(origin,point,65,9.81f,out var v,out var t));
            Assert.That(Vector3.Distance(BallisticSolver.Position(origin,v,9.81f,t),target+motion*t),Is.LessThan(.02));
        }
    }
}