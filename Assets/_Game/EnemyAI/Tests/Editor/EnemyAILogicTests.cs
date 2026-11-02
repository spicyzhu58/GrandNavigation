using NUnit.Framework;
using UnityEngine;
namespace GrandNavigation.Gameplay.AI.Tests
{
    public class EnemyAILogicTests
    {
        EnemyAIProfile p;
        [SetUp]public void Setup(){p=ScriptableObject.CreateInstance<EnemyAIProfile>();}
        [TearDown]public void Cleanup(){Object.DestroyImmediate(p);}
        [Test]public void ThresholdsHysteresisAndMinimumTime()
        {
            Assert.AreEqual(EnemyState.Approach,EnemyAILogic.Decide(EnemyState.Patrol,1,true,true,800,0,p));
            Assert.AreEqual(EnemyState.Approach,EnemyAILogic.Decide(EnemyState.Approach,1,true,true,900,2.99f,p));
            Assert.AreEqual(EnemyState.Engage,EnemyAILogic.Decide(EnemyState.Approach,1,true,true,900,3,p));
            Assert.AreEqual(EnemyState.Engage,EnemyAILogic.Decide(EnemyState.Engage,1,true,true,999,10,p));
            Assert.AreEqual(EnemyState.Approach,EnemyAILogic.Decide(EnemyState.Engage,1,true,false,1000,3,p));
            Assert.AreEqual(EnemyState.Approach,EnemyAILogic.Decide(EnemyState.Approach,1,true,false,800,10,p));
        }
        [Test]public void DeathRetreatAndInvalidTargetPriority()
        {
            Assert.AreEqual(EnemyState.Destroyed,EnemyAILogic.Decide(EnemyState.Engage,0,false,false,0,0,p));
            Assert.AreEqual(EnemyState.Retreat,EnemyAILogic.Decide(EnemyState.Engage,.2f,false,false,0,0,p));
            Assert.AreEqual(EnemyState.Retreat,EnemyAILogic.Decide(EnemyState.Retreat,1,false,false,0,50,p));
            Assert.AreEqual(EnemyState.Patrol,EnemyAILogic.Decide(EnemyState.Engage,1,false,false,0,0,p));
            Assert.AreEqual(EnemyState.Destroyed,EnemyAILogic.Decide(EnemyState.Destroyed,1,true,true,0,100,p));
        }
        [Test]public void UniformDiskAndProjectedProgress()
        {
            var r=new System.Random(731);float squared=0;
            for(int i=0;i<10000;i++){var e=EnemyAILogic.DiskError(r,24);Assert.LessOrEqual(e.magnitude,24.001f);Assert.AreEqual(0,e.y);squared+=e.sqrMagnitude;}
            Assert.That(squared/10000,Is.EqualTo(24*24*.5f).Within(10));
            Assert.IsFalse(EnemyAILogic.Stuck(7.9f,0,8,25));Assert.IsTrue(EnemyAILogic.Stuck(8,24.9f,8,25));Assert.IsFalse(EnemyAILogic.Stuck(8,25,8,25));
        }
        [Test]public void ConfigurationRejectsInvalidCapabilitiesAndNumbers()
        {
            Assert.IsNull(p.Validate(15,450));Assert.IsNotNull(p.Validate(14,450));Assert.IsNotNull(p.Validate(15,0));
            p.engageEnterRange=1000;Assert.IsNotNull(p.Validate(15,450));p.engageEnterRange=900;p.avoidLookAhead=float.NaN;Assert.IsNotNull(p.Validate(15,450));
        }
    }
}
