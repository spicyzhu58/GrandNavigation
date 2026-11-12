using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace GrandNavigation.Battle2D.Editor
{
    public static class Loop2DSetup
    {
        public static void Apply()
        {
            if(EditorApplication.isPlaying||Object.FindFirstObjectByType<BattleManager2D>())throw new System.Exception("Stop Play; manager must not already exist.");
            var manager=new GameObject("Battle Manager 2D").AddComponent<BattleManager2D>();manager.player=Object.FindFirstObjectByType<PlayerInput2D>().ship;
            manager.enemies=Object.FindObjectsByType<ShipActor2D>(FindObjectsSortMode.None).Where(s=>s.team!=manager.player.team).ToArray();
            var canvas=new GameObject("2D Game UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
            manager.startPanel=Panel(canvas.transform,"Start Panel");Text(manager.startPanel.transform,"GRAND NAVIGATION 2D",70,40);manager.startButton=Button(manager.startPanel.transform,"Start Game",-30);
            manager.quitButton=Button(manager.startPanel.transform,"Quit",-120);
            manager.resultPanel=Panel(canvas.transform,"Result Panel");manager.title=Text(manager.resultPanel.transform,"VICTORY",70,48);manager.retryButton=Button(manager.resultPanel.transform,"RETRY",-30);manager.resultPanel.SetActive(false);
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        static GameObject Panel(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;go.GetComponent<Image>().color=new Color(.015f,.04f,.07f,.9f);return go;
        }
        static Text Text(Transform parent,string value,float y,int size)
        {
            var go=new GameObject(value,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(650,90);rect.anchoredPosition=new Vector2(0,y);var t=go.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;
        }
        static Button Button(Transform parent,string value,float y)
        {
            var go=new GameObject(value,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(280,70);rect.anchoredPosition=new Vector2(0,y);go.GetComponent<Image>().color=new Color(.1f,.3f,.5f);Text(go.transform,value,0,28);return go.GetComponent<Button>();
        }
    }
}
