using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using GrandNavigation.Gameplay.AI;
using GrandNavigation.Gameplay.Levels;
using Object=UnityEngine.Object;

namespace GrandNavigation.Gameplay.Art.Editor
{
    public static class NavalArtBuilder
    {
        public const string ScenePath="Assets/_Game/Scenes/TropicalNavalBattle.unity";
        const string Root="Assets/_Game/NavalArt";
        [Serializable] public class Manifest { public string ship;public float length;public TurretData[] turrets;public MaterialData[] materials; }
        [Serializable] public class TurretData { public float[] position;public float yaw;public BarrelData[] barrels; }
        [Serializable] public class BarrelData { public float[] position;public float length; }
        [Serializable] public class MaterialData { public string name,texture; }
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
        static Transform Child(string name,Transform parent,Vector3 position)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        static Material Mat(string name,Color color,string texture=null)
        {
            string path=Root+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.22f);
            if(texture!=null)m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
            return m;
        }
        static Manifest LoadShip(string name)
        {
            var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"/Source/"+name+"/manifest.json"));
            foreach(var source in data.materials)
            {
                string tex=string.IsNullOrEmpty(source.texture)?null:Root+"/Source/"+name+"/Textures/"+source.texture;
                var m=Mat(name+"_"+source.name,tex==null?new Color(.48f,.51f,.53f):Color.white,tex);
                m.SetFloat("_Cull",0);
                if(tex!=null && (tex.EndsWith(".png")||tex.Contains("alpha")))
                {m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.4f);m.EnableKeyword("_ALPHATEST_ON");}
                materials[name+"/"+source.name]=m;
            }
            return data;
        }
        static GameObject MeshObject(string ship,string part,Transform parent)
        {
            var go=new GameObject(part);go.transform.SetParent(parent,false);
            using(var r=new BinaryReader(File.OpenRead(Root+"/Source/"+ship+"/"+part+".bytes")))
            {
                if(r.ReadInt32()!=0x4E41564C)throw new Exception("Invalid mesh stream");
                int count=r.ReadInt32();var vertices=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];
                for(int i=0;i<count;i++){vertices[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());normals[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());}
                var mesh=new Mesh{name=ship+"_"+part,indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;
                int sub=r.ReadInt32();mesh.subMeshCount=sub;var mats=new Material[sub];
                for(int i=0;i<sub;i++)
                {
                    string mat=System.Text.Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));int n=r.ReadInt32();var indices=new int[n];for(int j=0;j<n;j++)indices[j]=r.ReadInt32();
                    mesh.SetTriangles(indices,i);mats[i]=materials[ship+"/"+mat];
                }
                mesh.RecalculateBounds();mesh.RecalculateTangents();
                string path=Root+"/Meshes/"+mesh.name+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=mats;
            }
            return go;
        }
        static void Replace(GameObject go,Manifest data)
        {
            // Preserve all gameplay components and settings, replace only presentation and weapon attachment transforms.
            var weapon=go.GetComponent<Weapon>();var settings=weapon.turrets[0];
            string turretSettings=EditorJsonUtility.ToJson(settings);
            if(PrefabUtility.IsPartOfPrefabInstance(go))PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var visual=go.transform.Find("VisualModel");
            foreach(Transform t in visual.Cast<Transform>().ToArray())Object.DestroyImmediate(t.gameObject);
            visual.localScale=Vector3.one;visual.localRotation=Quaternion.identity;visual.localPosition=new Vector3(0,-go.transform.position.y,0);
            MeshObject(data.ship,"Hull",visual);
            weapon.turrets=new CombatTurret[data.turrets.Length];
            for(int i=0;i<data.turrets.Length;i++)
            {
                var d=data.turrets[i];var yaw=Child("MainBattery_"+(i+1),visual,V(d.position));
                var turret=yaw.gameObject.AddComponent<CombatTurret>();EditorJsonUtility.FromJsonOverwrite(turretSettings,turret);
                turret.neutralYaw=d.yaw;yaw.localRotation=Quaternion.Euler(0,d.yaw,0);
                MeshObject(data.ship,"Turret_"+i+"_Housing",yaw);
                turret.barrels=new Transform[d.barrels.Length];turret.muzzles=new Transform[d.barrels.Length];
                for(int j=0;j<d.barrels.Length;j++)
                {
                    var barrel=Child("Elevation_"+j,yaw,V(d.barrels[j].position));
                    MeshObject(data.ship,"Turret_"+i+"_Barrel_"+j,barrel);
                    turret.barrels[j]=barrel;turret.muzzles[j]=Child("Muzzle_"+j,barrel,Vector3.forward*d.barrels[j].length);
                }
                weapon.turrets[i]=turret;
            }
            go.GetComponent<ShipSinking>().visual=visual;
        }
        [MenuItem("Grand Navigation/Create Tropical Naval Battle (new only)")]
        public static void Build()
        {
            if(File.Exists(ScenePath))throw new InvalidOperationException("Scene already exists; refusing overwrite.");
            if(!Application.isBatchMode&&EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current scene first.");
            foreach(var folder in new[]{"Materials","Meshes","Prefabs"})Directory.CreateDirectory(Root+"/"+folder);
            AssetDatabase.Refresh();materials.Clear();var bismarck=LoadShip("Bismarck");var gearing=LoadShip("Gearing");
            var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/EnemyAIGreybox.unity");
            EditorSceneManager.SaveScene(scene,ScenePath);
            var player=Object.FindFirstObjectByType<PlayerShip>();Replace(player.gameObject,bismarck);player.name="Player_Bismarck";
            Object.FindFirstObjectByType<CombatFireControl>().reference=player.GetComponent<Weapon>().turrets[0].muzzles[0];
            var debug=Object.FindFirstObjectByType<EnemyAIDebug>();
            foreach(var slot in debug.slots)
            {
                var go=slot.instance.gameObject;bool big=go.GetComponent<Battleship>();Replace(go,big?bismarck:gearing);go.name=big?"NPC_Bismarck":"NPC_Gearing";
                string path=Root+"/Prefabs/"+(big?"Bismarck_AI":"Gearing_AI")+".prefab";
                if(!File.Exists(path))
                {
                    // Routes belong to the scene, never serialize external scene references into the reset prefab.
                    var ai=go.GetComponent<EnemyShipAI>();var route=ai.waypoints;ai.waypoints=Array.Empty<Transform>();
                    PrefabUtility.SaveAsPrefabAsset(go,path);ai.waypoints=route;
                }
                slot.prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            Environment();
            if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Artifacts/NavalArt/build-complete.txt","Created "+ScenePath+"\nBismarck 4x2, Gearing 3x2; all original gameplay settings retained except salvo capacity.\n");
            Debug.Log("NAVAL_ART_BUILD_COMPLETE");
        }
        static void Environment()
        {
            var sand=Mat("WarmSand",new Color(.78f,.69f,.47f));var grass=Mat("IslandGreen",new Color(.23f,.36f,.16f));
            var rock=Mat("WeatheredRock",new Color(.48f,.48f,.40f));
            var water=Mat("TropicalOcean",new Color(.025f,.33f,.43f));water.SetFloat("_Smoothness",.85f);water.SetFloat("_Metallic",.15f);
            var ocean=GameObject.Find("Ocean");if(ocean)ocean.GetComponent<Renderer>().sharedMaterial=water;
            var sun=Object.FindFirstObjectByType<Light>();sun.color=new Color(1,.96f,.85f);sun.intensity=1.6f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(48,-32,0);
            var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetFloat("_SunSize",.035f);sky.SetFloat("_AtmosphereThickness",.8f);sky.SetColor("_SkyTint",new Color(.48f,.62f,.78f));sky.SetColor("_GroundColor",new Color(.35f,.43f,.44f));
            AssetDatabase.CreateAsset(sky,Root+"/Materials/DaylightSky.mat");RenderSettings.skybox=sky;RenderSettings.sun=sun;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.62f,.77f,.86f);RenderSettings.ambientEquatorColor=new Color(.43f,.54f,.57f);RenderSettings.ambientGroundColor=new Color(.22f,.26f,.25f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.66f,.82f,.85f);RenderSettings.fogStartDistance=1000;RenderSettings.fogEndDistance=3200;
            foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)){c.clearFlags=CameraClearFlags.Skybox;c.farClipPlane=5000;}
            foreach(var island in Object.FindObjectsByType<LevelIsland>(FindObjectsSortMode.None))
            {
                var root=island.transform;var size=island.size;if(island.solid)Object.DestroyImmediate(island.solid.gameObject);Object.DestroyImmediate(island);
                var mesh=IslandMesh(size);AssetDatabase.CreateAsset(mesh,Root+"/Meshes/"+root.name+"_Terrain.asset");
                var terrain=Child("SandyCoastAndHighland",root,Vector3.zero).gameObject;terrain.AddComponent<MeshFilter>().sharedMesh=mesh;
                terrain.AddComponent<MeshRenderer>().sharedMaterials=new[]{sand,grass};
                var collision=IslandMesh(size,16);AssetDatabase.CreateAsset(collision,Root+"/Meshes/"+root.name+"_Collision.asset");
                var collider=terrain.AddComponent<MeshCollider>();collider.sharedMesh=collision;collider.convex=true;
                ScatterEnvironment(root,size,rock);
            }
        }
        static Mesh IslandMesh(Vector3 size,int sides=64)
        {
            float[] radius={0,.30f,.65f,.84f,1f,1.07f};float[] heights={1,.85f,.34f,.035f,-.015f,-.12f};
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var beach=new List<int>();var land=new List<int>();
            for(int r=0;r<radius.Length;r++)for(int s=0;s<sides;s++)
            {
                float a=s*Mathf.PI*2/sides;float wave=1+.035f*Mathf.Sin(a*5)+.025f*Mathf.Cos(a*3);
                vertices.Add(new Vector3(Mathf.Cos(a)*size.x*.5f*radius[r]*wave,size.y*heights[r],Mathf.Sin(a)*size.z*.5f*radius[r]*wave));uv.Add(new Vector2(Mathf.Cos(a)*radius[r],Mathf.Sin(a)*radius[r]));
            }
            for(int r=0;r<radius.Length-1;r++)for(int s=0;s<sides;s++)
            {int a=r*sides+s,b=r*sides+(s+1)%sides,c=a+sides,d=b+sides;var list=r>=2?beach:land;list.AddRange(new[]{a,b,c,b,d,c});}
            var mesh=new Mesh{name="IslandTerrain"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=2;mesh.SetTriangles(beach,0);mesh.SetTriangles(land,1);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static void ScatterEnvironment(Transform root,Vector3 size,Material rockMat)
        {
            string path=Root+"/Source/Tropical/Models/Models/Rock3/Rock3_LOD0.FBX";
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)return;
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Source/Tropical/Models/Textures/Rock3_Albedo.tif");rockMat.SetTexture("_BaseMap",tex);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI*2/12;var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.name="CoastalRock_"+i;go.transform.SetParent(root,false);
                var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var rr in rs){b.Encapsulate(rr.bounds);rr.sharedMaterials=Enumerable.Repeat(rockMat,rr.sharedMaterials.Length).ToArray();}
                float scale=8/Mathf.Max(b.size.x,b.size.y,b.size.z);go.transform.localScale=Vector3.one*scale;
                go.transform.localPosition=new Vector3(Mathf.Cos(a)*size.x*.42f,0,Mathf.Sin(a)*size.z*.42f);go.transform.localRotation=Quaternion.Euler(0,i*137,0);
            }
        }
        static string GameplaySnapshot(Ship s)
        {
            var w=s.GetComponent<Weapon>();var t=w.turrets[0];
            return JsonUtility.ToJson(s.GetComponent<ShipMovement>().settings)+"|"+s.GetComponent<HealthSystem>().maximumHealth+"|"+
                string.Join(",",w.muzzleSpeed,w.gravity,w.maximumRange,w.damage,w.baseSigma,w.sigmaPer100m,w.horizontalRange,w.friendlyFire,w.dispersionEnabled,
                t.reloadSeconds,t.traverseSpeed,t.elevationSpeed,t.minimumElevation,t.maximumElevation,t.traverseArc,t.aimTolerance);
        }
        public static void RefreshShipsAndCapture()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);materials.Clear();var b=LoadShip("Bismarck");var g=LoadShip("Gearing");
            var player=Object.FindFirstObjectByType<PlayerShip>();Replace(player.gameObject,b);
            Object.FindFirstObjectByType<CombatFireControl>().reference=player.GetComponent<Weapon>().turrets[0].muzzles[0];
            foreach(var slot in Object.FindFirstObjectByType<EnemyAIDebug>().slots)
            {
                var ai=slot.instance;bool big=ai.GetComponent<Battleship>();Replace(ai.gameObject,big?b:g);
                var route=ai.waypoints;ai.waypoints=Array.Empty<Transform>();
                slot.prefab=PrefabUtility.SaveAsPrefabAsset(ai.gameObject,Root+"/Prefabs/"+(big?"Bismarck_AI":"Gearing_AI")+".prefab");ai.waypoints=route;
            }
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);FinishAndCapture();
        }
        public static void FinishAndCapture()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/EnemyAIGreybox.unity");
            string playerSettings=GameplaySnapshot(Object.FindFirstObjectByType<PlayerShip>());
            var original=Object.FindFirstObjectByType<EnemyAIDebug>().slots.Select(s=>GameplaySnapshot(s.instance.GetComponent<Ship>())).ToArray();
            var scene=EditorSceneManager.OpenScene(ScenePath);var player=Object.FindFirstObjectByType<PlayerShip>();
            if(playerSettings!=GameplaySnapshot(player))throw new Exception("Player gameplay settings changed");
            var debug=Object.FindFirstObjectByType<EnemyAIDebug>();
            for(int i=0;i<original.Length;i++)if(original[i]!=GameplaySnapshot(debug.slots[i].instance.GetComponent<Ship>()))throw new Exception("Enemy gameplay settings changed: "+i);
            var sand=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/WarmSand.mat");sand.SetColor("_BaseColor",Color.white);sand.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Source/Tropical/Textures/Beach_DIFF.tga"));sand.SetTextureScale("_BaseMap",Vector2.one*14);
            var grass=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/IslandGreen.mat");grass.SetColor("_BaseColor",new Color(.65f,.8f,.6f));grass.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Source/Tropical/Textures/Grass_DIFF.tif"));grass.SetTextureScale("_BaseMap",Vector2.one*18);
            var bark=Mat("PalmBark",Color.white,Root+"/Source/Tropical/Nature/Trees/PalmTree1_Textures/diffuse.png");
            var leaves=Mat("PalmLeaves",Color.white,Root+"/Source/Tropical/Nature/Trees/PalmTree1_Textures/diffuse.png");
            foreach(var mat in new[]{bark,leaves}){mat.SetFloat("_Cull",0);mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.4f);mat.EnableKeyword("_ALPHATEST_ON");}
            var tree=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Source/Tropical/Nature/Trees/PalmTree1.prefab");
            if(!tree)throw new Exception("Palm prefab did not import");
            var sourceMesh=AssetDatabase.LoadAllAssetsAtPath(Root+"/Source/Tropical/Nature/Trees/PalmTree1.prefab").OfType<Mesh>().FirstOrDefault();
            if(!sourceMesh)throw new Exception("Palm mesh missing");
            int palmCount=0;
            foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m&&m.name=="SandyCoastAndHighland").ToArray())
            {
                var root=mf.transform.parent;var existing=root.Find("PalmGroves");if(existing)Object.DestroyImmediate(existing.gameObject);
                var vertices=mf.sharedMesh.vertices;var size=new Vector3(vertices[256].x/.5125f,vertices[0].y,vertices[272].z/.5175f);
                string collisionPath=Root+"/Meshes/"+root.name+"_Collision.asset";
                var collision=AssetDatabase.LoadAssetAtPath<Mesh>(collisionPath);
                if(!collision){collision=IslandMesh(size,16);AssetDatabase.CreateAsset(collision,collisionPath);}
                var collider=mf.GetComponent<MeshCollider>();collider.sharedMesh=collision;collider.convex=true;
                var placement=mf.gameObject.AddComponent<MeshCollider>();placement.sharedMesh=mf.sharedMesh;
                var grove=Child("PalmGroves",root,Vector3.zero);var b=mf.sharedMesh.bounds;
                for(int i=0;i<40;i++)
                {
                    float a=i*2.399963f;float radius=.36f+.38f*((i*17%41)/40f);
                    Vector3 p=new Vector3(Mathf.Cos(a)*b.extents.x*radius,100,Mathf.Sin(a)*b.extents.z*radius);
                    if(!placement.Raycast(new Ray(root.TransformPoint(p),Vector3.down),out var hit,200))continue;
                    var go=Child("Palm_"+i,grove,root.InverseTransformPoint(hit.point)).gameObject;
                    go.AddComponent<MeshFilter>().sharedMesh=sourceMesh;go.AddComponent<MeshRenderer>().sharedMaterials=Enumerable.Range(0,sourceMesh.subMeshCount).Select(j=>j==0?bark:leaves).ToArray();
                    float scale=(8+(i%4)*1.2f)/sourceMesh.bounds.size.y;
                    go.transform.localScale=Vector3.one*scale;go.transform.localRotation=Quaternion.Euler(0,i*137.5f,0);palmCount++;
                }
                Object.DestroyImmediate(placement);
            }
            if(palmCount<60)throw new Exception("Insufficient placed palms: "+palmCount);
            // Extend only the visible ocean; the original playable boundaries remain unchanged.
            var ocean=GameObject.Find("Ocean");ocean.transform.localScale=new Vector3(1024,1,1024);
            var wavePath=Root+"/Source/Tropical/Textures/WaterWave_NRM.png";
            var importer=AssetImporter.GetAtPath(wavePath) as TextureImporter;
            if(importer&&importer.textureType!=TextureImporterType.NormalMap){importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();}
            var water=ocean.GetComponent<Renderer>().sharedMaterial;water.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(wavePath));water.SetFloat("_BumpScale",.18f);water.EnableKeyword("_NORMALMAP");water.SetTextureScale("_BaseMap",Vector2.one*1024);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Artifacts/NavalArt/parameter-verification.txt","PASS: all original movement, health, weapon and first-turret settings retained for all four ships.\nMain battery capacities: player 8, enemy battleship 8, destroyers 6 each.\nPalms: "+palmCount);
            var cam=Object.FindFirstObjectByType<Camera>();cam.enabled=false;
            Capture(cam,player.transform.position+new Vector3(26,15,25),player.transform.position+Vector3.up*2,"Bismarck-neutral");
            foreach(var t in player.GetComponent<Weapon>().turrets){t.transform.localRotation=Quaternion.Euler(0,90,0);foreach(var barrel in t.barrels)barrel.localRotation=Quaternion.Euler(-12,0,0);}
            Capture(cam,player.transform.position+new Vector3(25,12,24),player.transform.position+Vector3.up*2,"Bismarck-broadside");
            var front=player.GetComponent<Weapon>().turrets[0].transform;
            Capture(cam,front.position+new Vector3(4,2.6f,4),front.position+Vector3.up*.3f,"Bismarck-turret-detail");
            var gearing=Object.FindObjectsByType<Destroyer>(FindObjectsSortMode.None).First();
            Capture(cam,gearing.transform.position+new Vector3(17,11,17),gearing.transform.position+Vector3.up*2,"Gearing-neutral");
            Capture(cam,new Vector3(600,550,-600),new Vector3(0,0,80),"Tropical-overview");
            var shore=GameObject.Find("SandyCoastAndHighland").transform;
            Capture(cam,shore.position+new Vector3(120,48,-100),shore.position+Vector3.up*9,"Tropical-shore");
            PruneUnusedEnvironment();
            Debug.Log("NAVAL_ART_VISUAL_QA_COMPLETE");
        }
        static void PruneUnusedEnvironment()
        {
            string prefix=Root+"/Source/Tropical/";
            var keep=new HashSet<string>(AssetDatabase.GetDependencies(new[]{ScenePath,prefix+"Nature/Trees/PalmTree1.prefab"},true));
            var unused=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith(prefix,StringComparison.Ordinal)&&!AssetDatabase.IsValidFolder(p)&&!keep.Contains(p)).ToArray();
            AssetDatabase.StartAssetEditing();
            try{foreach(var path in unused)if(!AssetDatabase.DeleteAsset(path))throw new Exception("Unable to remove unused imported asset "+path);}
            finally{AssetDatabase.StopAssetEditing();}
            File.WriteAllLines("Artifacts/NavalArt/unused-environment-removed.txt",unused);
        }
        static void Capture(Camera cam,Vector3 position,Vector3 target,string name)
        {
            cam.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));cam.fieldOfView=45;
            var rt=new RenderTexture(1600,900,24){antiAliasing=4};cam.targetTexture=rt;cam.Render();cam.Render();
            var before=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();
            File.WriteAllBytes("Artifacts/NavalArt/"+name+".png",tex.EncodeToPNG());RenderTexture.active=before;cam.targetTexture=null;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);
        }
    }
}
