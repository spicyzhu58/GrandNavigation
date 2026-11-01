using NUnit.Framework;
using UnityEngine;
namespace GrandNavigation.Gameplay.Levels.Tests
{
    public class LevelBallisticsTests
    {
        [TestCase(400)][TestCase(600)][TestCase(800)][TestCase(1000)][TestCase(1200)]
        public void FixedSpeedAtMapRanges(float range)
        {
            var o=new Vector3(0,3,0);var p=new Vector3(range,0,0);
            foreach(var platform in new[]{Vector3.zero,Vector3.right*10,Vector3.left*3,new Vector3(6,0,8)})
            {
                Assert.IsTrue(CombatBallistics.Solve(o,p,platform,140,9.81f,out var relative,out float time));
                Assert.That(relative.magnitude,Is.EqualTo(140).Within(.01));
                Assert.That(Vector3.Distance(CombatBallistics.Position(o,relative+platform,9.81f,time),p),Is.LessThan(.01));
            }
        }
        [Test]public void HorizontalLimitAndLegacyRuleRemainDistinct()
        {
            var o=Vector3.up*3;var target=Vector3.right*1200;
            Assert.IsTrue(CombatBallistics.Intercept(o,target,Vector3.zero,Vector3.zero,140,9.81f,1200,out _,out _,true));
            Assert.IsFalse(CombatBallistics.Intercept(o,target,Vector3.zero,Vector3.zero,140,9.81f,1200,out _,out _));
            Assert.IsFalse(CombatBallistics.Intercept(o,target+Vector3.right*.1f,Vector3.zero,Vector3.zero,140,9.81f,1200,out _,out _,true));
        }
    }
}