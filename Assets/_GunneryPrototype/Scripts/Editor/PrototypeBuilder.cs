using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace GrandNavigation.Gunnery.Editor
{
    public static class PrototypeBuilder
    {
        const string Root="Assets/_GunneryPrototype/";
        public static Material Material(string name,Color color)
        {
            string path=Root+"Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
            mat.color=color; EditorUtility.SetDirty(mat); return mat;
        }
        public static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat,bool collider=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
            go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        [MenuItem("Gunnery Prototype/Build Phase 1")]
        public static void BuildPhase1()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var gray=Material("PlayerGray",new Color(.36f,.43f,.5f));
            var red=Material("TargetOrange",new Color(.85f,.22f,.05f));
            var sea=Material("Sea",new Color(.025f,.22f,.32f));
            Box("Sea - mathematical surface Y=0",null,new Vector3(0,-.3f,70),new Vector3(400,.5f,400),sea);
            var player=new GameObject("PlayerShip");
            Box("Hull",player.transform,new Vector3(0,1.3f,0),new Vector3(8,2.6f,32),gray,true);
            Box("Bridge",player.transform,new Vector3(0,4,0),new Vector3(4,4,7),gray);
            var muzzle=new GameObject("Phase1Muzzle"); muzzle.transform.SetParent(player.transform); muzzle.transform.localPosition=new Vector3(0,4,14);
            var target=new GameObject("TargetShip"); target.transform.position=new Vector3(0,0,160);
            Box("Hull",target.transform,new Vector3(0,1.5f,0),new Vector3(5,3,18),red,true);
            target.AddComponent<Health>();
            var cam=new GameObject("Main Camera").AddComponent<UnityEngine.Camera>(); cam.tag="MainCamera";
            cam.transform.position=new Vector3(65,130,-85); cam.transform.LookAt(new Vector3(0,0,65)); cam.farClipPlane=1000;
            cam.gameObject.AddComponent<AudioListener>();
            var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=2; sun.transform.rotation=Quaternion.Euler(45,-35,0);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.5f,.55f,.6f);
            var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere); shell.name="Shell"; Object.DestroyImmediate(shell.GetComponent<Collider>());
            shell.transform.localScale=Vector3.one*.35f; shell.AddComponent<ProjectileController>();
            var trail=shell.AddComponent<TrailRenderer>(); trail.time=.5f; trail.startWidth=.18f; trail.endWidth=.02f;
            trail.sharedMaterial=Material("ShellTrail",new Color(1,.8f,.25f));
            PrefabUtility.SaveAsPrefabAsset(shell,Root+"Prefabs/Projectile.prefab"); Object.DestroyImmediate(shell);
            EditorSceneManager.SaveScene(scene,Root+"Scenes/GunneryPrototype.unity"); AssetDatabase.SaveAssets();
        }
    }
}
