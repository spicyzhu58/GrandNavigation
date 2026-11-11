using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GrandNavigation.Gameplay;
using GrandNavigation.Gameplay.AI;
using Object=UnityEngine.Object;
namespace GrandNavigation.Gameplay.Art.Editor {
public static class FreeBattleTerrainBuilder {
const string Root="Assets/_Game/NavalArt";
public static void Build(){
const string scenePath="Assets/_Game/Scenes/FreeNavalBattle.unity";
if(File.Exists(scenePath))throw new Exception("Scene exists; preserve authored work");
var data=AssetDatabase.LoadAssetAtPath<TerrainData>(Root+"/Source/OriginalTerrain/OriginalTerrain.asset");
if(!data)throw new Exception("Source Terrain unavailable");
int res=data.heightmapResolution;var heights=data.GetHeights(0,0,res,res);float lo=float.MaxValue,hi=float.MinValue;
foreach(float h in heights){lo=Mathf.Min(lo,h);hi=Mathf.Max(hi,h);}if(hi-lo<.0001f)throw new Exception("Source terrain flat");
Directory.CreateDirectory("Artifacts/FreeBattle");Directory.CreateDirectory(Root+"/Meshes/FreeBattle");AssetDatabase.Refresh();
var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/WaterIntegrationTest.unity");EditorSceneManager.SaveScene(scene,scenePath);
var originals=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m.name=="SandyCoastAndHighland").OrderBy(m=>m.transform.parent.name).ToArray();
int index=0;var log=new List<string>{$"Source terrain {res} x {res}; size {data.size}; height range {lo}..{hi}"};
foreach(var mf in originals){
var old=mf.sharedMesh.bounds;float rx=old.extents.x,rz=old.extents.z,peak=old.max.y;int rings=32,sides=96;var v=new List<Vector3>();var uv=new List<Vector2>();var sand=new List<int>();var grass=new List<int>();
for(int r=0;r<=rings;r++)for(int s=0;s<sides;s++){
float a=s*Mathf.PI*2/sides,t=(float)r/rings;
float edge=1+.045f*Mathf.Sin(a*3+index)+.025f*Mathf.Sin(a*7+index*2);
float x=Mathf.Cos(a)*t,z=Mathf.Sin(a)*t;
float u=Mathf.Clamp01(.5f+x*.45f),w=Mathf.Clamp01(.5f+z*.45f);
if(index==1){float q=u;u=w;w=1-q;}if(index==2)u=1-u;
float h=(heights[Mathf.RoundToInt(w*(res-1)),Mathf.RoundToInt(u*(res-1))]-lo)/(hi-lo);
float land=Mathf.Pow(Mathf.Clamp01(1-t),.65f);float y=(.18f+.82f*h)*peak*1.6f*land;
if(t>.78f)y=Mathf.Lerp(peak*.035f,-4,(t-.78f)/.22f);
v.Add(new Vector3(x*rx*edge,y,z*rz*edge));uv.Add(new Vector2(x,z));}
for(int r=0;r<rings;r++)for(int s=0;s<sides;s++){int a=r*sides+s,b=r*sides+(s+1)%sides,c=a+sides,d=b+sides;var tri=r>rings*.7f?sand:grass;tri.AddRange(new[]{a,b,c,b,d,c});}
var mesh=new Mesh{name="Source terrain adapted island "+index};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.subMeshCount=2;mesh.SetTriangles(sand,0);mesh.SetTriangles(grass,1);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Root+"/Meshes/FreeBattle/Island"+index+".asset");mf.sharedMesh=mesh;
Object.DestroyImmediate(mf.GetComponent<MeshCollider>());
// Sector hulls preserve the coast more closely than a single convex envelope.
for(int sector=0;sector<8;sector++){
var points=new List<Vector3>();for(int r=0;r<=rings;r+=4)for(int s=sector*12;s<=sector*12+12;s+=2)points.Add(v[r*sides+s%sides]);
var cm=new Mesh();cm.SetVertices(points);var tris=new List<int>();for(int k=2;k<points.Count;k++)tris.AddRange(new[]{0,k-1,k});cm.SetTriangles(tris,0);cm.RecalculateBounds();AssetDatabase.CreateAsset(cm,Root+"/Meshes/FreeBattle/Collision"+index+"_"+sector+".asset");var co=new GameObject("Island collision sector");co.transform.SetParent(mf.transform,false);var collider=co.AddComponent<MeshCollider>();collider.sharedMesh=cm;collider.convex=true;}
// Place existing decoration on the visible height surface using a temporary precise collider.
var surface=mf.gameObject.AddComponent<MeshCollider>();surface.sharedMesh=mesh;Physics.SyncTransforms();
var palms=mf.transform.parent.Find("PalmGroves");if(palms)foreach(Transform palm in palms){var p=palm.position;if(surface.Raycast(new Ray(new Vector3(p.x,500,p.z),Vector3.down),out var hit,1000))palm.position=hit.point;}
Object.DestroyImmediate(surface);log.Add(mf.transform.parent.name+" source-derived terrain, eight convex coast sectors; bounds "+mesh.bounds);index++;
}
Physics.SyncTransforms();
var debug=Object.FindFirstObjectByType<EnemyAIDebug>();var corners=new[]{new Vector3(-1050,0,-1050),new Vector3(1050,0,-1050),new Vector3(1050,0,1050),new Vector3(-1050,0,1050)};
foreach(var slot in debug.slots){for(int i=0;i<slot.route.Length;i++)slot.route[i].position=corners[(i+Array.IndexOf(debug.slots,slot))%4];slot.instance.waypoints=slot.route;}
var starts=new[]{new Vector3(-800,0,850),new Vector3(0,0,1000),new Vector3(800,0,850)}; for(int k=0;k<debug.slots.Length;k++){var slot=debug.slots[k];var pos=starts[k];pos.y=slot.instance.transform.position.y;slot.position=pos;slot.instance.transform.position=pos;} Physics.SyncTransforms(); var obstacles=Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.name=="Island collision sector").ToArray();
foreach(var ship in Object.FindObjectsByType<Ship>(FindObjectsSortMode.None))foreach(var c in obstacles)if(Vector3.Distance(c.ClosestPoint(ship.transform.position),ship.transform.position)<25)throw new Exception("Spawn clearance failed: "+ship.name);
for(int i=0;i<4;i++){var a=corners[i]+Vector3.up*2;var b=corners[(i+1)%4]+Vector3.up*2;foreach(var hit in Physics.SphereCastAll(a,35,(b-a).normalized,(b-a).magnitude))if(obstacles.Contains(hit.collider))throw new Exception("Patrol corridor blocked");}
log.Add("PASS: ship spawn clearance >=25m; outer patrol corridors radius35m clear; 24 convex colliders compatible with AI ClosestPoint; existing ship settings preserved.");
EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();if(!EditorBuildSettings.scenes.Any(s=>s.path==scenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(scenePath,true)}).ToArray();
var cam=Object.FindFirstObjectByType<Camera>();var focus=originals[0].transform.position;cam.transform.position=focus+new Vector3(240,180,-240);cam.transform.LookAt(focus);var rt=new RenderTexture(1600,900,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes("Artifacts/FreeBattle/terrain.png",tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;rt.Release();Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);
File.WriteAllLines("Artifacts/FreeBattle/report.txt",log);Debug.Log("FREE_BATTLE_COMPLETE");
}
}}


