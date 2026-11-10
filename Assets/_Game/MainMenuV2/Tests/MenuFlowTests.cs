using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace GrandNavigation.MainMenuV2.Tests
{
    public class MenuFlowTests
    {
        MenuFlowController flow;Mouse mouse;
        readonly List<InputDevice> native=new List<InputDevice>();
        [UnitySetUp]public IEnumerator Setup()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/MainMenuV2.unity",new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;
            flow=Object.FindFirstObjectByType<MenuFlowController>();
            foreach(var d in InputSystem.devices)if(d.enabled && (d is Mouse || d is Keyboard))native.Add(d);
            foreach(var d in native)InputSystem.DisableDevice(d);
            mouse=InputSystem.AddDevice<Mouse>();
            yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(mouse!=null)InputSystem.RemoveDevice(mouse);
            foreach(var d in native)if(d.added)InputSystem.EnableDevice(d);
            native.Clear();yield return null;
        }
        IEnumerator Click(Button button)
        {
            Assert.IsTrue(button.gameObject.activeInHierarchy);Assert.IsTrue(button.interactable);
            Vector2 p=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=p});
            yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(MouseButton.Left));
            yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=p});
            yield return null;yield return null;
        }
        IEnumerator AwaitState(MenuFlowController.MenuState expected)
        {
            float deadline=Time.realtimeSinceStartup+6;int frames=0;
            Vector3 previous=flow.cameraRig.view.transform.position;float maxStep=0;
            while(flow.State!=expected && Time.realtimeSinceStartup<deadline)
            {
                Assert.IsFalse(flow.menuPanel.activeSelf);Assert.IsFalse(flow.preparationPanel.activeSelf);
                yield return null;
                var next=flow.cameraRig.view.transform.position;maxStep=Mathf.Max(maxStep,Vector3.Distance(previous,next));previous=next;frames++;
            }
            Assert.That(flow.State,Is.EqualTo(expected));Assert.That(frames,Is.GreaterThan(20));
            Assert.That(maxStep,Is.LessThan(5),"Camera must traverse the orbit, not teleport.");
            Debug.Log($"Menu transition: frames={frames}, largest frame displacement={maxStep:F3}m");
        }
        [UnityTest]public IEnumerator RealMouseStartGoBackRepeatedClicksAndEditorExit()
        {
            var camera=flow.cameraRig.view;var ship=GameObject.Find("ShipRoot");int scene=SceneManager.GetActiveScene().handle;
            Assert.That(flow.State,Is.EqualTo(MenuFlowController.MenuState.MainMenu));
            yield return Click(flow.startButton);
            Assert.That(flow.State,Is.EqualTo(MenuFlowController.MenuState.TransitionToPreparation));
            for(int i=0;i<20;i++){flow.StartPreparation();flow.Back();}
            yield return AwaitState(MenuFlowController.MenuState.ShipPreparation);
            Assert.IsTrue(flow.preparationPanel.activeSelf);Assert.IsFalse(flow.menuPanel.activeSelf);
            Assert.That(flow.cameraRig.view,Is.SameAs(camera));Assert.That(GameObject.Find("ShipRoot"),Is.SameAs(ship));
            Assert.That((int)SceneManager.GetActiveScene().handle,Is.EqualTo(scene));
            yield return Click(flow.goButton);
            Assert.That(flow.message.text,Is.EqualTo("Gameplay scene not implemented yet."));
            Assert.That((int)SceneManager.GetActiveScene().handle,Is.EqualTo(scene));
            yield return Click(flow.backButton);yield return AwaitState(MenuFlowController.MenuState.MainMenu);
            Assert.That(Vector3.Distance(camera.transform.position,flow.cameraRig.menuPose.transform.position),Is.LessThan(.001));
            yield return Click(flow.startButton);yield return AwaitState(MenuFlowController.MenuState.ShipPreparation);
            yield return Click(flow.backButton);yield return AwaitState(MenuFlowController.MenuState.MainMenu);
            yield return Click(flow.exitButton);
            Assert.IsTrue(Application.isPlaying);Assert.That(flow.message.text,Does.Contain("Editor"));
        }
        [UnityTest]public IEnumerator EntireOrbitFramesHullAndEndsBowAtBottomAtBothResolutions()
        {
            var rig=flow.cameraRig;var cam=rig.view;
            foreach(var resolution in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {
                var texture=new RenderTexture(resolution.x,resolution.y,24);cam.targetTexture=texture;
                try
                {
                    Vector3 old=cam.transform.position;Quaternion rotation=cam.transform.rotation;
                    for(int i=0;i<=240;i++)
                    {
                        rig.Apply(rig.menuPose,rig.preparationPose,i/240f);
                        Assert.That(cam.transform.position.y,Is.GreaterThan(8));
                        Assert.That(new Vector2(cam.transform.position.x,cam.transform.position.z).magnitude,Is.GreaterThan(40));
                        if(i>0){Assert.That(Vector3.Distance(old,cam.transform.position),Is.LessThan(1));Assert.That(Quaternion.Angle(rotation,cam.transform.rotation),Is.LessThan(2));}
                        foreach(float x in new[]{-3.5f,3.5f})foreach(float y in new[]{0f,10f})foreach(float z in new[]{-23f,23f})
                        {
                            var p=cam.WorldToViewportPoint(new Vector3(x,y,z));
                            Assert.That(p.z,Is.GreaterThan(cam.nearClipPlane));
                            Assert.That(p.x,Is.InRange(.01f,.99f),$"x framing t={i/240f}");
                            Assert.That(p.y,Is.InRange(.01f,.99f),$"y framing t={i/240f}");
                        }
                        old=cam.transform.position;rotation=cam.transform.rotation;
                    }
                    var bow=cam.WorldToViewportPoint(new Vector3(0,2,23));var stern=cam.WorldToViewportPoint(new Vector3(0,2,-21));
                    Assert.That(bow.y,Is.LessThan(stern.y));Assert.That(Mathf.Abs(bow.x-stern.x),Is.LessThan(.001));
                    Assert.That(Vector3.Distance(cam.transform.position,rig.preparationPose.transform.position),Is.LessThan(.001));
                }
                finally{cam.targetTexture=null;Object.Destroy(texture);}
            }
            rig.Initialize();yield return null;
        }
    }
}

