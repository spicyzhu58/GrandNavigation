using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace GrandNavigation.Gameplay.Levels.Tests
{
    public class LevelTests
    {
        LevelDebug level;PlayerShip player;Rigidbody body;
        readonly List<InputDevice> native=new List<InputDevice>();
        Mouse mouse;Keyboard keyboard;
        [UnitySetUp] public IEnumerator Setup()
        {
            #if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/LevelGreybox.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            #endif
            yield return null;level=UnityEngine.Object.FindFirstObjectByType<LevelDebug>();player=level.player;body=player.GetComponent<Rigidbody>();
            foreach(var d in InputSystem.devices)if(d.enabled&&(d is Mouse||d is Keyboard))native.Add(d);
            foreach(var d in native)InputSystem.DisableDevice(d);
            mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();
            Application.runInBackground=true;
            player.GetComponent<PlayerShipInput>().enabled=false;
            UnityEngine.Object.FindFirstObjectByType<PlayerCombatInput>().enabled=false;
            Time.timeScale=6;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            foreach(var d in native)if(d.added)InputSystem.EnableDevice(d);native.Clear();yield return null;
        }
        static void Report(string text){Debug.Log("LEVEL MEASURE: "+text);System.IO.File.AppendAllText("Temp/level-measurements.txt",text+Environment.NewLine);}
        [UnityTest] public IEnumerator GeometryReferencesAndTargetReset()
        {
            Assert.That(Vector3.Distance(GameObject.Find("Ocean").GetComponent<Renderer>().bounds.size,new Vector3(2560,0,2560)),Is.LessThan(.001));
            Assert.IsNull(GameObject.Find("Ocean").GetComponent<Collider>());
            Assert.That(Vector3.Distance(player.WorldPosition,new Vector3(0,0,-360)),Is.LessThan(.001));
            Assert.That(player.GetComponent<BoxCollider>().size.z,Is.EqualTo(32));
            var hull=player.transform.Find("VisualModel/Hull").GetComponent<Renderer>().bounds;
            var bow=player.transform.Find("VisualModel/Bow").GetComponent<Renderer>().bounds;hull.Encapsulate(bow);
            Assert.That(hull.size.z,Is.EqualTo(32).Within(.001));
            foreach(var island in UnityEngine.Object.FindObjectsByType<LevelIsland>(FindObjectsSortMode.None))
            {
                var b=island.solid.GetComponent<Collider>().bounds;
                Assert.That(Vector3.Distance(b.size,island.size),Is.LessThan(.001));Assert.That(b.min.y,Is.EqualTo(0).Within(.001));
                Assert.That(island.transform.localScale,Is.EqualTo(Vector3.one));
            }
            foreach(var target in level.targets)
            {
                Assert.That(target.Health.CurrentHealth,Is.EqualTo(1000));Assert.IsFalse(target.Movement.enabled);
                level.fire.Lock(target);Assert.That(level.fire.Target,Is.EqualTo(target));
                target.TakeDamage(1000);yield return null;Assert.IsFalse(target.IsAlive);Assert.IsNull(level.fire.Target);
            }
            yield return new WaitForSeconds(3.2f);level.ResetTargets();yield return null;
            foreach(var t in level.targets){Assert.IsTrue(t.IsAlive);Assert.That(t.Health.CurrentHealth,Is.EqualTo(1000));Assert.IsTrue(t.GetComponent<Collider>().enabled);}
            Assert.That(player.GetComponents<Ship>().Length,Is.EqualTo(1));Assert.IsInstanceOf<IDamageable>(player);
        }
        [UnityTest] public IEnumerator MeasuredAccelerationBrakeTurnAndReverse()
        {
            body.position=new Vector3(750,0,-800);body.rotation=Quaternion.identity;
            player.Movement.SetTargetSpeed(10);float start=Time.fixedTime;
            while(player.Movement.ActualSpeed<9.99f&&Time.fixedTime-start<9)yield return new WaitForFixedUpdate();
            float acceleration=Time.fixedTime-start;Assert.That(acceleration,Is.InRange(5.8f,6.3f));
            player.Movement.SetTargetSpeed(0);start=Time.fixedTime;
            while(player.Movement.ActualSpeed>.01f&&Time.fixedTime-start<8)yield return new WaitForFixedUpdate();
            float braking=Time.fixedTime-start;Assert.That(braking,Is.InRange(4.8f,5.3f));
            player.Movement.SetTargetSpeed(-3);yield return new WaitForSeconds(4);
            Assert.That(player.Movement.ActualSpeed,Is.EqualTo(-3).Within(.05));
            player.Movement.SetSteeringInput(1);yield return new WaitForSeconds(1);Assert.Less(player.Movement.TurnRate,0);
            level.ResetPlayer();body.position=new Vector3(750,0,-800);body.rotation=Quaternion.identity;
            player.Movement.SetTargetSpeed(10);yield return new WaitForSeconds(7);player.Movement.SetSteeringInput(1);
            yield return new WaitForSeconds(2);
            Vector3 a=body.position;float heading=body.rotation.eulerAngles.y;start=Time.fixedTime;float turned=0,last=heading;
            while(turned<180&&Time.fixedTime-start<50)
            {
                yield return new WaitForFixedUpdate();float yaw=body.rotation.eulerAngles.y;
                turned+=Mathf.DeltaAngle(last,yaw);last=yaw;
            }
            float halfTurn=Time.fixedTime-start,radius=Vector3.Distance(a,body.position)*.5f;
            Assert.That(radius,Is.InRange(95f,130f));Assert.That(halfTurn,Is.InRange(35f,39f));
            Report($"acceleration={acceleration:F2}s; braking={braking:F2}s; steady half-turn={halfTurn:F2}s; measured chord radius={radius:F2}UU; turnRate={player.Movement.TurnRate:F2}; speed={player.Movement.ActualSpeed:F3}");
        }
        [UnityTest] public IEnumerator IslandSlidingAndBoundaryContainment()
        {
            body.position=new Vector3(-310,0,-60);body.rotation=Quaternion.Euler(0,25,0);body.linearVelocity=Vector3.zero;
            player.Movement.SetTargetSpeed(10);yield return new WaitForSeconds(12);
            var island=GameObject.Find("Island_A/Solid").GetComponent<Collider>();var own=player.GetComponent<Collider>();
            Assert.IsFalse(Physics.ComputePenetration(own,body.position,body.rotation,island,island.transform.position,island.transform.rotation,out _,out float overlap)&&overlap>.05);
            Assert.That(body.position.z,Is.LessThan(-42));Assert.That(body.position.x,Is.GreaterThan(-300));
            Assert.That(Mathf.Abs(body.position.y),Is.LessThan(.001));Assert.That(body.linearVelocity.magnitude,Is.LessThan(10.1));
            level.ResetPlayer();body.position=new Vector3(1220,0,-800);body.rotation=Quaternion.Euler(0,90,0);
            player.Movement.SetTargetSpeed(10);yield return new WaitForSeconds(15);
            Assert.That(own.bounds.max.x,Is.LessThanOrEqualTo(1280.1f));Assert.That(body.position.x,Is.GreaterThan(1250));
        }
        [UnityTest] public IEnumerator FiveRangesActualFlightAndMovingModel()
        {
            player.Movement.enabled=false;body.position=new Vector3(-1000,0,-900);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.zero;
            yield return new WaitForFixedUpdate();yield return null;
            var w=player.Weapon;Vector3 origin=w.turrets[0].muzzles[0].position;
            foreach(float distance in new[]{400f,600,800,1000,1200})
            {
                Vector3 point=origin+Vector3.right*distance;point.y=0;
                foreach(var velocity in new[]{Vector3.zero,Vector3.right*10,Vector3.left*3,new Vector3(6,0,8)})
                {
                    Assert.IsTrue(CombatBallistics.Solve(origin,point,velocity,140,9.81f,out var relative,out float flight));
                    Assert.That(relative.magnitude,Is.EqualTo(140).Within(.01));
                    Assert.That(Vector3.Distance(CombatBallistics.Position(origin,relative+velocity,9.81f,flight),point),Is.LessThan(.02));
                }
                Assert.IsTrue(w.Solve(origin,point,out var v,out float time));int splash=Projectile.SplashCount;
                var shell=UnityEngine.Object.Instantiate(w.projectile);shell.Launch(origin,v,9.81f,player,25,false);
                float launchTime=Time.fixedTime;
                while(shell&&Time.fixedTime-launchTime<time+.5f)yield return new WaitForFixedUpdate();
                float actualTime=Time.fixedTime-launchTime;
                Assert.That(Projectile.SplashCount,Is.EqualTo(splash+1));Assert.IsTrue(!shell);
                Report($"range={distance:F0}UU actual muzzleY={origin.y:F3} solved flight={time:F3}s; measured flight={actualTime:F3}s; actual splash passed");
            }
            Assert.IsFalse(w.Solve(origin,new Vector3(origin.x+1200.1f,0,origin.z),out _,out _));
            Assert.IsFalse(CombatBallistics.Intercept(origin,new Vector3(origin.x+1200.1f,0,origin.z),Vector3.zero,Vector3.zero,140,9.81f,1200,out _,out _,true));
            Assert.That(w.projectile.lifetime,Is.GreaterThan(10));Assert.That(w.projectile.worldBounds,Is.EqualTo(1400));
        }
        [UnityTest] public IEnumerator IslandBlocksRealLowArcAndTargetReceivesRealShell()
        {
            var w=player.Weapon;var target=level.targets[0];Vector3 origin=w.turrets[0].muzzles[0].position;
            foreach(var blockedTarget in level.targets)
            {
                Assert.IsTrue(w.Solve(origin,new Vector3(blockedTarget.WorldPosition.x,0,blockedTarget.WorldPosition.z),out var v,out float time));
                int hp=blockedTarget.Health.CurrentHealth;int splash=Projectile.SplashCount;
                var shell=UnityEngine.Object.Instantiate(w.projectile);shell.Launch(origin,v,9.81f,player,25,false);
                yield return new WaitForSeconds(time+.2f);
                Assert.IsTrue(!shell);Assert.That(blockedTarget.Health.CurrentHealth,Is.EqualTo(hp),blockedTarget.name+" should be protected by island");
                Assert.That(Projectile.SplashCount,Is.EqualTo(splash),"Low arc should hit island before the sea");
            }
            foreach(var t in level.targets)
            {
                Vector3 o=t.WorldPosition-Vector3.right*30+Vector3.up*.2f;
                var hit=UnityEngine.Object.Instantiate(w.projectile);hit.Launch(o,Vector3.right*140,9.81f,player,25,false);
                yield return new WaitForSeconds(.4f);Assert.That(t.Health.CurrentHealth,Is.EqualTo(975));
            }
            Assert.IsNotEmpty(UnityEngine.Object.FindObjectsByType<CombatEffects>(FindObjectsSortMode.None));
        }
        [UnityTest] public IEnumerator FarLockCameraPredictionAndDrivingInput()
        {
            Time.timeScale=1;
            var input=UnityEngine.Object.FindFirstObjectByType<PlayerCombatInput>();input.enabled=true;
            var target=level.targets[2];var tb=target.GetComponent<Rigidbody>();
            foreach(float distance in new[]{400f,600,800,1200})
            {
                var fire=level.fire;Vector3 origin=fire.reference.position;
                tb.position=new Vector3(origin.x+distance,1,origin.z);
                yield return new WaitForFixedUpdate();yield return null;
                var cam=input.view;level.cameraRig.enabled=false;level.cameraRig.orbit.SetControlActive(false);
                cam.transform.position=player.WorldPosition+new Vector3(0,15,0);cam.transform.LookAt(target.WorldPosition);Physics.SyncTransforms();
                var screen=cam.WorldToScreenPoint(target.WorldPosition);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(screen.x,screen.y)}.WithButton(MouseButton.Right));
                yield return null;yield return null;Assert.That(fire.Target,Is.EqualTo(target),"Ray lock distance "+distance);
                InputSystem.QueueStateEvent(mouse,new MouseState());level.cameraRig.enabled=true;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift));
                yield return new WaitForSeconds(1.7f);
                Assert.IsTrue(level.aim.IsAiming);
                var viewport=cam.WorldToViewportPoint(target.WorldPosition+Vector3.up*1.5f);
                Assert.That(viewport.x,Is.InRange(.45f,.55f));Assert.That(viewport.y,Is.InRange(.45f,.55f));Assert.That(viewport.z,Is.GreaterThan(0));
                Assert.IsTrue(fire.Valid);
                var region=UnityEngine.Object.FindFirstObjectByType<PredictionRegion>();Assert.That(region.longitudinalRadius,Is.EqualTo(24));Assert.That(region.lateralRadius,Is.EqualTo(12));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;fire.Lock(null);
            }
            player.GetComponent<PlayerShipInput>().enabled=true;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D,Key.LeftShift));level.fire.Lock(target);
            yield return new WaitForSeconds(1);
            Assert.That(player.Movement.TargetSpeed,Is.GreaterThan(5));Assert.That(player.Movement.RudderAngle,Is.GreaterThan(5));
            Assert.IsTrue(level.aim.IsAiming);
        }
    }
}
