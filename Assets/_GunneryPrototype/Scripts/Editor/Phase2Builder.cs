using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace GrandNavigation.Gunnery.Editor
{
    public static class Phase2Builder
    {
        [MenuItem("Gunnery Prototype/Build Phase 2")]
        public static void Build()
        {
            PrototypeBuilder.BuildPhase1();
            var player=GameObject.Find("PlayerShip"); var target=GameObject.Find("TargetShip");
            target.transform.rotation=Quaternion.Euler(0,90,0);
            target.AddComponent<Rigidbody>().isKinematic=true;
            var motion=target.AddComponent<TargetMotion>();
            PrototypeBuilder.Box("Bridge",target.transform,new Vector3(0,3,0),new Vector3(3,3,5),
                PrototypeBuilder.Material("TargetOrange",new Color(.85f,.22f,.05f)));
            var root=new GameObject("Gunnery Systems");
            var selection=root.AddComponent<TargetSelectionController>();
            var prediction=root.AddComponent<LeadPredictionController>(); prediction.selection=selection;
            prediction.fireControl=GameObject.Find("Phase1Muzzle").transform;
            var aim=root.AddComponent<ManualAimController>(); aim.prediction=prediction;
            var rig=root.AddComponent<PrototypeCameraController>(); rig.view=UnityEngine.Camera.main; rig.player=player.transform; rig.selection=selection; rig.aim=aim;
            var session=root.AddComponent<PrototypeSession>(); session.selection=selection; session.prediction=prediction; session.aim=aim; session.cameraRig=rig; session.target=motion;
            root.AddComponent<PrototypeHUD>().session=session;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        }
    }
}