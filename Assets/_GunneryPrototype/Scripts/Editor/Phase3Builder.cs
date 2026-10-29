using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace GrandNavigation.Gunnery.Editor
{
    public static class Phase3Builder
    {
        [MenuItem("Gunnery Prototype/Build Complete Prototype")]
        public static void Build()
        {
            Phase2Builder.Build();
            var session=Object.FindFirstObjectByType<PrototypeSession>();
            var player=GameObject.Find("PlayerShip");
            GameObject.Find("Phase1Muzzle").name="FireControlReference";
            var gray=PrototypeBuilder.Material("PlayerGray",new Color(.36f,.43f,.5f));
            var dark=PrototypeBuilder.Material("GunSteel",new Color(.12f,.16f,.2f));
            var weapons=session.gameObject.AddComponent<WeaponManager>(); weapons.session=session; weapons.shooter=player.transform;
            weapons.projectilePrefab=AssetDatabase.LoadAssetAtPath<ProjectileController>("Assets/_GunneryPrototype/Prefabs/Projectile.prefab");
            weapons.turrets=new TurretController[3];
            float[] z={11,5,-11};
            for(int i=0;i<3;i++)
            {
                var mount=new GameObject("Turret "+(i+1)); mount.transform.SetParent(player.transform,false); mount.transform.localPosition=new Vector3(0,i==1?4.8f:3.2f,z[i]);
                var turret=mount.AddComponent<TurretController>(); weapons.turrets[i]=turret; turret.aim=session.aim; turret.prediction=session.prediction;
                var yaw=new GameObject("Yaw"); yaw.transform.SetParent(mount.transform,false); turret.yawBase=yaw.transform;
                PrototypeBuilder.Box("Armored housing",yaw.transform,Vector3.zero,new Vector3(3.4f,1.4f,3.1f),gray);
                var pitch=new GameObject("Elevation"); pitch.transform.SetParent(yaw.transform,false); pitch.transform.localPosition=Vector3.up*.4f; turret.elevationPivot=pitch.transform;
                for(int j=0;j<2;j++)
                {
                    float x=j==0?-.7f:.7f;
                    PrototypeBuilder.Box("Barrel "+(j+1),pitch.transform,new Vector3(x,0,3.1f),new Vector3(.32f,.32f,5.4f),dark);
                    var muzzle=new GameObject("Muzzle "+(j+1)); muzzle.transform.SetParent(pitch.transform,false); muzzle.transform.localPosition=new Vector3(x,0,5.8f); turret.muzzles[j]=muzzle.transform;
                }
            }
            session.gameObject.AddComponent<CombatAudio>();
            var hud=session.gameObject.AddComponent<WeaponsHUD>(); hud.weapons=weapons; hud.session=session;
            // Ship prefabs contain geometry, colliders and weapon hardware. Scene systems supply targeting references.
            var prefab=PrefabUtility.SaveAsPrefabAsset(player,"Assets/_GunneryPrototype/Prefabs/PlayerShip.prefab");
            PrefabUtility.SaveAsPrefabAsset(session.target.gameObject,"Assets/_GunneryPrototype/Prefabs/TargetShip.prefab");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        }
    }
}