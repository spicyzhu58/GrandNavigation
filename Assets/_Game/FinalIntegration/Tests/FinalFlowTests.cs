using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GrandNavigation.Gameplay;
using GrandNavigation.Seamless;
using GrandNavigation.Gameplay.Water;
using GrandNavigation.Gameplay.AI;
using GrandNavigation.Gameplay.Battle;
namespace GrandNavigation.FinalIntegration.Tests
{
    public class FinalFlowTests
    {
        const string Scene="Assets/_Game/Scenes/FinalIntegratedGame.unity";
        Mouse mouse;Keyboard keyboard;readonly List<InputDevice> native=new();
        InputSettings.BackgroundBehavior background;InputSettings.EditorInputBehaviorInPlayMode routing;
        [UnitySetUp]public IEnumerator Setup()
        {
            Time.timeScale=1;Application.runInBackground=true;
            background=InputSystem.settings.backgroundBehavior;routing=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            foreach(var d in InputSystem.devices)if(d.enabled&&(d is Mouse||d is Keyboard))native.Add(d);
            foreach(var d in native)InputSystem.DisableDevice(d);
            mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync(Scene);yield return null;yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);
            foreach(var d in native)if(d.added)InputSystem.EnableDevice(d);native.Clear();
            InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=routing;yield return null;
        }
        IEnumerator Click(Button button)
        {
            var p=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return null;yield return null;
        }
        IEnumerator WaitStage(GameFlowController flow,GameStage stage)
        {
            float limit=Time.realtimeSinceStartup+12;
            Vector3 old=flow.rig.view.transform.position;
            while(flow.Stage!=stage&&Time.realtimeSinceStartup<limit)
            {
                yield return null;
                Assert.Less(Vector3.Distance(old,flow.rig.view.transform.position),8,"Continuous orbit frame displacement");old=flow.rig.view.transform.position;
            }
            Assert.AreEqual(stage,flow.Stage);
        }
        IEnumerator Reloaded(GameFlowController old)
        {
            float limit=Time.realtimeSinceStartup+10;
            while(old&&Time.realtimeSinceStartup<limit)yield return null;
            Assert.IsFalse(old);yield return null;yield return null;
        }
        void Counts()
        {
            Assert.AreEqual(1,Object.FindObjectsByType<PlayerShip>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1,Object.FindObjectsByType<GameFlowController>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1,Object.FindObjectsByType<BattleManager>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1,Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1,Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1,Camera.allCamerasCount);
        }
        [UnityTest]public IEnumerator TwoFinalArtLoopsWithRealUIAndHandoff()
        {
            for(int round=0;round<2;round++)
            {
                var flow=Object.FindFirstObjectByType<GameFlowController>();Counts();
                Assert.AreEqual(GameStage.MainMenu,flow.Stage);Assert.AreEqual(1,Time.timeScale);
                Assert.IsTrue(Object.FindFirstObjectByType<WaterSceneContext>().enabled);Assert.AreEqual(4,flow.battle.player.GetComponent<Weapon>().turrets.Length);
                var player=flow.battle.player;var camera=flow.rig.view;int scene=SceneManager.GetActiveScene().handle;
                var npcs=Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None);
                var positions=new Vector3[npcs.Length];for(int i=0;i<npcs.Length;i++){positions[i]=npcs[i].transform.position;Assert.IsFalse(npcs[i].enabled);}
                var spawn=player.transform.position;int shots=Projectile.SpawnedCount;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));
                yield return new WaitForSeconds(.3f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                Assert.AreEqual(spawn,player.transform.position);Assert.AreEqual(shots,Projectile.SpawnedCount);
                for(int i=0;i<npcs.Length;i++)Assert.AreEqual(positions[i],npcs[i].transform.position);
                Assert.IsFalse(flow.battle.enabled);Assert.AreEqual(BattleState.Playing,flow.battle.State);
                yield return Click(flow.startButton);yield return WaitStage(flow,GameStage.Preparation);
                if(round==0){yield return Click(flow.backButton);yield return WaitStage(flow,GameStage.MainMenu);yield return Click(flow.startButton);yield return WaitStage(flow,GameStage.Preparation);}
                yield return Click(flow.goButton);for(int i=0;i<10;i++)flow.Go();
                Assert.AreEqual(GameStage.Transition,flow.Stage);Assert.IsFalse(player.Weapon.enabled);
                yield return WaitStage(flow,GameStage.Playing);
                Vector3 end=camera.transform.position;Quaternion rotation=camera.transform.rotation;float fov=camera.fieldOfView;
                yield return null;yield return null;
                Assert.Less(Vector3.Distance(end,camera.transform.position),.1f,"No handoff translation snap");
                Assert.Less(Quaternion.Angle(rotation,camera.transform.rotation),.1f,"No handoff rotation snap");Assert.AreEqual(fov,camera.fieldOfView);
                Assert.AreSame(player,flow.battle.player);Assert.AreSame(camera,flow.rig.view);Assert.AreEqual(scene,(int)SceneManager.GetActiveScene().handle);
                Assert.AreEqual(15,player.Movement.settings.maxForwardSpeed);Assert.IsTrue(player.Movement.enabled);
                foreach(var ai in npcs)Assert.IsTrue(ai.enabled);
                Assert.IsTrue(Object.FindFirstObjectByType<CombatHUD>().enabled);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return new WaitForSeconds(.4f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                Assert.Greater(player.WorldVelocity.magnitude,0);
                player.Weapon.SetAim(player.WorldPosition+Vector3.forward*600);Assert.IsTrue(player.FireWeapon());
                foreach(var enemy in flow.battle.participants)enemy.TakeDamage(int.MaxValue);yield return null;yield return null;
                Assert.AreEqual(GameStage.Result,flow.Stage);Assert.AreEqual(BattleState.Victory,flow.battle.State);
                yield return Click(flow.battle.resultUI.retryButton);yield return Reloaded(flow);
                flow=Object.FindFirstObjectByType<GameFlowController>();Counts();Assert.AreEqual(GameStage.Playing,flow.Stage);Assert.AreEqual(1,Time.timeScale);
                Assert.AreEqual(3,flow.battle.RemainingEnemies);Assert.IsNull(Object.FindFirstObjectByType<CombatFireControl>().Target);
                Assert.AreEqual(0,Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length);
                flow.battle.player.TakeDamage(int.MaxValue);yield return null;yield return null;Assert.AreEqual(BattleState.Defeat,flow.battle.State);
                yield return Click(flow.battle.resultUI.mainMenuButton);yield return Reloaded(flow);
                flow=Object.FindFirstObjectByType<GameFlowController>();Assert.AreEqual(GameStage.MainMenu,flow.Stage);Assert.AreEqual(1,Time.timeScale);
                Assert.IsTrue(Object.FindFirstObjectByType<WaterSceneContext>().enabled);Assert.AreEqual(4,flow.battle.player.GetComponent<Weapon>().turrets.Length);
            }
        }
    }
}

