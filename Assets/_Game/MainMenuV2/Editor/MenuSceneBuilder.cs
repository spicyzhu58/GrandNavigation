using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace GrandNavigation.MainMenuV2.Editor
{
    public static class MenuSceneBuilder
    {
        const string Root="Assets/_Game/MainMenuV2/";
        public const string ScenePath="Assets/_Game/Scenes/MainMenuV2.unity";
        static Material hull,deck,dark,trim;
        [MenuItem("Grand Navigation/Create MainMenuV2 (new only)")]
        public static void Create()
        {
            if(System.IO.File.Exists(ScenePath))throw new System.InvalidOperationException("Scene already exists; no overwrite.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Current scene has unsaved changes.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            hull=Mat("Hull",new Color(.38f,.46f,.5f),.3f);
            deck=Mat("Deck",new Color(.24f,.3f,.33f),.15f);
            dark=Mat("DarkSteel",new Color(.09f,.14f,.18f),.2f);
            trim=Mat("Trim",new Color(.58f,.64f,.64f),.2f);
            var sea=Mat("Ocean",new Color(.018f,.065f,.105f),0);sea.shader=Shader.Find("Universal Render Pipeline/Unlit");
            Primitive("Ocean",null,PrimitiveType.Plane,new Vector3(0,-.15f,0),new Vector3(400,1,400),sea);
            RenderSettings.skybox=null;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.34f,.43f,.55f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=160;RenderSettings.fogEndDistance=550;
            RenderSettings.fogColor=new Color(.055f,.12f,.19f);
            var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2.1f;
            sun.color=new Color(.83f,.91f,1);sun.transform.rotation=Quaternion.Euler(35,-35,0);sun.shadows=LightShadows.Soft;
            var ship=new GameObject("ShipRoot");
            var slot=new GameObject("ShipModelSlot");slot.transform.SetParent(ship.transform,false);
            var visual=new GameObject("PlaceholderBattleship");visual.transform.SetParent(slot.transform,false);visual.AddComponent<ShipIdleMotion>();
            var v=visual.transform;
            Primitive("Hull core",v,PrimitiveType.Cube,new Vector3(0,.7f,-1),new Vector3(6,2.1f,36),hull);
            Primitive("Aft taper",v,PrimitiveType.Cube,new Vector3(0,.7f,-20),new Vector3(4.7f,2,3),hull);
            for(int i=0;i<4;i++)
            {
                float z=17.5f+i*1.6f,w=5.5f-i*1.35f;
                Primitive("Bow taper "+i,v,PrimitiveType.Cube,new Vector3(0,.8f,z),new Vector3(w,2,1.8f),hull);
                Primitive("Foredeck "+i,v,PrimitiveType.Cube,new Vector3(0,1.85f,z),new Vector3(w,.2f,1.8f),deck);
            }
            Primitive("Main deck",v,PrimitiveType.Cube,new Vector3(0,1.8f,-1.5f),new Vector3(5.8f,.25f,37),deck);
            Primitive("Citadel",v,PrimitiveType.Cube,new Vector3(0,2.55f,-1),new Vector3(4.2f,1.5f,18),hull);
            Primitive("Bridge lower",v,PrimitiveType.Cube,new Vector3(0,4.1f,2),new Vector3(3.6f,2,5),hull);
            Primitive("Bridge upper",v,PrimitiveType.Cube,new Vector3(0,5.65f,2.8f),new Vector3(2.8f,1.1f,2.5f),trim);
            Primitive("Bridge windows",v,PrimitiveType.Cube,new Vector3(0,5.65f,4.07f),new Vector3(2.4f,.35f,.05f),dark);
            Primitive("Bridge roof",v,PrimitiveType.Cube,new Vector3(0,6.3f,2.8f),new Vector3(3.2f,.22f,2.9f),hull);
            foreach(float z in new[]{-3f,-8f})
            {
                Primitive("Funnel",v,PrimitiveType.Cylinder,new Vector3(0,4.6f,z),new Vector3(1.7f,1.5f,2.3f),hull);
                Primitive("Funnel cap",v,PrimitiveType.Cylinder,new Vector3(0,6.1f,z),new Vector3(1.85f,.23f,2.45f),dark);
            }
            Primitive("Mast",v,PrimitiveType.Cylinder,new Vector3(0,7.3f,.5f),new Vector3(.18f,2.4f,.18f),dark);
            Primitive("Yard",v,PrimitiveType.Cube,new Vector3(0,8.5f,.5f),new Vector3(4,.12f,.14f),dark);
            Turret(v,12,0);Turret(v,7,0);Turret(v,-15,180);
            for(int side=-1;side<=1;side+=2)
            {
                Primitive("Deck rim",v,PrimitiveType.Cube,new Vector3(side*2.85f,2.05f,-1),new Vector3(.12f,.28f,34),trim);
                for(int z=-17;z<=15;z+=4)Primitive("Bollard",v,PrimitiveType.Cylinder,new Vector3(side*2.5f,2.05f,z),new Vector3(.3f,.25f,.3f),dark);
            }
            PrefabUtility.SaveAsPrefabAsset(ship,Root+"Prefabs/MenuBattleship.prefab");
            Object.DestroyImmediate(ship);
            ship=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/MenuBattleship.prefab"));
            var pivot=new GameObject("OrbitPivot").transform;pivot.position=new Vector3(0,2,0);
            var look=new GameObject("ShipLookTarget").transform;look.position=new Vector3(0,2,0);
            var menu=Pose("MenuPose",new Vector3(48,10,30),look,32);
            var prep=Pose("PreparationPose",new Vector3(0,65,43),look,44);
            var camera=new GameObject("Menu Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            camera.tag="MainCamera";camera.nearClipPlane=.3f;camera.farClipPlane=1500;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.fogColor;
            var rig=new GameObject("MenuCameraRig").AddComponent<MenuCameraRig>();
            rig.view=camera;rig.pivot=pivot;rig.menuPose=menu;rig.preparationPose=prep;rig.Initialize();
            var flow=new GameObject("MenuFlow").AddComponent<MenuFlowController>();flow.cameraRig=rig;
            flow.preparation=flow.gameObject.AddComponent<ShipPreparationController>();flow.preparation.shipModelSlot=ship.transform.Find("ShipModelSlot");
            var canvas=new GameObject("Menu Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var main=Panel("Main Menu UI",canvas.transform);var preparation=Panel("Preparation UI",canvas.transform);
            flow.menuPanel=main;flow.preparationPanel=preparation;
            Label("Title",main.transform,"GRAND NAVIGATION",42,new Vector2(0,425),new Vector2(1100,70));
            flow.startButton=Button("Start Game",main.transform,new Vector2(0,-135));
            flow.exitButton=Button("Exit",main.transform,new Vector2(0,-220));
            Label("Preparation Title",preparation.transform,"Ship Preparation",32,new Vector2(0,455),new Vector2(700,60));
            flow.goButton=Button("GO",preparation.transform,new Vector2(0,-445));
            flow.backButton=Button("Back",preparation.transform,new Vector2(-740,445),new Vector2(170,55));
            flow.message=Label("Status",canvas.transform,"",22,new Vector2(0,-350),new Vector2(1000,50));
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            preparation.SetActive(false);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
        }
        static MenuCameraPose Pose(string name,Vector3 position,Transform target,float fov)
        {
            var pose=new GameObject(name).AddComponent<MenuCameraPose>();pose.transform.position=position;pose.lookTarget=target;pose.fieldOfView=fov;pose.transform.rotation=pose.LookRotation;return pose;
        }
        static Material Mat(string name,Color color,float smooth)
        {
            string path=Root+"Materials/"+name+".mat";
            if(System.IO.File.Exists(path))throw new System.InvalidOperationException("Material already exists: "+path);
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,path);return m;
        }
        static GameObject Primitive(string name,Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static void Turret(Transform parent,float z,float yaw)
        {
            var root=new GameObject("Main Turret").transform;root.SetParent(parent,false);root.localPosition=new Vector3(0,z==7?3:2,z);root.localRotation=Quaternion.Euler(0,yaw,0);
            Primitive("Barbette",root,PrimitiveType.Cylinder,new Vector3(0,.15f,0),new Vector3(2.8f,.3f,2.8f),dark);
            Primitive("Turret housing",root,PrimitiveType.Cube,new Vector3(0,.8f,0),new Vector3(3.1f,1.1f,2.8f),hull);
            foreach(float x in new[]{-.65f,.65f})Primitive("Barrel",root,PrimitiveType.Cube,new Vector3(x,.9f,3),new Vector3(.25f,.25f,4.2f),dark);
        }
        static GameObject Panel(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return go;
        }
        static Text Label(string name,Transform parent,string value,int size,Vector2 position,Vector2 bounds)
        {
            var t=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<Text>();t.transform.SetParent(parent,false);t.rectTransform.anchoredPosition=position;t.rectTransform.sizeDelta=bounds;
            t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(.85f,.93f,1);t.raycastTarget=false;return t;
        }
        static Button Button(string name,Transform parent,Vector2 position,Vector2? bounds=null)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=position;r.sizeDelta=bounds??new Vector2(300,65);
            go.GetComponent<Image>().color=new Color(.035f,.12f,.19f,.9f);var b=go.GetComponent<Button>();
            var c=b.colors;c.highlightedColor=new Color(.5f,.8f,1);c.pressedColor=new Color(.3f,.6f,.8f);b.colors=c;
            Label("Label",go.transform,name,26,Vector2.zero,r.sizeDelta);return b;
        }
    }
}


