using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace GrandNavigation.Gameplay.Levels.Editor
{
    public static class LevelGreyboxBuilder
    {
        public const string Path="Assets/_Game/Scenes/LevelGreybox.unity";
        const string Root="Assets/_Game/LevelGreybox";
        [MenuItem("Grand Navigation/Create Level Greybox (new only)")]
        public static void Create()
        {
            if(System.IO.File.Exists(Path))throw new System.InvalidOperationException("Level already exists; refusing overwrite.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Unsaved scene: save first.");
            System.IO.Directory.CreateDirectory(Root+"/Materials");System.IO.Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/PlayerCombatPrototype.unity");
            EditorSceneManager.SaveScene(scene,Path);
            var root=new GameObject("LevelGreybox").transform;
            Transform Group(string n){var t=new GameObject(n).transform;t.SetParent(root,false);return t;}
            var environment=Group("Environment");var islands=Group("Islands");var spawns=Group("SpawnPoints");
            var players=Group("Player");var targets=Group("TestTargets");var cameras=Group("Cameras");var ui=Group("UI");var debug=Group("Debug");
            var oldPlayer=Object.FindFirstObjectByType<PlayerShip>();
            foreach(var t in Object.FindObjectsByType<EnemyShip>(FindObjectsSortMode.None))Object.DestroyImmediate(t.gameObject);
            foreach(var name in new[]{"Sea","Island","Boundary 0","Boundary 1","Boundary 2","Boundary 3"})
            {var go=GameObject.Find(name);if(go)Object.DestroyImmediate(go);}
            var sea=new Material(Shader.Find("Universal Render Pipeline/Unlit"));sea.SetColor("_BaseColor",new Color(.025f,.12f,.19f));
            AssetDatabase.CreateAsset(sea,Root+"/Materials/Ocean.mat");
            var land=new Material(Shader.Find("Universal Render Pipeline/Lit"));land.SetColor("_BaseColor",new Color(.36f,.43f,.32f));
            AssetDatabase.CreateAsset(land,Root+"/Materials/Island.mat");
            var contact=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Game/ShipMovement/Materials/ShipContact.physicMaterial");
            GameObject Part(string name,Transform parent,PrimitiveType shape,Vector3 position,Vector3 size,Material mat,bool collide)
            {
                var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
                go.GetComponent<Renderer>().sharedMaterial=mat;
                if(!collide)Object.DestroyImmediate(go.GetComponent<Collider>());else go.GetComponent<Collider>().sharedMaterial=contact;
                return go;
            }
            var ocean=Part("Ocean",environment,PrimitiveType.Plane,Vector3.zero,new Vector3(256,1,256),sea,false);
            var boundaries=new GameObject("MapBoundaries").transform;boundaries.SetParent(environment,false);
            for(int i=0;i<4;i++)
            {
                bool x=i<2;float sign=i%2==0?-1:1;
                var wall=Part("Boundary_"+i,boundaries,PrimitiveType.Cube,x?new Vector3(sign*1285,30,0):new Vector3(0,30,sign*1285),
                    x?new Vector3(10,80,2580):new Vector3(2580,80,10),land,true);
                wall.GetComponent<Renderer>().enabled=false;
            }
            Vector3[] ip={new Vector3(-260,0,40),new Vector3(320,0,70),new Vector3(70,0,310)};
            Vector3[] sizes={new Vector3(220,35,140),new Vector3(200,30,160),new Vector3(170,25,120)};
            for(int i=0;i<3;i++)
            {
                var island=new GameObject("Island_"+(char)('A'+i));island.transform.SetParent(islands,false);island.transform.position=ip[i];
                var solid=Part("Solid",island.transform,PrimitiveType.Cube,Vector3.zero,Vector3.one,land,true);
                var settings=island.AddComponent<LevelIsland>();settings.solid=solid.transform;settings.size=sizes[i];settings.Apply();
            }
            Transform Spawn(string name,Vector3 pos){var t=new GameObject(name).transform;t.SetParent(spawns,false);t.position=pos;return t;}
            var playerSpawn=Spawn("PlayerSpawnPoint",new Vector3(0,0,-360));
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/PlayerCombat/Prefabs/PlayerCombatShip.prefab");
            var playerGo=(GameObject)PrefabUtility.InstantiatePrefab(prefab);playerGo.name="PlayerShip";playerGo.transform.SetParent(players,false);
            playerGo.transform.SetPositionAndRotation(playerSpawn.position,Quaternion.identity);
            var player=playerGo.GetComponent<PlayerShip>();var movement=playerGo.GetComponent<ShipMovement>();var weapon=playerGo.GetComponent<Weapon>();
            var settingsM=movement.settings;settingsM.maxForwardSpeed=10;settingsM.maxReverseSpeed=3;settingsM.forwardAcceleration=1.67f;
            settingsM.brakingDeceleration=2;settingsM.reverseAcceleration=.8f;settingsM.maxTurnRate=5;settingsM.fullTurnSpeed=10;
            var visual=playerGo.transform.Find("VisualModel");visual.localPosition+=Vector3.up;
            var collider=playerGo.GetComponent<BoxCollider>();collider.center+=Vector3.up;collider.size=new Vector3(collider.size.x,collider.size.y,32);
            var hull=visual.Find("Hull");hull.localScale=new Vector3(hull.localScale.x,hull.localScale.y,30);hull.localPosition=new Vector3(0,0,-1);
            var bow=visual.Find("Bow");bow.localScale=new Vector3(bow.localScale.x,bow.localScale.y,2);bow.localPosition=new Vector3(0,0,15);
            var deck=visual.Find("Deck");deck.localScale=new Vector3(deck.localScale.x,deck.localScale.y,29);deck.localPosition=new Vector3(0,deck.localPosition.y,-1);
            weapon.muzzleSpeed=140;weapon.gravity=9.81f;weapon.maximumRange=1200;weapon.horizontalRange=true;
            var shell=(GameObject)PrefabUtility.InstantiatePrefab(weapon.projectile.gameObject);
            var projectile=shell.GetComponent<Projectile>();projectile.lifetime=20;projectile.worldBounds=1400;
            shell.GetComponent<TrailRenderer>().time=1.2f;
            PrefabUtility.SaveAsPrefabAsset(shell,Root+"/Prefabs/LevelShell.prefab");Object.DestroyImmediate(shell);
            weapon.projectile=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/LevelShell.prefab").GetComponent<Projectile>();
            PrefabUtility.RecordPrefabInstancePropertyModifications(weapon);
            var fire=Object.FindFirstObjectByType<CombatFireControl>();fire.player=player;fire.reference=weapon.turrets[0].muzzles[0];fire.observationInterval=.5f;
            var aim=Object.FindFirstObjectByType<CombatAimController>();aim.player=player;aim.initialRange=400;
            var input=Object.FindFirstObjectByType<PlayerCombatInput>();input.player=player;
            var hud=Object.FindFirstObjectByType<CombatHUD>();hud.player=player;
            var orbit=Object.FindFirstObjectByType<ShipFollowCamera>();orbit.target=player.transform;
            var pivot=new GameObject("CameraPivot").transform;pivot.SetParent(player.transform,false);pivot.localPosition=Vector3.up*3;orbit.SetPivot(pivot);
            var cam=orbit.GetComponent<Camera>();cam.farClipPlane=5000;cam.transform.position=player.transform.position+new Vector3(0,20,-32);
            cam.transform.LookAt(player.transform.position+Vector3.up*3);cam.transform.SetParent(cameras,true);
            var rig=cam.GetComponent<CombatCamera>();rig.aimingDistance=90;rig.aimingHeight=45;rig.aimingFov=36;
            fire.transform.SetParent(ui,true);
            var region=Object.FindFirstObjectByType<PredictionRegion>();region.longitudinalRadius=24;region.lateralRadius=12;region.borderWidth=.35f;region.transform.SetParent(ui,true);
            var sun=Object.FindFirstObjectByType<Light>();sun.transform.SetParent(environment,true);
            Object.DestroyImmediate(oldPlayer.gameObject);
            var level=debug.gameObject.AddComponent<LevelDebug>();level.player=player;level.playerSpawn=playerSpawn;level.fire=fire;level.aim=aim;level.cameraRig=rig;
            level.targetPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/PlayerCombat/Prefabs/TargetDestroyer.prefab");
            level.targetsRoot=targets;level.targets=new Ship[3];level.targetSpawns=new Transform[3];
            Vector3[] tp={new Vector3(-360,0,220),new Vector3(380,0,180),new Vector3(0,0,420)};
            for(int i=0;i<3;i++)
            {
                level.targetSpawns[i]=Spawn("TargetSpawn_0"+(i+1),tp[i]);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(level.targetPrefab);go.name="Target_0"+(i+1);go.transform.SetParent(targets,false);go.transform.position=tp[i]+Vector3.up;
                var ship=go.GetComponent<Ship>();go.GetComponent<HealthSystem>().maximumHealth=1000;
                go.GetComponent<TestTargetMotion>().enabled=false;go.GetComponent<ShipMovement>().enabled=false;go.GetComponent<Weapon>().enabled=false;
                go.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;level.targets[i]=ship;
            }
            var b=ocean.GetComponent<Renderer>().bounds;
            if(Vector3.Distance(b.size,new Vector3(2560,0,2560))>.01f)throw new System.Exception("Ocean bounds incorrect");
            EditorSceneManager.SaveScene(scene,Path);AssetDatabase.SaveAssets();
        }
    }
}