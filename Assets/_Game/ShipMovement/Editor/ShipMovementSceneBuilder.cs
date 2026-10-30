using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace GrandNavigation.Gameplay.Editor
{
    public static class ShipMovementSceneBuilder
    {
        const string Root="Assets/_Game/ShipMovement/";
        const string ScenePath="Assets/_Game/Scenes/ShipMovementPrototype.unity";
        [MenuItem("Grand Navigation/Create Ship Movement Prototype (new only)")]
        public static void Create()
        {
            if(System.IO.File.Exists(ScenePath))throw new System.InvalidOperationException("Scene exists; refusing to overwrite.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Unsaved scene changes.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var sea=Material("Sea",new Color(.035f,.17f,.25f),true);
            var hull=Material("Hull",new Color(.5f,.59f,.64f));
            var deck=Material("Deck",new Color(.26f,.34f,.39f));
            var rock=Material("Island",new Color(.35f,.32f,.25f));
            var border=Material("Boundary",new Color(.08f,.28f,.35f));
            var physics=new PhysicsMaterial("Ship frictionless contact"){dynamicFriction=0,staticFriction=0,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum,bounceCombine=PhysicsMaterialCombine.Minimum};
            AssetDatabase.CreateAsset(physics,Root+"Materials/ShipContact.physicMaterial");
            Part("Sea",null,PrimitiveType.Plane,Vector3.zero,new Vector3(40,1,40),sea,false);
            var island=Part("IslandObstacle",null,PrimitiveType.Cube,new Vector3(0,3,50),new Vector3(40,8,22),rock,true);
            island.GetComponent<Collider>().sharedMaterial=physics;
            Part("Island peak",island.transform,PrimitiveType.Sphere,new Vector3(0,.6f,0),new Vector3(.65f,1.2f,.75f),rock,false);
            for(int i=0;i<4;i++)
            {
                bool x=i<2;float sign=i%2==0?-1:1;
                var wall=Part("MapBoundary"+i,null,PrimitiveType.Cube,x?new Vector3(sign*200,3,0):new Vector3(0,3,sign*200),
                    x?new Vector3(2,8,402):new Vector3(402,8,2),border,true);
                wall.GetComponent<Collider>().sharedMaterial=physics;
            }
            var ship=new GameObject("PlayerShip");
            ship.transform.position=new Vector3(0,1,-70);
            var collider=ship.AddComponent<BoxCollider>();collider.size=new Vector3(5.5f,2.5f,28);collider.center=new Vector3(0,.1f,0);collider.sharedMaterial=physics;
            var body=ship.AddComponent<Rigidbody>();body.mass=1000;
            ship.AddComponent<PlayerShip>();ship.AddComponent<PlayerShipInput>();
            var visual=new GameObject("VisualModel").transform;visual.SetParent(ship.transform,false);
            Part("Hull",visual,PrimitiveType.Cube,Vector3.zero,new Vector3(5.4f,2,25),hull,false);
            Part("Bow",visual,PrimitiveType.Cube,new Vector3(0,0,12.8f),new Vector3(3.3f,2,2.3f),hull,false);
            Part("Deck",visual,PrimitiveType.Cube,new Vector3(0,1.05f,0),new Vector3(5.2f,.2f,24.5f),deck,false);
            Part("Bridge",visual,PrimitiveType.Cube,new Vector3(0,2.4f,0),new Vector3(3.2f,2.5f,4),hull,false);
            Part("Bridge windows",visual,PrimitiveType.Cube,new Vector3(0,2.9f,2.02f),new Vector3(2.6f,.5f,.1f),deck,false);
            Part("Funnel",visual,PrimitiveType.Cylinder,new Vector3(0,3,-3.5f),new Vector3(1.4f,1.7f,1.4f),deck,false);
            foreach(float z in new[]{8f,4f,-9f})
            {
                var turret=new GameObject("Twin turret").transform;turret.SetParent(visual,false);turret.localPosition=new Vector3(0,1.4f,z);turret.localRotation=Quaternion.Euler(0,z<0?180:0,0);
                Part("Housing",turret,PrimitiveType.Cube,new Vector3(0,.4f,0),new Vector3(2.6f,.9f,2.4f),hull,false);
                foreach(float x in new[]{-.55f,.55f})Part("Barrel",turret,PrimitiveType.Cube,new Vector3(x,.6f,2),new Vector3(.2f,.2f,2.8f),deck,false);
            }
            // No independent Ship component: PlayerShip inherits Ship and owns the component references.
            PrefabUtility.SaveAsPrefabAsset(ship,Root+"Prefabs/PlayerShip.prefab");Object.DestroyImmediate(ship);
            ship=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/PlayerShip.prefab"));
            var camera=new GameObject("Follow Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            camera.tag="MainCamera";camera.fieldOfView=60;camera.farClipPlane=1200;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.11f,.22f,.32f);
            var follow=camera.gameObject.AddComponent<ShipFollowCamera>();follow.target=ship.transform;
            camera.transform.position=ship.transform.TransformPoint(follow.offset);camera.transform.LookAt(ship.transform.position+Vector3.forward*10+Vector3.up*2);
            var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.5f;sun.transform.rotation=Quaternion.Euler(45,-35,0);sun.shadows=LightShadows.Soft;
            RenderSettings.skybox=null;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.5f,.58f);
            var canvas=new GameObject("Movement HUD",typeof(Canvas),typeof(CanvasScaler)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var panel=new GameObject("Panel",typeof(RectTransform),typeof(Image));panel.transform.SetParent(canvas.transform,false);
            var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(16,-16);rect.sizeDelta=new Vector2(285,250);panel.GetComponent<Image>().color=new Color(0,.03f,.06f,.85f);
            var text=new GameObject("Readout",typeof(RectTransform),typeof(Text)).GetComponent<Text>();text.transform.SetParent(panel.transform,false);text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=new Vector2(14,10);text.rectTransform.offsetMax=new Vector2(-12,-12);
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=20;text.color=Color.white;
            var hud=canvas.gameObject.AddComponent<ShipMovementHUD>();hud.movement=ship.GetComponent<ShipMovement>();hud.readout=text;
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
        }
        static Material Material(string name,Color color,bool unlit=false)
        {
            var material=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.15f);AssetDatabase.CreateAsset(material,Root+"Materials/"+name+".mat");return material;
        }
        static GameObject Part(string name,Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material material,bool collision)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
    }
}
