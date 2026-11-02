using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
namespace GrandNavigation.Gameplay.Tests
{
    public class PlayerCombatTests
    {
        PlayerShip player;Destroyer target;CombatFireControl fire;CombatAimController aim;PlayerCombatInput combatInput;
        Mouse mouse;Keyboard keyboard;readonly List<InputDevice> native=new List<InputDevice>();
        [UnitySetUp]public IEnumerator Setup()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/PlayerCombatPrototype.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null;player=Object.FindFirstObjectByType<PlayerShip>();target=Object.FindFirstObjectByType<Destroyer>();fire=Object.FindFirstObjectByType<CombatFireControl>();aim=Object.FindFirstObjectByType<CombatAimController>();combatInput=Object.FindFirstObjectByType<PlayerCombatInput>();
            foreach(var d in InputSystem.devices)if(d.enabled&&(d is Mouse||d is Keyboard))native.Add(d);foreach(var d in native)InputSystem.DisableDevice(d);
            mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();Application.runInBackground=true;
            target.GetComponent<TestTargetMotion>().enabled=false;target.Movement.SetTargetSpeed(0);target.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
            yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            foreach(var d in native)if(d.added)InputSystem.EnableDevice(d);native.Clear();
            yield return null;
        }
        [UnityTest]public IEnumerator MouseAimsWithoutLockAndShiftUsesPrototypeCloseup()
        {
            Assert.IsNull(fire.Target);Vector3 initial=aim.PlayerAimPoint;float yaw=player.Weapon.turrets[0].LocalYaw;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width*.5f,Screen.height*.5f),delta=new Vector2(160,-35)});
            yield return null;yield return null;
            Assert.That(Vector3.Distance(initial,aim.PlayerAimPoint),Is.GreaterThan(1));Assert.That(player.Weapon.AimPoint,Is.EqualTo(aim.PlayerAimPoint));
            yield return new WaitForSeconds(.4f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yaw,player.Weapon.turrets[0].LocalYaw)),Is.GreaterThan(.1f));
            Vector3 chosen=aim.PlayerAimPoint;fire.Lock(target);Assert.That(aim.PlayerAimPoint,Is.EqualTo(chosen));
            // Keep an already-visible manual correction when entering the destination frame.
            chosen=target.WorldPosition+Vector3.right*2;chosen.y=0;aim.SetPoint(chosen);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift));yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(aim.IsAiming);Assert.That(aim.PlayerAimPoint,Is.EqualTo(chosen));
            var rig=Object.FindFirstObjectByType<CombatCamera>();var expected=target.WorldPosition+Vector3.up*(rig.targetFocusHeight+rig.aimingHeight)-rig.ViewingDirection*rig.aimingDistance;
            Assert.That(Vector3.Distance(rig.view.transform.position,expected),Is.LessThan(1));
            aim.SetPoint(player.WorldPosition+Vector3.left*250);yield return new WaitForSeconds(.5f);
            Assert.That(Vector3.Distance(rig.view.transform.position,expected),Is.LessThan(1),"Manual point must not zoom the closeup back out");
            float fov=rig.view.fieldOfView;InputSystem.QueueDeltaStateEvent(mouse.scroll,new Vector2(0,1));yield return new WaitForSeconds(.7f);Assert.That(rig.view.fieldOfView,Is.LessThan(fov-1));
        }
        [UnityTest]public IEnumerator TargetCameraFreezesBearingAndReanchorsOnNextEntry()
        {
            combatInput.enabled=false;player.GetComponent<PlayerShipInput>().enabled=false;player.Movement.enabled=false;
            var body=player.GetComponent<Rigidbody>();body.isKinematic=true;
            var targetBody=target.GetComponent<Rigidbody>();target.Movement.enabled=false;targetBody.isKinematic=true;
            body.position=target.WorldPosition+Vector3.right*100;
            yield return new WaitForFixedUpdate();yield return null;
            fire.Lock(target);Vector3 chosen=aim.PlayerAimPoint;
            var rig=Object.FindFirstObjectByType<CombatCamera>();
            aim.SetAiming(true);rig.SetAiming(true);chosen=aim.PlayerAimPoint;
            Assert.That(Vector3.Distance(rig.ViewingDirection,Vector3.left),Is.LessThan(.01f));
            yield return new WaitForSeconds(2);
            Vector3 bearing=rig.ViewingDirection;
            body.position+=Vector3.forward*60;body.rotation=Quaternion.Euler(0,135,0);
            targetBody.position+=Vector3.forward*10;targetBody.rotation=Quaternion.Euler(0,90,0);
            yield return new WaitForSeconds(2);
            Assert.That(rig.ViewingDirection,Is.EqualTo(bearing));
            Vector3 expected=target.WorldPosition+Vector3.up*(rig.targetFocusHeight+rig.aimingHeight)-bearing*rig.aimingDistance;
            Assert.That(Vector3.Distance(rig.view.transform.position,expected),Is.LessThan(.2f));
            Assert.That(Vector3.Angle(rig.view.transform.forward,target.WorldPosition+Vector3.up*rig.targetFocusHeight-rig.view.transform.position),Is.LessThan(1));
            Assert.That(aim.PlayerAimPoint,Is.EqualTo(chosen));
            aim.SetAiming(false);rig.SetAiming(false);yield return new WaitForSeconds(1);
            aim.SetAiming(true);rig.SetAiming(true);
            Assert.That(Vector3.Angle(bearing,rig.ViewingDirection),Is.GreaterThan(10));
            Vector3 before=aim.PlayerAimPoint;aim.Adjust(Vector2.right);
            Assert.That(Vector3.Dot((aim.PlayerAimPoint-before).normalized,Vector3.Cross(Vector3.up,rig.ViewingDirection)),Is.GreaterThan(.99f));
            targetBody.isKinematic=false;target.TakeDamage(150);yield return null;yield return null;
            Assert.IsFalse(aim.IsAiming);
        }
        [UnityTest]public IEnumerator AimEntryRecentersOnceWithoutSnappingTurretsOrFollowingPrediction()
        {
            combatInput.enabled=false;player.GetComponent<PlayerShipInput>().enabled=false;player.Movement.enabled=false;target.Movement.enabled=false;
            fire.Lock(target);var rig=Object.FindFirstObjectByType<CombatCamera>();
            aim.SetPoint(player.WorldPosition+Vector3.left*1000);
            Quaternion barrel=player.Weapon.turrets[0].muzzles[0].rotation;
            aim.SetAiming(true);rig.SetAiming(true);
            Vector3 centre=target.WorldPosition+Vector3.up*rig.targetFocusHeight;
            Vector3 position=centre+Vector3.up*rig.aimingHeight-rig.ViewingDirection*rig.aimingDistance;
            Quaternion rotation=Quaternion.LookRotation(centre-position);
            Vector3 expected=CombatCamera.EntrySeaPoint(position,rotation,target.WorldPosition,5000);
            Assert.That(Vector3.Distance(expected,aim.PlayerAimPoint),Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(barrel,player.Weapon.turrets[0].muzzles[0].rotation),Is.LessThan(.001f));
            aim.Adjust(new Vector2(20,10));Vector3 manual=aim.PlayerAimPoint;
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(manual,aim.PlayerAimPoint,"Camera blend and fire-control refresh cannot overwrite manual aim");
            aim.SetAiming(false);rig.SetAiming(false);aim.SetAiming(true);rig.SetAiming(true);
            Assert.AreEqual(manual,aim.PlayerAimPoint,"Visible manual correction survives re-entry");
            aim.SetPoint(player.WorldPosition+Vector3.left*1000);manual=aim.PlayerAimPoint;
            rig.SetAiming(true);yield return null;Assert.AreEqual(manual,aim.PlayerAimPoint,"Holding Shift never recenters again");
            aim.SetAiming(false);rig.SetAiming(false);aim.SetAiming(true);rig.SetAiming(true);
            Assert.AreNotEqual(manual,aim.PlayerAimPoint,"A new entry restores an offscreen aim");
        }
        [UnityTest]public IEnumerator ActualImpactHudGroupsFrontAndMergesOnlyAllBarrels()
        {
            combatInput.enabled=false;player.GetComponent<PlayerShipInput>().enabled=false;
            player.Movement.enabled=false;player.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
            var hud=Object.FindFirstObjectByType<CombatHUD>();
            Vector3 point=player.WorldPosition+Vector3.right*160;point.y=0;aim.SetPoint(point);
            float deadline=Time.time+9;
            while(Time.time<deadline&&!hud.ImpactsMerged)yield return null;
            Assert.IsTrue(hud.ImpactsMerged,"All actual impacts should converge to a circle");
            Assert.That(Vector3.Distance(hud.FrontImpact,point),Is.LessThan(3));
            Assert.That(Vector3.Distance(hud.RearImpact,point),Is.LessThan(3));
            var t=player.Weapon.turrets[0];t.enabled=false;
            foreach(var barrel in t.barrels)barrel.rotation=Quaternion.Euler(0,20,0)*barrel.rotation;
            yield return null;yield return null;
            Assert.IsFalse(hud.ImpactsMerged,"One divergent front turret must break the circle");
            Vector3 sum=Vector3.zero;
            for(int i=0;i<2;i++)for(int j=0;j<2;j++)
            {Assert.IsTrue(player.Weapon.turrets[i].TryGetImpact(j,out var impact));sum+=impact;}
            Assert.That(Vector3.Distance(hud.FrontImpact,sum/4),Is.LessThan(.001f));
            Assert.That(Vector3.Distance(hud.FrontImpact,hud.RearImpact),Is.GreaterThan(5));
            Assert.That(aim.PlayerAimPoint,Is.EqualTo(point),"Actual impact HUD cannot modify the player's command");
            t.enabled=true;
            deadline=Time.time+4;while(Time.time<deadline&&!hud.ImpactsMerged)yield return null;
            Assert.IsTrue(hud.ImpactsMerged);
        }
        [UnityTest]public IEnumerator ImmediateSalvoUsesActualBarrelsAndReloads()
        {
            player.Weapon.dispersionEnabled=false;aim.SetPoint(player.WorldPosition+Vector3.right*180);
            var directions=new List<Vector3>();var origins=new List<Vector3>();foreach(var t in player.Weapon.turrets)foreach(var m in t.muzzles){directions.Add(m.forward);origins.Add(m.position);}
            int before=Projectile.SpawnedCount;Assert.IsTrue(player.FireWeapon());Assert.That(player.Weapon.LastSalvoCount,Is.EqualTo(6));Assert.That(Projectile.SpawnedCount-before,Is.EqualTo(6));
            var shots=Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);Assert.That(shots.Length,Is.EqualTo(6));
            foreach(var shell in shots)
            {
                int index=origins.FindIndex(p=>Vector3.Distance(p,shell.Origin)<.001f);Assert.That(index,Is.GreaterThanOrEqualTo(0));
                Assert.That(Vector3.Distance(directions[index],(shell.InitialVelocity-player.WorldVelocity).normalized),Is.LessThan(.00001f));
                Assert.That((shell.InitialVelocity-player.WorldVelocity).magnitude,Is.EqualTo(65).Within(.01));
            }
            Assert.IsFalse(player.FireWeapon());Assert.That(player.Weapon.LastSalvoCount,Is.Zero);
            foreach(var t in player.Weapon.turrets)Assert.That(t.ReloadRemaining,Is.GreaterThan(4.9));
            yield return new WaitForSeconds(5.1f);Assert.IsTrue(player.FireWeapon());Assert.That(player.Weapon.LastSalvoCount,Is.LessThanOrEqualTo(6));
        }
        [UnityTest]public IEnumerator MovingAlignedSalvoHitsChosenWorldPoint()
        {
            combatInput.enabled=false;player.GetComponent<PlayerShipInput>().enabled=false;player.Movement.enabled=false;
            player.GetComponent<Rigidbody>().linearVelocity=new Vector3(8,0,12);player.Weapon.dispersionEnabled=false;
            Vector3 point=new Vector3(160,0,-65);aim.SetPoint(point);
            float deadline=Time.time+8;bool ready=false;
            while(Time.time<deadline){ready=true;foreach(var t in player.Weapon.turrets)ready&=t.Aligned;if(ready)break;yield return null;}
            Assert.IsTrue(ready);Vector3 inherited=player.WorldVelocity;Assert.IsTrue(player.FireWeapon());Assert.That(player.Weapon.LastSalvoCount,Is.EqualTo(6));
            foreach(var shell in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            {
                Assert.That((shell.InitialVelocity-inherited).magnitude,Is.EqualTo(65).Within(.01));
                float t=(shell.InitialVelocity.y+Mathf.Sqrt(shell.InitialVelocity.y*shell.InitialVelocity.y+2*9.81f*shell.Origin.y))/9.81f;
                Assert.That(Vector3.Distance(point,CombatBallistics.Position(shell.Origin,shell.InitialVelocity,9.81f,t)),Is.LessThan(.08));
            }
            int splash=Projectile.SplashCount;yield return new WaitForSeconds(5);Assert.That(Projectile.SplashCount-splash,Is.EqualTo(6));
        }
        [UnityTest]public IEnumerator LockShiftDriveManualAimAndSinkUseRealInputAndOop()
        {
            var screen=combatInput.view.WorldToScreenPoint(target.WorldPosition);InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(screen.x,screen.y)}.WithButton(MouseButton.Right));yield return null;yield return null;
            Assert.That(fire.Target,Is.EqualTo(target));Assert.IsTrue(fire.Valid);
            InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift,Key.W,Key.D));yield return null;yield return null;
            Assert.IsTrue(aim.IsAiming);Vector3 old=aim.PlayerAimPoint;
            InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(40,20));yield return null;yield return null;Assert.That(Vector3.Distance(old,aim.PlayerAimPoint),Is.GreaterThan(1));
            Vector3 chosen=aim.PlayerAimPoint;yield return new WaitForSeconds(.7f);Assert.That(aim.PlayerAimPoint,Is.EqualTo(chosen));Assert.That(player.Movement.TargetSpeed,Is.GreaterThan(4));Assert.That(player.Movement.RudderAngle,Is.GreaterThan(10));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;Assert.IsFalse(aim.IsAiming);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift));yield return null;yield return null;Assert.That(aim.PlayerAimPoint,Is.EqualTo(chosen));
            Assert.That(player.GetComponents<Ship>().Length,Is.EqualTo(1));Assert.IsInstanceOf<EnemyShip>(target);Assert.IsNotNull(player.Weapon);Assert.IsNotNull(player.Health);Assert.IsNotNull(player.Movement);
            ((IDamageable)target).TakeDamage(150);yield return null;yield return null;
            Assert.IsNull(fire.Target);Assert.IsFalse(fire.Valid);Assert.IsFalse(fire.observation.Valid);Assert.IsFalse(target.Movement.enabled);Assert.IsFalse(target.Weapon.enabled);Assert.IsFalse(aim.IsAiming);
        }
        [UnityTest]public IEnumerator SweptProjectileDealsOneHitAndRejectsFriendlyDamage()
        {
            Vector3 position=target.WorldPosition;int hp=target.Health.CurrentHealth;int hit=Projectile.DamageCount;
            var shell=Object.Instantiate(player.Weapon.projectile);shell.Launch(position-Vector3.right*20,Vector3.right*1000,9.81f,player,25,false);
            yield return new WaitForSeconds(.1f);Assert.That(target.Health.CurrentHealth,Is.EqualTo(hp-25));Assert.That(Projectile.DamageCount-hit,Is.EqualTo(1));
            target.team=player.team;var friendly=Object.Instantiate(player.Weapon.projectile);friendly.Launch(position-Vector3.right*20,Vector3.right*1000,9.81f,player,25,false);
            yield return new WaitForSeconds(.1f);Assert.That(target.Health.CurrentHealth,Is.EqualTo(hp-25));
            var once=new GameObject("Single resolution").AddComponent<Projectile>();once.ResolveHit(target,25);once.ResolveHit(target,25);Assert.That(target.Health.CurrentHealth,Is.EqualTo(hp-50));Object.Destroy(once.gameObject);
        }
        [UnityTest]public IEnumerator AlignedSixShellSalvoActuallySinksLockedTarget()
        {
            combatInput.enabled=false;player.Weapon.dispersionEnabled=false;
            target.GetComponent<Rigidbody>().position=player.WorldPosition+Vector3.right*160;
            target.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();yield return null;
            fire.Lock(target);aim.SetPoint(target.WorldPosition);
            float timeout=Time.time+8;bool ready=false;
            while(Time.time<timeout){ready=true;foreach(var turret in player.Weapon.turrets)ready&=turret.Aligned;if(ready)break;yield return null;}
            Assert.IsTrue(ready);int hits=Projectile.DamageCount;Assert.IsTrue(player.FireWeapon());Assert.That(player.Weapon.LastSalvoCount,Is.EqualTo(6));
            yield return new WaitForSeconds(4);
            Assert.That(Projectile.DamageCount-hits,Is.EqualTo(6));Assert.That(target.Health.CurrentHealth,Is.Zero);Assert.IsNull(fire.Target);
        }
        [UnityTest]public IEnumerator MechanicalArcsNeverCrossForbiddenSector()
        {
            combatInput.enabled=false;aim.SetPoint(player.WorldPosition+Vector3.back*160);
            for(int i=0;i<200;i++)
            {
                foreach(var t in player.Weapon.turrets){t.Track(.04f);Assert.IsTrue(GunneryMath.InArc(t.LocalYaw,t.neutralYaw,t.traverseArc));}
            }
            var front=player.Weapon.turrets[0];Assert.IsFalse(front.Safe(-player.WorldForward));
            foreach(var b in front.barrels)b.rotation=Quaternion.LookRotation(-player.WorldForward);
            Assert.IsTrue(player.FireWeapon());Assert.That(player.Weapon.LastSalvoCount,Is.EqualTo(4));Assert.That(front.ReloadRemaining,Is.Zero);yield return null;
        }
    }
}
