using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GrandNavigation.Gameplay.Levels;
namespace GrandNavigation.Gameplay.AI.Editor
{
    public static class EnemyAISceneBuilder
    {
        public const string ScenePath="Assets/_Game/Scenes/EnemyAIGreybox.unity";
        const string Root="Assets/_Game/EnemyAI";
        [MenuItem("Grand Navigation/Create Enemy AI Greybox (new only)")]
        public static void Create()
        {
            if(System.IO.File.Exists(ScenePath))throw new System.InvalidOperationException("AI scene already exists");
            if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Save scene and exit Play first");
            foreach(string d in new[]{"Profiles","Prefabs"})System.IO.Directory.CreateDirectory(Root+"/"+d);
            AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/LevelGreybox.unity");
            EditorSceneManager.SaveScene(scene,ScenePath);
            var old=Object.FindFirstObjectByType<LevelDebug>();
            var targets=Object.FindObjectsByType<EnemyShip>(FindObjectsSortMode.InstanceID).OrderBy(s=>s.name).ToArray();
            Vector3[] positions=targets.Select(s=>s.transform.position).ToArray();
            Quaternion[] rotations=targets.Select(s=>s.transform.rotation).ToArray();
            var parent=targets[0].transform.parent;
            foreach(var s in targets)Object.DestroyImmediate(s.gameObject);
            if(old)Object.DestroyImmediate(old);
            var debug=new GameObject("Enemy AI Debug").AddComponent<EnemyAIDebug>();debug.enemiesRoot=parent;debug.slots=new EnemyAIDebug.SpawnSlot[3];
            var large=MakeProfile(false);var small=MakeProfile(true);
            var largePrefab=MakePrefab(false,large);var smallPrefab=MakePrefab(true,small);
            Physics.SyncTransforms();
            for(int i=0;i<3;i++)
            {
                bool big=i==0;var prefab=big?largePrefab:smallPrefab;
                Vector3 position=positions[i];
                // Keep saved target positions unless the physical island volume overlaps this hull.
                bool Free(Vector3 v)=>!Physics.OverlapBox(v+Vector3.up*2,new Vector3(18,3,23),Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.GetComponentInParent<Ship>());
                if(!Free(position))
                {
                    bool found=false;
                    for(int radius=40;radius<=400&&!found;radius+=40)
                        for(int a=0;a<8;a++){Vector3 q=position+Quaternion.Euler(0,a*45,0)*Vector3.forward*radius;if(Mathf.Abs(q.x)<1100&&Mathf.Abs(q.z)<1100&&Free(q)){position=q;found=true;break;}}
                    if(!found)throw new System.Exception("No safe nearby spawn");
                }
                var routeRoot=new GameObject("NPC_"+(i+1)+"_PatrolRoute").transform;routeRoot.SetParent(debug.transform,false);
                var route=new Transform[4];
                Vector3[] offsets={new Vector3(0,0,240),new Vector3(300,0,240),new Vector3(300,0,-240),new Vector3(0,0,-240)};
                for(int j=0;j<4;j++){var t=new GameObject("Waypoint_"+j).transform;t.SetParent(routeRoot,false);var q=position+offsets[j];q.x=Mathf.Clamp(q.x,-1000,1000);q.z=Mathf.Clamp(q.z,-1000,1000);q.y=0;t.position=q;route[j]=t;}
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.SetParent(parent,true);go.transform.SetPositionAndRotation(position,rotations[i]);go.name="NPC_"+(i+1)+"_"+(big?"Battleship":"Destroyer");
                var ai=go.GetComponent<EnemyShipAI>();ai.waypoints=route;PrefabUtility.RecordPrefabInstancePropertyModifications(ai);
                debug.slots[i]=new EnemyAIDebug.SpawnSlot{prefab=prefab,instance=ai,position=position,euler=rotations[i].eulerAngles,route=route};
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            // Scene reload debug needs a build-list entry, preserving all existing entries and order.
            if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
        }
        static EnemyAIProfile MakeProfile(bool small)
        {
            var p=ScriptableObject.CreateInstance<EnemyAIProfile>();
            if(small)
            {
                p.detectionRange=600;p.engageEnterRange=400;p.engageExitRange=500;p.preferredCombatRange=300;p.combatRangeTolerance=50;
                p.decisionInterval=.5f;p.minStateTime=2;p.targetForgetDelay=5;p.retreatHealthRatio=.3f;
                p.patrolSpeed=20;p.approachSpeed=25;p.combatSpeed=p.retreatSpeed=20;p.waypointRadius=60;p.allyArrivalRadius=120;
                p.preferredBroadsideAngle=70;p.broadsideSwitchDelay=12;p.reactionDelay=1;p.aimErrorRadius=15;p.aimTolerance=5;
                p.avoidLookAhead=8;p.obstacleClearance=35;p.stuckTimeout=5;p.minProgressDistance=20;
            }
            AssetDatabase.CreateAsset(p,Root+"/Profiles/"+(small?"Destroyer":"Battleship")+".asset");return p;
        }
        static GameObject MakePrefab(bool small,EnemyAIProfile profile)
        {
            string type=small?"Destroyer":"Battleship";
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/PlayerCombat/Prefabs/Target"+type+".prefab"));go.name="AI"+type;
            var ship=go.GetComponent<Ship>();ship.team=1;
            Object.DestroyImmediate(go.GetComponent<TestTargetMotion>());
            go.GetComponent<HealthSystem>().maximumHealth=small?150:450;
            var movement=go.GetComponent<ShipMovement>();movement.enabled=true;movement.settings.maxForwardSpeed=small?25:15;
            movement.settings.maxReverseSpeed=4;movement.settings.maxTurnRate=small?12:6;movement.settings.fullTurnSpeed=small?20:15;
            movement.settings.forwardAcceleration=small?4:2;movement.settings.brakingDeceleration=small?5:3;
            var w=go.GetComponent<Weapon>();w.enabled=true;w.muzzleSpeed=140;w.gravity=9.81f;w.maximumRange=1200;w.horizontalRange=true;w.friendlyFire=false;
            w.projectile=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/LevelGreybox/Prefabs/LevelShell.prefab").GetComponent<Projectile>();
            var donor=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/PlayerCombat/Prefabs/PlayerCombatShip.prefab").GetComponent<Weapon>();
            w.turrets=new CombatTurret[small?2:3];var visual=go.transform.Find("VisualModel");
            // Independent unit-scale turret root follows sinking visual without scaling muzzle transforms.
            var turretRoot=new GameObject("AI Main Battery").transform;turretRoot.SetParent(visual,false);turretRoot.localScale=new Vector3(1/visual.localScale.x,1/visual.localScale.y,1/visual.localScale.z);
            for(int i=0;i<w.turrets.Length;i++)
            {
                int source=i==w.turrets.Length-1?2:i;
                var t=Object.Instantiate(donor.turrets[source],turretRoot);t.name="AI Twin Turret "+(i+1);
                t.transform.localPosition=new Vector3(0,1.6f,i==w.turrets.Length-1?(small?-6:-10):i==0?(small?6:10):5);
                t.aimTolerance=profile.aimTolerance;w.turrets[i]=t;
            }
            go.AddComponent<EnemyNavigation>();go.AddComponent<EnemyShipAI>().profile=profile;
            var saved=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/AI"+type+".prefab");Object.DestroyImmediate(go);return saved;
        }
    }
}
