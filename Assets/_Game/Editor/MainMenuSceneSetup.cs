using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GrandNavigation.Menu.Editor
{
    public static class MainMenuSceneSetup
    {
        const string MenuPath="Assets/_Game/Scenes/MainMenu.unity";
        const string BattlePath="Assets/_GunneryPrototype/Scenes/GunneryPrototype.unity";
        [MenuItem("Grand Navigation/Create Main Menu (new only)")]
        public static void Create()
        {
            if(System.IO.File.Exists(MenuPath)) throw new System.InvalidOperationException("MainMenu already exists; refusing to overwrite.");
            if(EditorSceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save the current scene first.");
            var battle=AssetDatabase.LoadAssetAtPath<SceneAsset>(BattlePath);
            if(!battle) throw new System.InvalidOperationException("Battle scene missing.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Menu Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            camera.tag="MainCamera"; camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.025f,.065f,.12f);
            var canvas=new GameObject("Main Menu Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var title=Text("Title",canvas.transform,font,"Grand Navigation",54,new Vector2(0,65),new Vector2(1000,100));
            title.fontStyle=FontStyle.Bold;title.color=new Color(.88f,.95f,1);
            var go=new GameObject("Start Game",typeof(RectTransform),typeof(Image),typeof(Button));
            go.transform.SetParent(canvas.transform,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(280,64);rect.anchoredPosition=new Vector2(0,-65);
            go.GetComponent<Image>().color=new Color(.06f,.3f,.43f);
            var button=go.GetComponent<Button>();
            var colors=button.colors;colors.highlightedColor=new Color(.65f,.9f,1);colors.pressedColor=new Color(.4f,.7f,.85f);button.colors=colors;
            var label=Text("Label",go.transform,font,"Start Game",26,Vector2.zero,new Vector2(280,64));
            label.raycastTarget=false;
            var controller=new GameObject("Main Menu Controller").AddComponent<MainMenuController>();
            controller.Configure(battle,button);
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            EditorSceneManager.SaveScene(scene,MenuPath);
            var existing=EditorBuildSettings.scenes.Where(s=>s.path!=MenuPath && s.path!=BattlePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(MenuPath,true),new EditorBuildSettingsScene(BattlePath,true)}.Concat(existing).ToArray();
            AssetDatabase.SaveAssets();
        }
        static Text Text(string name,Transform parent,Font font,string content,int size,Vector2 position,Vector2 bounds)
        {
            var text=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent,false);text.rectTransform.sizeDelta=bounds;text.rectTransform.anchoredPosition=position;
            text.font=font;text.text=content;text.fontSize=size;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;
            return text;
        }
    }
}
