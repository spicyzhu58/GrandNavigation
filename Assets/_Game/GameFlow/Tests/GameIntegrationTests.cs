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
using GrandNavigation.MainMenuV2;
using GrandNavigation.Gameplay;
using GrandNavigation.Gameplay.AI;
using GrandNavigation.Gameplay.Battle;
namespace GrandNavigation.GameFlow.Tests
{
    public class GameIntegrationTests
    {
        Mouse mouse;
        readonly List<InputDevice> native=new();
        InputSettings.BackgroundBehavior background;
        InputSettings.EditorInputBehaviorInPlayMode routing;
        [UnitySetUp] public IEnumerator Setup()
        {
            background=InputSystem.settings.backgroundBehavior;routing=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            foreach(var d in InputSystem.devices)if(d.enabled&&(d is Mouse||d is Keyboard))native.Add(d);
            foreach(var d in native)InputSystem.DisableDevice(d);
            mouse=InputSystem.AddDevice<Mouse>();Time.timeScale=1;Application.runInBackground=true;
            yield return SceneManager.LoadSceneAsync("Assets/_Game/Scenes/MainMenuV2.unity");yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;if(mouse!=null)InputSystem.RemoveDevice(mouse);
            foreach(var d in native)if(d.added)InputSystem.EnableDevice(d);native.Clear();
            InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=routing;
            yield return null;
        }
        IEnumerator Click(Button button)
        {
            Assert.IsTrue(button.interactable);Assert.IsTrue(button.gameObject.activeInHierarchy);
            var p=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return null;yield return null;
        }
        IEnumerator AwaitScene(string name)
        {
            float end=Time.realtimeSinceStartup+10;
            while(SceneManager.GetActiveScene().name!=name&&Time.realtimeSinceStartup<end)yield return null;
            Assert.AreEqual(name,SceneManager.GetActiveScene().name);yield return null;yield return null;
            Assert.AreEqual(1,Time.timeScale);
            Assert.AreEqual(1,Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1,Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1,Camera.allCamerasCount);
        }
        [UnityTest]public IEnumerator RepeatedCommandsLoadOnceAndVictoryCanReturn()
        {
            int loads=0;SceneManager.sceneLoaded+=Loaded;
            try
            {
                var menu=Object.FindFirstObjectByType<MenuFlowController>();menu.StartPreparation();
                float deadline=Time.realtimeSinceStartup+8;
                while(menu.State!=MenuFlowController.MenuState.ShipPreparation&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.AreEqual(MenuFlowController.MenuState.ShipPreparation,menu.State);
                for(int i=0;i<20;i++)menu.Go();
                yield return AwaitScene("BattleLoop");Assert.AreEqual(1,loads);
                var battle=Object.FindFirstObjectByType<BattleManager>();
                foreach(var enemy in battle.participants)enemy.TakeDamage(int.MaxValue);
                yield return null;yield return null;Assert.AreEqual(BattleState.Victory,battle.State);
                for(int i=0;i<20;i++)battle.resultUI.ReturnToMainMenu();
                yield return AwaitScene("MainMenuV2");Assert.AreEqual(2,loads);
                Assert.AreEqual(MenuFlowController.MenuState.MainMenu,Object.FindFirstObjectByType<MenuFlowController>().State);
            }
            finally{SceneManager.sceneLoaded-=Loaded;}
            void Loaded(Scene s,LoadSceneMode mode){loads++;}
        }
        [UnityTest]public IEnumerator ThreeFullLoopsUsingInputSystemMouse()
        {
            int loads=0;SceneManager.sceneLoaded+=Loaded;
            try
            {
                for(int round=0;round<3;round++)
                {
                    var menu=Object.FindFirstObjectByType<MenuFlowController>();
                    Assert.AreEqual(MenuFlowController.MenuState.MainMenu,menu.State);
                    Assert.AreEqual(CursorLockMode.None,Cursor.lockState);Assert.IsTrue(Cursor.visible);
                    yield return Click(menu.startButton);
                    float end=Time.realtimeSinceStartup+8;
                    while(menu.State!=MenuFlowController.MenuState.ShipPreparation&&Time.realtimeSinceStartup<end)yield return null;
                    Assert.AreEqual(MenuFlowController.MenuState.ShipPreparation,menu.State);
                    yield return Click(menu.goButton);
                    if(menu){menu.Go();menu.Go();}
                    yield return AwaitScene("BattleLoop");
                    var battle=Object.FindFirstObjectByType<BattleManager>();
                    Assert.AreEqual(1,Object.FindObjectsByType<BattleManager>(FindObjectsSortMode.None).Length);
                    Assert.AreEqual(1,Object.FindObjectsByType<PlayerShip>(FindObjectsSortMode.None).Length);
                    Assert.AreEqual(3,Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None).Length);
                    Assert.AreEqual(BattleState.Playing,battle.State);Assert.AreEqual(3,battle.RemainingEnemies);
                    Assert.AreEqual(15,battle.player.Movement.settings.maxForwardSpeed);
                    Assert.IsNull(Object.FindFirstObjectByType<CombatFireControl>().Target);
                    Assert.AreEqual(CursorLockMode.Locked,Cursor.lockState);
                    foreach(var ai in Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None))Assert.IsTrue(ai.enabled);
                    battle.player.Movement.SetTargetSpeed(5);yield return new WaitForSeconds(.3f);
                    Assert.Greater(battle.player.WorldVelocity.magnitude,0);
                    battle.player.Weapon.SetAim(battle.player.WorldPosition+Vector3.forward*600);
                    Assert.IsTrue(battle.player.FireWeapon());
                    foreach(var enemy in battle.participants)enemy.TakeDamage(int.MaxValue);
                    yield return null;yield return null;
                    Assert.AreEqual(BattleState.Victory,battle.State);Assert.AreEqual(0,Time.timeScale);
                    yield return Click(battle.resultUI.retryButton);yield return AwaitScene("BattleLoop");
                    battle=Object.FindFirstObjectByType<BattleManager>();
                    Assert.AreEqual(BattleState.Playing,battle.State);Assert.AreEqual(3,battle.RemainingEnemies);
                    Assert.AreEqual(15,battle.player.Movement.settings.maxForwardSpeed);
                    Assert.AreEqual(battle.player.Health.maximumHealth,battle.player.Health.CurrentHealth);
                    Assert.AreEqual(0,Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length);
                    battle.player.TakeDamage(int.MaxValue);yield return null;yield return null;
                    Assert.AreEqual(BattleState.Defeat,battle.State);
                    var ui=battle.resultUI;yield return Click(ui.mainMenuButton);
                    if(ui){ui.ReturnToMainMenu();ui.ReturnToMainMenu();}
                    yield return AwaitScene("MainMenuV2");
                    Assert.AreEqual(0,Object.FindObjectsByType<BattleManager>(FindObjectsSortMode.None).Length);
                    Assert.AreEqual(0,Object.FindObjectsByType<PlayerShip>(FindObjectsSortMode.None).Length);
                    Assert.AreEqual(0,Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None).Length);
                }
                Assert.AreEqual(9,loads,"Exactly GO, Retry and Return loads per round.");
            }
            finally{SceneManager.sceneLoaded-=Loaded;}
            void Loaded(Scene s,LoadSceneMode mode){loads++;}
        }
    }
}
