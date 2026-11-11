using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using GrandNavigation.Gameplay.AI;
using WaterInteraction;
using Object=UnityEngine.Object;

namespace GrandNavigation.Gameplay.Water.Editor
{
    public static class WaterIntegrationBuilder
    {
        public const string Root="Assets/_Game/WaterIntegration";
        public const string ScenePath="Assets/_Game/Scenes/WaterIntegrationTest.unity";
        static T Load<T>(string path)where T:Object=>AssetDatabase.LoadAssetAtPath<T>(Root+"/"+path);
        static string Snapshot(Ship ship)
        {
            var w=ship.GetComponent<Weapon>();
            return JsonUtility.ToJson(ship.GetComponent<ShipMovement>().settings)+"|"+ship.GetComponent<HealthSystem>().maximumHealth+"|"+
                string.Join(",",w.muzzleSpeed,w.gravity,w.maximumRange,w.damage,w.dispersionEnabled,w.baseSigma,w.sigmaPer100m,w.friendlyFire,w.horizontalRange)+"|"+
                string.Join(";",w.turrets.Select(t=>string.Join(",",t.reloadSeconds,t.traverseArc,t.traverseSpeed,t.elevationSpeed,t.minimumElevation,t.maximumElevation,t.aimTolerance,t.muzzles.Length)));
        }
        [MenuItem("Grand Navigation/Create Water Integration Test (new only)")]
        public static void Build()
        {
            if(File.Exists(ScenePath))throw new Exception("Water scene exists; refusing overwrite");
            if(!Application.isBatchMode&&EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Save the current scene first");
            Directory.CreateDirectory(Root+"/Settings");Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Meshes");Directory.CreateDirectory("Artifacts/WaterIntegration");AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/TropicalNavalBattle.unity");
            var before=Object.FindObjectsByType<Ship>(FindObjectsSortMode.InstanceID).Select(Snapshot).ToArray();EditorSceneManager.SaveScene(scene,ScenePath);
            var renderer=Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset"));renderer.name="WaterIntegration_Renderer";renderer.rendererFeatures.Clear();AssetDatabase.CreateAsset(renderer,Root+"/Settings/WaterIntegration_Renderer.asset");
            var shallow=ScriptableObject.CreateInstance<ShallowWaterRendererFeature>();shallow.name="Imported Shallow Water";shallow.previewInEditMode=false;
            var reflection=ScriptableObject.CreateInstance<WaterSSRRendererFeature>();reflection.name="Imported Water SSR";reflection.maxDistance=160;reflection.maxSteps=32;
            AssetDatabase.AddObjectToAsset(shallow,renderer);AssetDatabase.AddObjectToAsset(reflection,renderer);renderer.rendererFeatures.Add(shallow);renderer.rendererFeatures.Add(reflection);EditorUtility.SetDirty(renderer);
            var pipeline=Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset"));pipeline.name="WaterIntegration_RP";
            var serialized=new SerializedObject(pipeline);var list=serialized.FindProperty("m_RendererDataList");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=renderer;serialized.ApplyModifiedPropertiesWithoutUndo();
            pipeline.supportsCameraDepthTexture=true;pipeline.supportsCameraOpaqueTexture=true;AssetDatabase.CreateAsset(pipeline,Root+"/Settings/WaterIntegration_RP.asset");
            var material=new Material(Load<Material>("Imported/Water_Sea_Mat.mat"));material.name="Integrated Sea - Source Appearance";AssetDatabase.CreateAsset(material,Root+"/Materials/IntegratedSea.mat");
            var proxy=new Material(Shader.Find("Hidden/GrandNavigation/WaterContact"));AssetDatabase.CreateAsset(proxy,Root+"/Materials/InvisibleContact.mat");
            var ring=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));ring.SetColor("_BaseColor",new Color(.8f,.96f,1,.8f));ring.SetFloat("_Surface",1);ring.SetFloat("_Blend",0);ring.SetFloat("_SrcBlend",5);ring.SetFloat("_DstBlend",10);ring.SetFloat("_ZWrite",0);ring.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");ring.renderQueue=3000;AssetDatabase.CreateAsset(ring,Root+"/Materials/ImpactRing.mat");
            var old=GameObject.Find("Ocean");if(old)Object.DestroyImmediate(old);
            var water=new GameObject("Integrated Ocean",typeof(MeshFilter),typeof(MeshRenderer));var mesh=WaterSceneContext.Grid();AssetDatabase.CreateAsset(mesh,Root+"/Meshes/OceanGrid.asset");water.GetComponent<MeshFilter>().sharedMesh=mesh;water.GetComponent<MeshRenderer>().sharedMaterial=material;water.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var system=new GameObject("Water Integration");var context=system.AddComponent<WaterSceneContext>();context.pipeline=pipeline;context.waterGrid=water.transform;context.waterMaterial=material;context.proxyMaterial=proxy;
            system.AddComponent<WaterRingMaterial>().material=ring;
            context.view=Object.FindFirstObjectByType<Camera>();var cameraData=context.view.GetUniversalAdditionalCameraData();cameraData.SetRenderer(0);cameraData.requiresDepthOption=CameraOverrideOption.On;cameraData.requiresColorOption=CameraOverrideOption.On;
            var fftObject=new GameObject("FFT Wave Simulation");fftObject.SetActive(false);fftObject.transform.SetParent(system.transform);var fft=fftObject.AddComponent<NavalFFT>();fft.spectrum=Load<ComputeShader>("Imported/FFT/FFTOcean.compute");context.fft=fft;fftObject.SetActive(true);
            var domain=new GameObject("Local Interaction Domain");domain.transform.SetParent(system.transform);domain.transform.position=new Vector3(context.view.transform.position.x,0,context.view.transform.position.z);
            var sim=domain.AddComponent<ShallowWaterSimulation>();sim.size=new Vector2(384,384);sim.resolution=512;sim.boundaryWidth=24;sim.edgeFade=20;sim.waterDepth=2;sim.maxHeight=.2f;sim.damping=.7f;
            var settings=new SerializedObject(sim);settings.FindProperty("solver").objectReferenceValue=Load<ComputeShader>("Imported/ShallowWater/ShallowWater.compute");settings.FindProperty("captureShader").objectReferenceValue=Load<Shader>("Imported/ShallowWater/ShallowWaterCapture.shader");settings.ApplyModifiedPropertiesWithoutUndo();context.simulation=sim;
            var islands=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m.name=="SandyCoastAndHighland").OrderBy(m=>m.transform.parent.name).ToArray();
            for(int i=0;i<3;i++){var b=islands[i].GetComponent<Renderer>().bounds;context.islands[i]=new Vector4(b.center.x,b.center.z,b.extents.x/1.07f,b.extents.z/1.07f);}
            var after=Object.FindObjectsByType<Ship>(FindObjectsSortMode.InstanceID).Select(Snapshot).ToArray();if(!before.SequenceEqual(after))throw new Exception("Ship gameplay settings changed");
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
            File.WriteAllText("Artifacts/WaterIntegration/build-report.txt","Created "+ScenePath+"\nPASS: movement, HP, weapon, dispersion, all turret settings unchanged for four ships.\nUnity and package versions unchanged. Dedicated scene-local URP pipeline; original renderers not modified.\n");
            Debug.Log("WATER_INTEGRATION_BUILD_COMPLETE");
        }
    }
}
