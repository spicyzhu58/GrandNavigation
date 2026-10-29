using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace GrandNavigation.Gunnery.Tests
{
    public class InputFlowTests
    {
        Mouse mouse; Keyboard keyboard;
        readonly System.Collections.Generic.List<InputDevice> nativeDevices=new System.Collections.Generic.List<InputDevice>();
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(mouse!=null) InputSystem.RemoveDevice(mouse);
            if(keyboard!=null) InputSystem.RemoveDevice(keyboard);
            foreach(var device in nativeDevices) if(device.added) InputSystem.EnableDevice(device);
            nativeDevices.Clear();
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            yield return null;
        }
        [UnityTest] public IEnumerator ClickLockRightAimOneClickSalvoHoldDoesNotRepeatEscapeAndReset()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_GunneryPrototype/Scenes/GunneryPrototype.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
            yield return null;
            var s=Object.FindFirstObjectByType<PrototypeSession>();
            var w=s.GetComponent<WeaponManager>();
            s.target.SetMode(TargetMotion.MotionMode.Stationary,0); s.ResetBattle();
            // Isolate injected input from native device events; restored by UnityTearDown even on assertion failure.
            foreach(var device in InputSystem.devices)
                if(device.enabled && (device is Mouse || device is Keyboard)) nativeDevices.Add(device);
            foreach(var device in nativeDevices) InputSystem.DisableDevice(device);
            mouse=InputSystem.AddDevice<Mouse>(); keyboard=InputSystem.AddDevice<Keyboard>();
            yield return new WaitForSeconds(.8f);
            Vector2 p=s.cameraRig.view.WorldToScreenPoint(s.target.transform.position+Vector3.up*1.5f);
            InputSystem.QueueStateEvent(mouse,new MouseState { position=p }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            Assert.IsTrue(s.selection.HasTarget,$"Mouse click must raycast-lock ship. Point={p}, viewport={Screen.width}x{Screen.height}, virtualCurrent={Mouse.current==mouse}");
            InputSystem.QueueStateEvent(mouse,new MouseState { position=p }); yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState { position=p }.WithButton(MouseButton.Right));
            yield return null; yield return null;
            Assert.IsTrue(s.aim.IsAiming,"Right-click must enter aiming.");
            InputSystem.QueueStateEvent(mouse,new MouseState { position=p }); yield return null;
            Vector3 before=s.aim.PlayerAimPoint;
            InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(4,2)); yield return null; yield return null;
            Assert.That(Vector3.Distance(before,s.aim.PlayerAimPoint),Is.GreaterThan(.1));
            yield return new WaitForSeconds(.6f);
            InputSystem.QueueStateEvent(mouse,new MouseState { position=p }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            Assert.That(ProjectileController.SpawnedCount,Is.EqualTo(6),"A single click should fire a complete salvo.");
            yield return new WaitForSeconds(5.3f);
            Assert.That(ProjectileController.SpawnedCount,Is.EqualTo(6),"Holding must not fire another salvo after reload.");
            InputSystem.QueueStateEvent(mouse,new MouseState { position=p }); yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); yield return null; yield return null;
            Assert.IsFalse(s.aim.IsAiming); Assert.IsFalse(s.selection.HasTarget);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R)); yield return null; yield return null;
            Assert.That(s.target.GetComponent<Health>().CurrentHP,Is.EqualTo(600));
            Assert.That(ProjectileController.SpawnedCount,Is.Zero);
        }
    }
}
