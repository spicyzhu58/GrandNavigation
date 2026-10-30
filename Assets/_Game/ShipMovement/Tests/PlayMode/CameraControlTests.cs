using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    public class CameraControlTests
    {
        ShipFollowCamera rig;ShipMovement ship;Mouse mouse;Keyboard keyboard;
        readonly List<InputDevice> native=new List<InputDevice>();
        void Set(string name,object value)
        {
            typeof(ShipFollowCamera).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rig,value);
            typeof(ShipFollowCamera).GetMethod("OnValidate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(rig,null);
        }
        [UnitySetUp]public IEnumerator Setup()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/ShipMovementPrototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
            yield return null;rig=Object.FindFirstObjectByType<ShipFollowCamera>();ship=Object.FindFirstObjectByType<ShipMovement>();
            foreach(var d in InputSystem.devices)if(d.enabled && (d is Mouse || d is Keyboard))native.Add(d);
            foreach(var d in native)InputSystem.DisableDevice(d);
            mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();
            Application.runInBackground=true;yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            foreach(var d in native)if(d.added)InputSystem.EnableDevice(d);native.Clear();yield return null;
        }
        IEnumerator WaitForOrbit(float expected)
        {
            float timeout=Time.realtimeSinceStartup+5f;
            while(Mathf.Abs(rig.CurrentDistance-expected)>.25f && Time.realtimeSinceStartup<timeout)yield return null;
            Assert.That(rig.CurrentDistance,Is.EqualTo(expected).Within(.3));
        }
        [UnityTest]public IEnumerator SingleWheelNotchProducesVisibleSmoothZoom()
        {
            Assert.That(InputSystem.settings.scrollDeltaBehavior,Is.EqualTo(InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms));
            yield return new WaitForSeconds(.5f);
            InputSystem.QueueDeltaStateEvent(mouse.scroll,new Vector2(0,1));yield return null;yield return null;
            Assert.That(rig.RequestedDistance,Is.EqualTo(31).Within(.01),"One normalized wheel notch should zoom by 4 metres");
            yield return WaitForOrbit(31);
            InputSystem.QueueDeltaStateEvent(mouse.scroll,new Vector2(0,-1));yield return null;yield return null;
            Assert.That(rig.RequestedDistance,Is.EqualTo(35).Within(.01));
            yield return WaitForOrbit(35);
        }
        [UnityTest]public IEnumerator MouseWheelResetLiveSettingsAndShipIndependence()
        {
            float heading=ship.transform.eulerAngles.y;
            InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(600,-150));yield return null;yield return null;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(heading,rig.RequestedYaw)),Is.GreaterThan(70));
            Assert.That(rig.RequestedPitch,Is.GreaterThan(40));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(heading,ship.transform.eulerAngles.y)),Is.LessThan(.01));
            InputSystem.QueueDeltaStateEvent(mouse.scroll,new Vector2(0,10000));yield return null;yield return null;
            Assert.That(rig.RequestedDistance,Is.EqualTo(15));yield return new WaitForSeconds(.8f);
            Assert.That(rig.CurrentDistance,Is.InRange(14.9f,16f));
            InputSystem.QueueDeltaStateEvent(mouse.scroll,new Vector2(0,-10000));yield return null;yield return null;
            Assert.That(rig.RequestedDistance,Is.EqualTo(65));
            InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(3000,-10000));yield return null;yield return null;
            Assert.That(rig.RequestedPitch,Is.EqualTo(75));Assert.That(rig.RequestedYaw,Is.InRange(0,360));
            Set("invertY",true);float pitch=rig.RequestedPitch;
            InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(0,-100));yield return null;yield return null;
            Assert.That(rig.RequestedPitch,Is.LessThan(pitch));
            Set("maxDistance",10f);Assert.That(rig.RequestedDistance,Is.GreaterThanOrEqualTo(15)); // OnValidate restores max >= min.
            Set("maxDistance",65f);Set("defaultDistance",35f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R));yield return null;yield return null;
            Assert.That(rig.RequestedDistance,Is.EqualTo(35));Assert.That(rig.RequestedPitch,Is.EqualTo(30));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(rig.RequestedYaw,heading)),Is.LessThan(.1));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return WaitForOrbit(35);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
            Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));float releasedYaw=rig.RequestedYaw;
            InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(200,0));yield return null;yield return null;
            Assert.That(rig.RequestedYaw,Is.EqualTo(releasedYaw));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
            Vector3 initial=ship.WorldPosition;float orbitYaw=rig.RequestedYaw;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D));yield return new WaitForSeconds(1.5f);
            Assert.That(Vector3.Distance(initial,ship.WorldPosition),Is.GreaterThan(2));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(rig.RequestedYaw,orbitYaw)),Is.LessThan(.1),"Ship yaw must not drive orbit yaw");
            Assert.That(Vector3.Distance(rig.PivotPosition,ship.WorldPosition+Vector3.up*3),Is.LessThan(2));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        }
        [UnityTest]public IEnumerator ObstacleHullSeaClearanceAndCameraOwnership()
        {
            Set("captureCursor",false);
            InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(0,10000));yield return null;yield return null;
            yield return new WaitForSeconds(.8f);Assert.That(rig.RequestedPitch,Is.EqualTo(10));
            Vector3 origin=rig.PivotPosition,direction=(rig.transform.position-origin).normalized;
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name="Camera test obstacle";obstacle.transform.position=origin+direction*20;obstacle.transform.localScale=new Vector3(14,12,4);
            Physics.SyncTransforms();yield return null;yield return null;
            Assert.That(rig.CurrentDistance,Is.LessThan(20));
            Assert.IsFalse(obstacle.GetComponent<Collider>().bounds.Contains(rig.transform.position));
            Assert.That(rig.transform.position.y,Is.GreaterThanOrEqualTo(1));
            var hull=new Bounds(ship.transform.position,Vector3.zero);
            foreach(var renderer in ship.GetComponentsInChildren<Renderer>())hull.Encapsulate(renderer.bounds);
            Assert.IsFalse(hull.Contains(rig.transform.position));
            Object.Destroy(obstacle);yield return null;yield return WaitForOrbit(35);
            rig.SetControlActive(false);Vector3 saved=rig.transform.position;
            InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(400,200));yield return null;yield return null;
            Assert.That(rig.transform.position,Is.EqualTo(saved));
            rig.SetControlActive(true);yield return null;
        }
    }
}


