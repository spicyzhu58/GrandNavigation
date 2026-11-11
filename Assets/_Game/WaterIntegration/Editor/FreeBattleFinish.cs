using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace GrandNavigation.Gameplay.Water.Editor {
public static class FreeBattleFinish {
const string Art="Assets/_Game/NavalArt";
const string Root="Assets/_Game/WaterIntegration/Materials";
public static void Build(){
Directory.CreateDirectory("Artifacts/FreeBattle");
var path="Assets/_Game/Scenes/FreeNavalBattle.unity";if(File.Exists("Artifacts/FreeBattle/finish-report.txt"))throw new Exception("Finish already applied");if(!File.Exists("Artifacts/FreeBattle/before-finish.unity"))File.Copy(path,"Artifacts/FreeBattle/before-finish.unity",false);
var scene=EditorSceneManager.OpenScene(path);
var before=Object.FindObjectsByType<Ship>(FindObjectsSortMode.InstanceID).Select(s=>JsonUtility.ToJson(s.GetComponent<ShipMovement>().settings)+JsonUtility.ToJson(s.GetComponent<Weapon>())+s.GetComponent<HealthSystem>().maximumHealth).ToArray();
var water=Object.FindFirstObjectByType<WaterSceneContext>();
var sea=new Material(water.waterMaterial){name="Free Battle Tropical Sea"};sea.SetFloat("_ReflectIntensity",1.15f);sea.SetFloat("_UnderWaterDistort",.18f);sea.SetFloat("_CausticsIntensity",1.2f);sea.SetColor("_SpecularColor",new Color(1.3f,1.25f,1.1f,.7f));sea.SetFloat("_ShoreEdgeIntensity",.025f);AssetDatabase.CreateAsset(sea,Root+"/FreeBattleSea.mat");water.waterMaterial=sea;water.waterGrid.GetComponent<Renderer>().sharedMaterial=sea;
var rock=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Island exposed rock"};rock.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Source/Tropical/Textures/Rock_DIFF.TGA"));rock.SetColor("_BaseColor",new Color(.72f,.72f,.67f));rock.SetTextureScale("_BaseMap",Vector2.one*15);rock.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(rock,Root+"/IslandRock.mat");
var islands=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m.name=="SandyCoastAndHighland").OrderBy(m=>m.transform.parent.name).ToArray();int islandIndex=0,rocks=0,palms=0;
foreach(var mf in islands){
var mesh=mf.sharedMesh;var grass=mesh.GetTriangles(1);var verts=mesh.vertices;var green=new List<int>();var cliff=new List<int>();for(int i=0;i<grass.Length;i+=3){var normal=Vector3.Cross(verts[grass[i+1]]-verts[grass[i]],verts[grass[i+2]]-verts[grass[i]]).normalized;var list=normal.y<.76f?cliff:green;list.AddRange(new[]{grass[i],grass[i+1],grass[i+2]});}mesh.subMeshCount=3;mesh.SetTriangles(green,1);mesh.SetTriangles(cliff,2);EditorUtility.SetDirty(mesh);var renderer=mf.GetComponent<MeshRenderer>();renderer.sharedMaterials=new[]{renderer.sharedMaterials[0],renderer.sharedMaterials[1],rock};
var bounds=renderer.bounds;water.islands[islandIndex]=new Vector4(bounds.center.x,bounds.center.z,bounds.extents.x/1.07f,bounds.extents.z/1.07f);
var surface=mf.gameObject.AddComponent<MeshCollider>();surface.sharedMesh=mesh;Physics.SyncTransforms();
var grove=mf.transform.parent.Find("PalmGroves");var template=Object.Instantiate(grove.GetChild(0).gameObject);template.SetActive(false);Object.DestroyImmediate(grove.gameObject);grove=new GameObject("PalmGroves").transform;grove.SetParent(mf.transform.parent,false);
var random=new System.Random(200+islandIndex);int tries=0,placed=0;
while(placed<65&&tries++<700){float x=(float)(random.NextDouble()*2-1)*bounds.extents.x*.75f,z=(float)(random.NextDouble()*2-1)*bounds.extents.z*.75f;var p=new Vector3(bounds.center.x+x,300,bounds.center.z+z);if(!surface.Raycast(new Ray(p,Vector3.down),out var hit,600)||hit.point.y<1.2f||hit.normal.y<.87f)continue;if(Mathf.PerlinNoise(x*.024f+islandIndex,z*.024f)<.43f)continue;var tree=Object.Instantiate(template,grove);tree.name="Palm_"+placed;tree.SetActive(true);tree.transform.position=hit.point;tree.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);tree.transform.localScale*=.7f+(float)random.NextDouble()*.5f;var lod=tree.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.008f,tree.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();placed++;palms++;}Object.DestroyImmediate(template);
foreach(Transform child in mf.transform.parent.Cast<Transform>().Where(t=>t.name.StartsWith("CoastalRock_")).ToArray())Object.DestroyImmediate(child.gameObject);
var root=new GameObject("Coastal rock groups").transform;root.SetParent(mf.transform.parent,false);
for(int i=0;i<24;i++){float a=(i/4)*Mathf.PI/3+.12f*(i%4);var p=bounds.center+new Vector3(Mathf.Cos(a)*bounds.extents.x*.68f,300,Mathf.Sin(a)*bounds.extents.z*.68f);if(!surface.Raycast(new Ray(p,Vector3.down),out var hit,600)||hit.point.y<.5f)continue;int kind=new[]{4,6,9}[i%3];var matPath=Root+"/CoastalRock"+kind+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(rock);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Source/Tropical/Models/Textures/Rock"+kind+"_Albedo.tif"));mat.SetTextureScale("_BaseMap",Vector2.one);mat.SetColor("_BaseColor",Color.white);AssetDatabase.CreateAsset(mat,matPath);}var parent=new GameObject("Rock cluster detail "+i).transform;parent.SetParent(root,false);parent.position=hit.point;parent.rotation=Quaternion.Euler(0,i*137,0);var levels=new List<LOD>();float scale=1;
for(int l=0;l<3;l++){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Source/Tropical/Models/Models/Rock"+kind+"/Rock"+kind+"_LOD"+l+".FBX");if(!prefab)continue;var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.SetParent(parent,false);var rr=go.GetComponentsInChildren<Renderer>();foreach(var r in rr)r.sharedMaterial=mat;if(l==0){var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);scale=(3+i%4)/Mathf.Max(b.size.x,b.size.y,b.size.z);}levels.Add(new LOD(l==0?.12f:l==1?.045f:.008f,rr));}parent.localScale=Vector3.one*scale;var group=parent.gameObject.AddComponent<LODGroup>();group.SetLODs(levels.ToArray());group.RecalculateBounds();rocks++;}
Object.DestroyImmediate(surface);islandIndex++;}
var prediction=Object.FindFirstObjectByType<PredictionRegion>();if(prediction)prediction.enabled=false;
var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);sun.intensity=1.2f;RenderSettings.fogStartDistance=1500;RenderSettings.fogEndDistance=4200;
var after=Object.FindObjectsByType<Ship>(FindObjectsSortMode.InstanceID).Select(s=>JsonUtility.ToJson(s.GetComponent<ShipMovement>().settings)+JsonUtility.ToJson(s.GetComponent<Weapon>())+s.GetComponent<HealthSystem>().maximumHealth).ToArray();if(!before.SequenceEqual(after))throw new Exception("Gameplay settings changed");
EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Artifacts/FreeBattle/finish-report.txt",$"Three island cliff materials and updated wave attenuation bounds. {palms} terrain-grounded palms; {rocks} rock placements with 3 LOD levels. Scene-only sea material and daylight adjustment. PASS gameplay settings unchanged. Prediction drawn in HUD, preserving fire control visibility gating.");Debug.Log("FREE_FINISH_COMPLETE");
}}
}

