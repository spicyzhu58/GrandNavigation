using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace GrandNavigation.Gunnery.Tests
{
    public class ProjectileTests
    {
        [UnityTest] public IEnumerator SweptProjectileHitsThinHullOnlyOnce()
        {
            var ship=GameObject.CreatePrimitive(PrimitiveType.Cube);
            ship.transform.position=new Vector3(350,4,5); ship.transform.localScale=new Vector3(5,5,.1f);
            var hp=ship.AddComponent<Health>();
            var shell=new GameObject("TestShell").AddComponent<ProjectileController>();
            shell.Launch(new Vector3(350,4,0),Vector3.forward*1000,9.81f,null);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.12f);
            Assert.That(hp.HitCount,Is.EqualTo(1));
            Assert.That(hp.CurrentHP,Is.EqualTo(hp.maxHP-shellDamage));
            Object.Destroy(ship);
        }
        const float shellDamage=40;
        [UnityTest] public IEnumerator AnalyticShellReachesSeaPoint()
        {
            ProjectileController.ResetStatistics();
            var origin=new Vector3(350,4,0); var point=new Vector3(350,0,100);
            Assert.IsTrue(BallisticSolver.TrySolve(origin,point,65,9.81f,out var v,out var t));
            var shell=new GameObject("WaterTestShell").AddComponent<ProjectileController>();
            shell.Launch(origin,v,9.81f,null);
            yield return new WaitForSeconds(t+.1f);
            Assert.That(ProjectileController.SplashCount,Is.EqualTo(1));
            Assert.That(Vector3.Distance(ProjectileController.LastImpact,point),Is.LessThan(.01));
        }
    }
}
