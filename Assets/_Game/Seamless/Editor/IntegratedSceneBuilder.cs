using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using GrandNavigation.Gameplay;
using GrandNavigation.Gameplay.Battle;
using GrandNavigation.MainMenuV2;
using GrandNavigation.GameFlow;
namespace GrandNavigation.Seamless.Editor
{
    public static class IntegratedSceneBuilder
    {
        public const string Path="Assets/_Game/Scenes/IntegratedGame.unity";
        [MenuItem("Grand Navigation/Create Integrated Game (new only)")]
        public static void Create()
        {
            if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty||System.IO.File.Exists(Path))throw new System.Exception("Stop Play, save scene; destination must be new.");
            var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/BattleLoop.unity");EditorSceneManager.SaveScene(scene,Path);
            var battle=Object.FindFirstObjectByType<BattleManager>();var orbit=Object.FindFirstObjectByType<ShipFollowCamera>();var camera=orbit.GetComponent<Camera>();
            var source=EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenuV2.unity",OpenSceneMode.Additive);
            var old=Object.FindFirstObjectByType<MenuFlowController>();
            var flow=new GameObject("Game Flow").AddComponent<GameFlowController>();SceneManager.MoveGameObjectToScene(flow.gameObject,scene);
            flow.battle=battle;flow.orbit=orbit;
            flow.rig=old.cameraRig;flow.preparation=old.preparation;
            flow.menuPanel=old.menuPanel;flow.preparationPanel=old.preparationPanel;flow.startButton=old.startButton;flow.goButton=old.goButton;flow.backButton=old.backButton;flow.exitButton=old.exitButton;flow.message=old.message;
            var roots=new[]{old.gameObject,old.menuPanel.transform.root.gameObject,old.cameraRig.gameObject,old.cameraRig.pivot.gameObject,old.cameraRig.menuPose.gameObject,old.cameraRig.preparationPose.gameObject,old.cameraRig.menuPose.lookTarget.gameObject}.Distinct().ToArray();
            foreach(var root in roots)SceneManager.MoveGameObjectToScene(root,scene);
            Vector3 shift=battle.player.transform.position;
            foreach(var t in new[]{flow.rig.pivot,flow.rig.menuPose.transform,flow.rig.preparationPose.transform,flow.rig.menuPose.lookTarget}.Distinct())t.position+=shift;
            flow.preparation.shipModelSlot=battle.player.transform;
            flow.rig.view=camera;
            flow.battlePose=new GameObject("BattlePose").AddComponent<MenuCameraPose>();SceneManager.MoveGameObjectToScene(flow.battlePose.gameObject,scene);flow.battlePose.lookTarget=flow.rig.pivot;
            Object.DestroyImmediate(old);
            EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(scene);
            var routes=ScriptableObject.CreateInstance<GameSceneRoutes>();routes.mainMenuScene=routes.gameplayScene=Path;AssetDatabase.CreateAsset(routes,"Assets/_Game/Seamless/IntegratedRoutes.asset");battle.resultUI.sceneRoutes=routes;
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Path,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=Path)).ToArray();
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
