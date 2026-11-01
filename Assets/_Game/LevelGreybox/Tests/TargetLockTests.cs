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
namespace GrandNavigation.Gameplay.Levels.Tests
{
    public class TargetLockTests
    {
        LevelDebug level;PlayerCombatInput input;Ship target;Camera camera;
        Mouse mouse;readonly List<InputDevice> native=new List<InputDevice>();
        [UnitySetUp]public IEnumerator Setup()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/LevelGreybox.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
            yield return null;Application.runInBackground=true;
            level=Object.FindFirstObjectByType<LevelDebug>();input=Object.FindFirstObjectByType<PlayerCombatInput>();camera=input.view;
            foreach(var d in InputSystem.devices)if(d.enabled&&(d is Mouse||d is Keyboard))native.Add(d);
            foreach(var d in native)InputSystem.DisableDevice(d);mouse=InputSystem.AddDevice<Mouse>();
            level.player.GetComponent<PlayerShipInput>().enabled=false;level.player.Movement.enabled=false;
            level.player.GetComponent<Rigidbody>().position=Vector3.zero;
            level.cameraRig.enabled=false;level.cameraRig.orbit.enabled=false;
            camera.transform.SetPositionAndRotation(new Vector3(0,1,0),Quaternion.identity);camera.fieldOfView=60;
            foreach(var island in Object.FindObjectsByType<LevelIsland>(FindObjectsSortMode.None))island.solid.GetComponent<Collider>().enabled=false;
            foreach(var t in level.targets)t.gameObject.SetActive(false);
            target=level.targets[0];target.gameObject.SetActive(true);
            Place(target,new Vector3(0,1,600));yield return new WaitForFixedUpdate();yield return null;
        }
        void Place(Ship t,Vector3 p){t.transform.SetPositionAndRotation(p,Quaternion.identity);var b=t.GetComponent<Rigidbody>();b.position=p;b.rotation=Quaternion.identity;Physics.SyncTransforms();}
        [UnityTearDown]public IEnumerator Cleanup(){InputSystem.RemoveDevice(mouse);foreach(var d in native)if(d.added)InputSystem.EnableDevice(d);native.Clear();yield return null;}
        Vector2 Centre(Ship t)=>camera.WorldToScreenPoint(t.GetComponent<Collider>().bounds.center);
        Vector2 Edge(Ship t,float offset)
        {
            Bounds b=t.GetComponent<Collider>().bounds;float x=float.NegativeInfinity;
            for(int i=0;i<8;i++)x=Mathf.Max(x,camera.WorldToScreenPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))).x);
            return new Vector2(x+offset,Centre(t).y);
        }
        GameObject Block(Vector3 position,Vector3 size)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Lock test occluder";go.transform.position=position;go.transform.localScale=size;Physics.SyncTransforms();return go;
        }
        [UnityTest]public IEnumerator PixelRadiusAndRightClickDoNotChangeAim()
        {
            Vector3 chosen=level.aim.PlayerAimPoint;
            foreach(float offset in new[]{0f,30,50,54.9f})
            {
                level.fire.Lock(null);Vector2 pointer=offset==0?Centre(target):Edge(target,offset);
                Assert.That(input.SelectTargetAt(pointer),Is.EqualTo(target),"offset "+offset);
                Assert.That(level.aim.PlayerAimPoint,Is.EqualTo(chosen));
            }
            level.fire.Lock(null);Assert.IsNull(input.SelectTargetAt(Edge(target,55.1f)));Assert.IsNull(level.fire.Target);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=Edge(target,30)}.WithButton(MouseButton.Right));
            yield return null;yield return null;Assert.That(level.fire.Target,Is.EqualTo(target));
            Assert.IsNull(input.SelectTargetAt(new Vector2(5,5)));Assert.That(level.fire.Target,Is.EqualTo(target),"Miss retains legal lock");
            Assert.That(level.aim.PlayerAimPoint,Is.EqualTo(chosen));
        }
        [UnityTest]public IEnumerator DistanceUsesPlayerHorizontalRangeAndViewport()
        {
            foreach(float d in new[]{400f,800,1200})
            {
                Place(target,new Vector3(0,1,d));yield return new WaitForFixedUpdate();yield return null;
                level.fire.Lock(null);Assert.That(input.SelectTargetAt(Edge(target,30)),Is.EqualTo(target),"distance "+d);
            }
            Place(target,new Vector3(0,1,1200.2f));yield return new WaitForFixedUpdate();yield return null;
            level.fire.Lock(null);Assert.IsNull(input.SelectTargetAt(Centre(target)));
            Place(target,new Vector3(0,1,-600));yield return new WaitForFixedUpdate();yield return null;
            Assert.IsNull(input.SelectTargetAt(new Vector2(camera.pixelWidth/2,camera.pixelHeight/2)));
            Place(target,new Vector3(0,1,800));yield return new WaitForFixedUpdate();yield return null;
            camera.rect=new Rect(.1f,.1f,.8f,.8f);
            foreach(float fov in new[]{18f,55f})
            {
                camera.fieldOfView=fov;Assert.That(input.SelectTargetAt(Edge(target,50)),Is.EqualTo(target));
            }
        }
        [UnityTest]public IEnumerator ExactPriorityAndClosestCandidateSwitchFireControl()
        {
            var second=level.targets[1];second.gameObject.SetActive(true);
            Place(second,new Vector3(12,1,600));yield return new WaitForFixedUpdate();yield return null;
            Vector3 chosen=level.aim.PlayerAimPoint;
            Assert.That(input.SelectTargetAt(Centre(second)),Is.EqualTo(second));
            Assert.That(level.fire.Target,Is.EqualTo(second));
            Assert.That(input.SelectTargetAt(Edge(target,-1)),Is.EqualTo(target));
            Assert.That(level.fire.Target,Is.EqualTo(target));Assert.IsTrue(level.fire.observation.Valid);
            Assert.That(level.aim.PlayerAimPoint,Is.EqualTo(chosen));
            // Both candidates are in the assist radius; the closer projected hull wins.
            Assert.That(input.SelectTargetAt(Edge(second,20)),Is.EqualTo(second));
        }
        [UnityTest]public IEnumerator FullAndPartialOcclusionTriggersAndWater()
        {
            var blocker=Block(new Vector3(0,1,300),new Vector3(30,30,5));
            Assert.IsNull(input.SelectTargetAt(Centre(target)));Assert.IsNull(input.SelectTargetAt(Edge(target,30)));
            blocker.transform.localScale=new Vector3(2,30,5);Physics.SyncTransforms();
            // Centre blocked, hull sides visible.
            Assert.That(input.SelectTargetAt(Centre(target)),Is.EqualTo(target));
            blocker.transform.localScale=new Vector3(30,30,5);blocker.GetComponent<Collider>().isTrigger=true;Physics.SyncTransforms();
            Assert.That(input.SelectTargetAt(Centre(target)),Is.EqualTo(target));
            blocker.GetComponent<Collider>().isTrigger=false;blocker.layer=4;Physics.SyncTransforms();
            Assert.That(input.SelectTargetAt(Centre(target)),Is.EqualTo(target));
            blocker.layer=5;Physics.SyncTransforms();Assert.That(input.SelectTargetAt(Centre(target)),Is.EqualTo(target));
            Object.Destroy(blocker);yield return null;
        }
        [UnityTest]public IEnumerator DeadFriendlyInactiveAndDestroyedTargetsAreRejected()
        {
            target.team=level.player.team;Assert.IsNull(input.SelectTargetAt(Centre(target)));
            target.team=1;Assert.That(input.SelectTargetAt(Centre(target)),Is.EqualTo(target));
            target.enabled=false;yield return null;Assert.IsNull(level.fire.Target);Assert.IsNull(input.SelectTargetAt(Centre(target)));
            target.enabled=true;input.SelectTargetAt(Centre(target));
            target.TakeDamage(1000);yield return null;yield return null;
            Assert.IsNull(level.fire.Target);Assert.IsNull(input.SelectTargetAt(Centre(target)));
            var second=level.targets[1];Place(second,new Vector3(0,1,600));second.gameObject.SetActive(true);
            yield return null;Assert.That(input.SelectTargetAt(Centre(second)),Is.EqualTo(second));
            second.gameObject.SetActive(false);yield return null;Assert.IsNull(level.fire.Target);
            second.gameObject.SetActive(true);yield return null;input.SelectTargetAt(Centre(second));
            Object.Destroy(second.gameObject);yield return null;yield return null;Assert.IsNull(level.fire.Target);
        }
    }
}
