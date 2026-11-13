using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using GrandNavigation.Gameplay;
using GrandNavigation.Gameplay.AI;
using GrandNavigation.Gameplay.Levels;
using GrandNavigation.Gameplay.Battle;
using GrandNavigation.Gameplay.Water;
using GrandNavigation.Seamless;
using GrandNavigation.GameFlow;
using Object=UnityEngine.Object;
namespace GrandNavigation.FinalIntegration.Editor
{
 public static class FinalSceneBuilder
 {
  public const string Path="Assets/_Game/Scenes/FinalIntegratedGame.unity";
  static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  static GameObject Copy(GameObject original,Scene scene,Transform parent=null){var go=Object.Instantiate(original);go.name=original.name;if(parent){go.transform.SetParent(parent,false);go.transform.localPosition=original.transform.localPosition;go.transform.localRotation=original.transform.localRotation;go.transform.localScale=original.transform.localScale;}else SceneManager.MoveGameObjectToScene(go,scene);return go;}
  public static void Build()
  {
   if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty||System.IO.File.Exists(Path))throw new Exception("Save Edit Mode scene; destination must be new.");
   var final=EditorSceneManager.OpenScene("Assets/_Game/Scenes/IntegratedGame.unity");EditorSceneManager.SaveScene(final,Path);
   var source=EditorSceneManager.OpenScene("Assets/_Game/Scenes/FreeNavalBattle.unity",OpenSceneMode.Additive);
   SceneManager.SetActiveScene(source);
   var sky=RenderSettings.skybox;var fog=RenderSettings.fog;var fogColor=RenderSettings.fogColor;var fogMode=RenderSettings.fogMode;var start=RenderSettings.fogStartDistance;var end=RenderSettings.fogEndDistance;
   var ambientMode=RenderSettings.ambientMode;var skyColor=RenderSettings.ambientSkyColor;var equator=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
   SceneManager.SetActiveScene(final);
   var player=All<PlayerShip>(final).Single();var sourcePlayer=All<PlayerShip>(source).Single();ReplaceVisual(player,sourcePlayer,true);
   var fc=All<CombatFireControl>(final).Single();fc.reference=player.GetComponent<Weapon>().turrets[0].muzzles[0];
   var debug=All<EnemyAIDebug>(final).Single();
   System.IO.Directory.CreateDirectory("Assets/_Game/FinalIntegration/Prefabs");AssetDatabase.Refresh();
   foreach(var slot in debug.slots){var ship=slot.instance.GetComponent<Ship>();var art=All<EnemyShip>(source).First(s=>(s is Battleship)==(ship is Battleship));ReplaceVisual(ship,art,false);
    var route=slot.instance.waypoints;slot.instance.waypoints=Array.Empty<Transform>();string path="Assets/_Game/FinalIntegration/Prefabs/"+ship.name+".prefab";
    PrefabUtility.SaveAsPrefabAsset(ship.gameObject,path);slot.instance.waypoints=route;slot.prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   }
   foreach(var island in All<LevelIsland>(final)){
    var root=island.transform;var art=All<Transform>(source).Single(t=>t.name==root.name);if(Vector3.Distance(art.position,root.position)>.01f)throw new Exception("Island position mismatch: "+root.name);
    foreach(Transform child in root.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);Object.DestroyImmediate(island);
    foreach(Transform child in art)Copy(child.gameObject,final,root);
   }
   var oldOcean=All<Transform>(final).FirstOrDefault(t=>t.name=="Ocean");if(oldOcean)Object.DestroyImmediate(oldOcean.gameObject);
   var sourceWater=All<WaterSceneContext>(source).Single();var ocean=Copy(sourceWater.waterGrid.gameObject,final);var waterRoot=Copy(sourceWater.gameObject,final);var water=waterRoot.GetComponent<WaterSceneContext>();
   water.waterGrid=ocean.transform;water.view=All<GameFlowController>(final).Single().rig.view;
   var sourceLight=All<Light>(source).First(l=>l.type==LightType.Directional);var sun=All<Light>(final).First(l=>l.type==LightType.Directional);EditorUtility.CopySerialized(sourceLight,sun);sun.transform.rotation=sourceLight.transform.rotation;
   RenderSettings.skybox=sky;RenderSettings.sun=sun;RenderSettings.fog=fog;RenderSettings.fogColor=fogColor;RenderSettings.fogMode=fogMode;RenderSettings.fogStartDistance=start;RenderSettings.fogEndDistance=end;RenderSettings.ambientMode=ambientMode;RenderSettings.ambientSkyColor=skyColor;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
   water.view.clearFlags=CameraClearFlags.Skybox;
   var region=All<PredictionRegion>(final).Single();region.enabled=true;region.screenOverlay=true;All<CombatHUD>(final).Single().predictionRegion=region;
   var battle=All<BattleManager>(final).Single();var routes=ScriptableObject.CreateInstance<GameSceneRoutes>();routes.mainMenuScene=routes.gameplayScene=Path;AssetDatabase.CreateAsset(routes,"Assets/_Game/FinalIntegration/FinalRoutes.asset");battle.resultUI.sceneRoutes=routes;
   EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(final);
   EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Path,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=Path)).ToArray();
   EditorSceneManager.SaveScene(final);AssetDatabase.SaveAssets();
   Debug.Log("FINAL_INTEGRATION_CREATED: original gameplay roots and routes preserved; model-specific batteries, original AI and weapon parameters.");
  }
  static void ReplaceVisual(Ship destination,Ship source,bool player)
  {
   if(PrefabUtility.IsPartOfPrefabInstance(destination))PrefabUtility.UnpackPrefabInstance(destination.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   var weapon=destination.GetComponent<Weapon>();var old=weapon.turrets;
   var settings=old.Select(EditorJsonUtility.ToJson).ToArray();
   var before=destination.transform.Find("VisualModel");if(!before)throw new Exception("Missing visual");
   var visual=Copy(source.transform.Find("VisualModel").gameObject,destination.gameObject.scene,destination.transform).transform;
   visual.localPosition=new Vector3(0,-destination.transform.position.y,0);
   var turrets=visual.GetComponentsInChildren<CombatTurret>();var selected=turrets;

   for(int i=0;i<selected.Length;i++){var t=selected[i];var barrels=t.barrels.ToArray();var muzzles=t.muzzles.ToArray();float yaw=t.neutralYaw;EditorJsonUtility.FromJsonOverwrite(settings[Mathf.Min(i,settings.Length-1)],t);t.barrels=barrels;t.muzzles=muzzles;t.neutralYaw=yaw;}

   weapon.turrets=selected;destination.GetComponent<ShipSinking>().visual=visual;Object.DestroyImmediate(before.gameObject);
  }
 }
}
