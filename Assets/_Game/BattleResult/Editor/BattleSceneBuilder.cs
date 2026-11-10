using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using GrandNavigation.Gameplay.AI;
namespace GrandNavigation.Gameplay.Battle.Editor
{
    public static class BattleSceneBuilder
    {
        public const string Path = "Assets/_Game/Scenes/BattleLoop.unity";
        [MenuItem("Grand Navigation/Create Battle Loop (new only)")]
        public static void Create()
        {
            if (System.IO.File.Exists(Path) || EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save scene, stop Play; destination must not exist.");
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/EnemyAIGreybox.unity");
            EditorSceneManager.SaveScene(scene, Path);
            foreach (var debug in Object.FindObjectsByType<EnemyAIDebug>(FindObjectsSortMode.None)) debug.enabled = false;
            var manager = new GameObject("Battle Manager").AddComponent<BattleManager>();
            manager.player = Object.FindFirstObjectByType<PlayerShip>();
            manager.participants = Object.FindObjectsByType<EnemyShip>(FindObjectsSortMode.InstanceID);
            var canvasGo = new GameObject("Battle Result UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ResultUI));
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);
            var ui = canvasGo.GetComponent<ResultUI>(); manager.resultUI = ui;
            var panel = new GameObject("Result Overlay", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvasGo.transform,false);
            var rect = (RectTransform)panel.transform; rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            panel.GetComponent<Image>().color = new Color(.015f,.035f,.07f,.88f); ui.panel=panel;
            ui.title = Label(panel.transform,"Result Title","VICTORY",new Vector2(0,100),new Vector2(650,100),64);
            ui.retryButton = Button(panel.transform,"Retry",new Vector2(0,-25));
            ui.mainMenuButton = Button(panel.transform,"Main Menu",new Vector2(0,-115)); ui.mainMenuButton.interactable=false;
            panel.SetActive(false);
            if (!Object.FindFirstObjectByType<EventSystem>()) new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(Path,true)}).ToArray();
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        static Text Label(Transform parent,string name,string value,Vector2 pos,Vector2 size,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=size;rect.anchoredPosition=pos;
            var text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;return text;
        }
        static Button Button(Transform parent,string name,Vector2 pos)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(340,70);rect.anchoredPosition=pos;
            go.GetComponent<Image>().color=new Color(.12f,.27f,.38f);
            Label(go.transform,"Label",name,Vector2.zero,new Vector2(340,70),30);
            return go.GetComponent<Button>();
        }
    }
}
