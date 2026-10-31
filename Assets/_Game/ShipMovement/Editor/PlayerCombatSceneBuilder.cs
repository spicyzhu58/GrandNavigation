using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace GrandNavigation.Gameplay.Editor
{
    public static class PlayerCombatSceneBuilder
    {
        const string Root="Assets/_Game/PlayerCombat";
        public const string ScenePath="Assets/_Game/Scenes/PlayerCombatPrototype.unity";
        [MenuItem("Grand Navigation/Create Player Combat Prototype (new only)")]
        public static void Create()
        {
            if(System.IO.File.Exists(ScenePath))throw new System.InvalidOperationException("Combat scene already exists; will not overwrite.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Save current scene before building.");
            foreach(var sub in new[]{"Materials","Prefabs"})System.IO.Directory.CreateDirectory(Root+"/"+sub);
            AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var grey=Mat("Combat hull",new Color(.5f,.59f,.64f));var dark=Mat("Deck",new Color(.22f,.29f,.34f));
            var orange=Mat("Target",new Color(.8f,.19f,.05f));var seaMat=Mat("Sea",new Color(.025f,.12f,.19f),true);
            var flash=Mat("Flash",new Color(1,.7f,.12f),true);var splash=Mat("Splash",new Color(.65f,.9f,1),true);
            var fill=Mat("Prediction fill",new Color(1,.8f,0,.12f),true,true);var edge=Mat("Prediction edge",new Color(1,.8f,0,.6f),true,true);
            Part("Sea",null,PrimitiveType.Plane,Vector3.zero,new Vector3(100,1,100),seaMat);
            var contact=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Game/ShipMovement/Materials/ShipContact.physicMaterial");
            for(int i=0;i<4;i++)
            {
                bool x=i<2;float sign=i%2==0?-1:1;
                var wall=Part("Boundary "+i,null,PrimitiveType.Cube,x?new Vector3(sign*450,3,0):new Vector3(0,3,sign*450),x?new Vector3(3,10,903):new Vector3(903,10,3),dark);
                wall.AddComponent<BoxCollider>().sharedMaterial=contact;
            }
            var obstacle=Part("Island",null,PrimitiveType.Cube,new Vector3(-110,4,30),new Vector3(35,10,40),dark);obstacle.AddComponent<BoxCollider>().sharedMaterial=contact;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/ShipMovement/Prefabs/PlayerShip.prefab");
            var playerGo=Object.Instantiate(source);playerGo.name="Player Battleship";playerGo.transform.SetPositionAndRotation(new Vector3(0,1,-70),Quaternion.identity);
            var player=playerGo.GetComponent<PlayerShip>();player.team=0;player.Health.maximumHealth=600;
            var visual=playerGo.transform.Find("VisualModel");
            foreach(var child in visual.Cast<Transform>().Where(t=>t.name=="Twin turret").ToArray())Object.DestroyImmediate(child.gameObject);
            var sinking=playerGo.AddComponent<ShipSinking>();sinking.visual=visual;
            var weapon=playerGo.GetComponent<Weapon>();weapon.turrets=new CombatTurret[3];
            for(int i=0;i<3;i++)
            {
                var pivot=new GameObject("Main turret "+(i+1)).transform;pivot.SetParent(visual,false);pivot.localPosition=new Vector3(0,1.4f,i==0?8:i==1?4:-9);
                var turret=pivot.gameObject.AddComponent<CombatTurret>();turret.neutralYaw=i==2?180:0;turret.barrels=new Transform[2];turret.muzzles=new Transform[2];pivot.localRotation=Quaternion.Euler(0,turret.neutralYaw,0);
                Part("Housing",pivot,PrimitiveType.Cube,new Vector3(0,.4f,0),new Vector3(2.6f,.9f,2.4f),grey);
                for(int j=0;j<2;j++)
                {
                    var barrel=new GameObject("Elevation "+j).transform;barrel.SetParent(pivot,false);barrel.localPosition=new Vector3(j==0?-.55f:.55f,.6f,0);turret.barrels[j]=barrel;
                    Part("Barrel",barrel,PrimitiveType.Cube,new Vector3(0,0,1.8f),new Vector3(.2f,.2f,3.6f),dark);
                    var muzzle=new GameObject("Muzzle "+j).transform;muzzle.SetParent(barrel,false);muzzle.localPosition=Vector3.forward*3.6f;turret.muzzles[j]=muzzle;
                }
                weapon.turrets[i]=turret;
            }
            var shell=Part("Main shell",null,PrimitiveType.Sphere,Vector3.zero,Vector3.one*.35f,flash);var projectile=shell.AddComponent<Projectile>();
            var trail=shell.AddComponent<TrailRenderer>();trail.sharedMaterial=flash;trail.time=.7f;trail.startWidth=.18f;trail.endWidth=.015f;trail.minVertexDistance=.1f;
            PrefabUtility.SaveAsPrefabAsset(shell,Root+"/Prefabs/MainShell.prefab");Object.DestroyImmediate(shell);
            weapon.projectile=AssetDatabase.LoadAssetAtPath<Projectile>(Root+"/Prefabs/MainShell.prefab");
            Set(playerGo.GetComponent<PlayerShipInput>(),"enableSpaceWeaponShortcut",false);
            PrefabUtility.SaveAsPrefabAsset(playerGo,Root+"/Prefabs/PlayerCombatShip.prefab");
            var enemy=new GameObject("Target Destroyer");enemy.transform.position=new Vector3(20,1,100);enemy.transform.rotation=Quaternion.Euler(0,90,0);
            var collider=enemy.AddComponent<BoxCollider>();collider.size=new Vector3(5,3,18);collider.sharedMaterial=contact;
            enemy.AddComponent<Rigidbody>().mass=500;var target=enemy.AddComponent<Destroyer>();target.team=1;enemy.GetComponent<HealthSystem>().maximumHealth=150;
            var ev=new GameObject("VisualModel").transform;ev.SetParent(enemy.transform,false);
            Part("Hull",ev,PrimitiveType.Cube,Vector3.zero,new Vector3(5,2,18),orange);
            Part("Bridge",ev,PrimitiveType.Cube,new Vector3(0,2.2f,0),new Vector3(2.5f,3,4),orange);
            Part("Bow",ev,PrimitiveType.Cube,new Vector3(0,0,9),new Vector3(3,2,2),orange);
            enemy.AddComponent<TestTargetMotion>();enemy.AddComponent<ShipSinking>().visual=ev;
            PrefabUtility.SaveAsPrefabAsset(enemy,Root+"/Prefabs/TargetDestroyer.prefab");
            CreateBattleshipTarget();
            var cam=new GameObject("Combat Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();cam.tag="MainCamera";cam.fieldOfView=60;cam.farClipPlane=1600;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.06f,.14f,.22f);
            cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            var orbit=cam.gameObject.AddComponent<ShipFollowCamera>();orbit.target=player.transform;Set(orbit,"allowCursorRelease",false);
            var cameraPivot=new GameObject("CameraPivot").transform;cameraPivot.SetParent(playerGo.transform,false);cameraPivot.localPosition=Vector3.up*3;orbit.SetPivot(cameraPivot);
            cam.transform.position=player.WorldPosition+new Vector3(0,20,-32);cam.transform.LookAt(playerGo.transform.position+Vector3.up*3);
            var systems=new GameObject("Combat Systems");var fire=systems.AddComponent<CombatFireControl>();fire.player=player;fire.reference=weapon.turrets[0].muzzles[0];
            var aim=systems.AddComponent<CombatAimController>();aim.player=player;aim.fireControl=fire;
            var rig=cam.gameObject.AddComponent<CombatCamera>();rig.view=cam;rig.orbit=orbit;rig.aim=aim;rig.fireControl=fire;
            var input=systems.AddComponent<PlayerCombatInput>();input.player=player;input.view=cam;input.fireControl=fire;input.aim=aim;input.cameraRig=rig;
            var effects=systems.AddComponent<CombatEffects>();effects.flashMaterial=flash;effects.splashMaterial=splash;
            var region=new GameObject("Yellow prediction region").AddComponent<PredictionRegion>();region.fireControl=fire;region.fillMaterial=fill;region.edgeMaterial=edge;
            var hud=systems.AddComponent<CombatHUD>();hud.player=player;hud.fireControl=fire;hud.aim=aim;hud.view=cam;
            var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.5f;sun.transform.rotation=Quaternion.Euler(45,-35,0);sun.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
            RenderSettings.skybox=null;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.5f,.58f);
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        }
        public static void CreateBattleshipTarget()
        {
            string path=Root+"/Prefabs/TargetBattleship.prefab";
            if(System.IO.File.Exists(path))return;
            var root=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/TargetDestroyer.prefab");
            try
            {
                Object.DestroyImmediate(root.GetComponent<Destroyer>());root.name="Target Battleship";
                var ship=root.AddComponent<Battleship>();ship.team=1;
                root.GetComponent<HealthSystem>().maximumHealth=450;
                root.transform.Find("VisualModel").localScale=new Vector3(1.3f,1,1.6f);
                root.GetComponent<BoxCollider>().size=new Vector3(6.5f,3,28.8f);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void Set(Object obj,string name,bool value){var so=new SerializedObject(obj);so.FindProperty(name).boolValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static Material Mat(string name,Color color,bool unlit=false,bool transparent=false)
        {
            var m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);
            if(transparent){m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",10);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;}
            AssetDatabase.CreateAsset(m,Root+"/Materials/"+name+".mat");return m;
        }
        static GameObject Part(string name,Transform parent,PrimitiveType shape,Vector3 p,Vector3 scale,Material m)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=m;Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
    }
}
