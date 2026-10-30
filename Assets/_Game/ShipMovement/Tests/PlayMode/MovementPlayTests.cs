using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace GrandNavigation.Gameplay.Tests
{
    public class MovementPlayTests
    {
        ShipMovement movement;PlayerShipInput input;Rigidbody body;Keyboard keyboard;
        readonly List<InputDevice> native=new List<InputDevice>();
        [UnitySetUp] public IEnumerator Setup()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/ShipMovementPrototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
            yield return null;
            movement=Object.FindFirstObjectByType<ShipMovement>();input=movement.GetComponent<PlayerShipInput>();body=movement.GetComponent<Rigidbody>();
            foreach(var device in InputSystem.devices)if(device.enabled && device is Keyboard)native.Add(device);
            foreach(var device in native)InputSystem.DisableDevice(device);
            keyboard=InputSystem.AddDevice<Keyboard>();
            Application.runInBackground=true;
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            foreach(var device in native)if(device.added)InputSystem.EnableDevice(device);
            native.Clear();yield return null;
        }
        void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        void Place(Vector3 position,Vector3 velocity)
        {
            body.position=position;body.rotation=Quaternion.identity;body.linearVelocity=velocity;body.angularVelocity=Vector3.zero;
            Physics.SyncTransforms();
        }
        [UnityTest]public IEnumerator KeyboardTargetHoldCancellationAndReverseCrossZero()
        {
            Keys(Key.W);yield return new WaitForSeconds(.6f);
            Assert.That(movement.TargetSpeed,Is.InRange(5.5f,7f));Assert.That(movement.ActualSpeed,Is.InRange(1.5f,3f));
            Keys();yield return null;yield return new WaitForFixedUpdate();float held=movement.TargetSpeed;
            yield return new WaitForSeconds(.3f);Assert.That(movement.TargetSpeed,Is.EqualTo(held));
            Keys(Key.W,Key.S);yield return new WaitForSeconds(.2f);Assert.That(movement.TargetSpeed,Is.EqualTo(held));
            Keys(Key.S);
            bool zeroSeen=false;float old=movement.ActualSpeed;float deadline=Time.time+6;
            while(Time.time<deadline && movement.ActualSpeed>=-.5f)
            {
                yield return new WaitForFixedUpdate();float speed=movement.ActualSpeed;
                if(Mathf.Abs(speed)<.13f)zeroSeen=true;
                Assert.That(Mathf.Abs(speed-old),Is.LessThan(.3));old=speed;
            }
            Assert.IsTrue(zeroSeen);Assert.That(movement.ActualSpeed,Is.LessThan(-.5));
            Keys();yield return null;
            Assert.That(movement.TargetSpeed,Is.InRange(-8,25));
        }
        [UnityTest]public IEnumerator RudderReturnsYawDecaysReverseTurnsOppositeAndSideSlipDamps()
        {
            Keys(Key.D);yield return new WaitForSeconds(.6f);
            Assert.That(movement.RudderAngle,Is.EqualTo(35).Within(.1));Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,body.rotation.eulerAngles.y)),Is.LessThan(.1));
            Keys(Key.A,Key.D);yield return new WaitForSeconds(.5f);Assert.That(movement.RudderAngle,Is.Zero.Within(.1));
            input.enabled=false;Place(new Vector3(-80,1,-70),Vector3.forward*15);movement.SetTargetSpeed(15);movement.SetSteeringInput(1);
            yield return new WaitForSeconds(1.2f);
            Assert.That(movement.TurnRate,Is.GreaterThan(8));Assert.That(Mathf.Abs(movement.LateralSpeed),Is.GreaterThan(.5));
            movement.SetSteeringInput(0);float rate=movement.TurnRate;
            yield return new WaitForFixedUpdate();Assert.That(movement.TurnRate,Is.GreaterThan(0));
            yield return new WaitForSeconds(1.3f);Assert.That(movement.RudderAngle,Is.Zero.Within(.1));Assert.That(movement.TurnRate,Is.Zero.Within(.1));
            float lateral=Mathf.Abs(movement.LateralSpeed);yield return new WaitForSeconds(1);
            Assert.That(Mathf.Abs(movement.LateralSpeed),Is.LessThan(lateral*.5f));
            Place(new Vector3(-80,1,-70),Vector3.back*6);movement.SetTargetSpeed(-6);movement.SetSteeringInput(1);
            yield return new WaitForSeconds(1);
            Assert.That(movement.TurnRate,Is.LessThan(-1));Assert.That(Mathf.DeltaAngle(0,body.rotation.eulerAngles.y),Is.LessThan(-1));
            Assert.That(body.position.y,Is.EqualTo(1).Within(.001));
        }
        [UnityTest]public IEnumerator ContinuousThrustAgainstIslandCannotPenetrateAndGlancingContactSlides()
        {
            input.enabled=false;
            var island=GameObject.Find("IslandObstacle").GetComponent<Collider>();var hull=movement.GetComponent<Collider>();
            Place(new Vector3(0,1,17),Vector3.forward*25);movement.SetTargetSpeed(25);
            for(int i=0;i<130;i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(body.position.z,Is.LessThan(25.12f));
                bool overlap=Physics.ComputePenetration(hull,body.position,body.rotation,island,island.transform.position,island.transform.rotation,out _,out float depth);
                Assert.IsFalse(overlap && depth>.06f,$"Hull penetration {depth}");
                Assert.That(Mathf.Abs(body.linearVelocity.y),Is.LessThan(.01));Assert.That(body.linearVelocity.magnitude,Is.LessThan(25.1f));
            }
            Place(new Vector3(15,1,22),new Vector3(8,0,18));movement.SetTargetSpeed(18);
            yield return new WaitForSeconds(.6f);
            Assert.That(body.position.x,Is.GreaterThan(17));Assert.That(body.position.z,Is.LessThan(25.12f));
            Assert.That(body.linearVelocity.x,Is.GreaterThan(1),"Tangential motion must survive contact.");
        }
        [UnityTest]public IEnumerator BoundaryStopsShipAndCameraHudTrackPhysicsRoot()
        {
            input.enabled=false;Place(new Vector3(100,1,177),Vector3.forward*25);movement.SetTargetSpeed(25);
            yield return new WaitForSeconds(1.8f);
            Assert.That(body.position.z,Is.LessThan(185.12f));
            var follow=Object.FindFirstObjectByType<ShipFollowCamera>();var hud=Object.FindFirstObjectByType<ShipMovementHUD>();
            Assert.That(follow.target,Is.EqualTo(movement.transform));Assert.That(Vector3.Distance(follow.transform.position,movement.transform.TransformPoint(follow.offset)),Is.LessThan(2));
            Assert.That(hud.readout.text,Does.Contain("Speed").And.Contain("Rudder"));
            Assert.That(movement.GetComponents<Ship>().Length,Is.EqualTo(1));Assert.IsInstanceOf<PlayerShip>(movement.GetComponent<Ship>());
            Assert.That(movement.WorldVelocity,Is.EqualTo(body.linearVelocity));
        }
    }
}
