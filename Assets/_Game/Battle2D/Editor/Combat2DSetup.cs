using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GrandNavigation.Gameplay.AI;
namespace GrandNavigation.Battle2D.Editor
{
    public static class Combat2DSetup
    {
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new System.Exception("Exit Play first");
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Battle2D/Sprites/White.png");
            System.IO.Directory.CreateDirectory("Assets/_Game/Battle2D/Prefabs");System.IO.Directory.CreateDirectory("Assets/_Game/Battle2D/Profiles");AssetDatabase.Refresh();
            var shell=new GameObject("Shell2D",typeof(SpriteRenderer),typeof(Rigidbody2D),typeof(CircleCollider2D),typeof(Projectile2D));
            shell.GetComponent<SpriteRenderer>().sprite=sprite;shell.GetComponent<SpriteRenderer>().color=Color.yellow;shell.GetComponent<SpriteRenderer>().sortingOrder=10;
            shell.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;shell.GetComponent<Rigidbody2D>().gravityScale=0;shell.GetComponent<CircleCollider2D>().isTrigger=true;
            var prefab=PrefabUtility.SaveAsPrefabAsset(shell,"Assets/_Game/Battle2D/Prefabs/Shell2D.prefab");Object.DestroyImmediate(shell);
            var input=Object.FindFirstObjectByType<PlayerInput2D>();input.view=Camera.main;
            foreach(var ship in Object.FindObjectsByType<ShipActor2D>(FindObjectsSortMode.None))
            {
                var weapon=ship.gameObject.AddComponent<Weapon2D>();weapon.projectile=prefab.GetComponent<Projectile2D>();
                if(ship.kind==ShipKind2D.Player)continue;
                bool small=ship.kind==ShipKind2D.Destroyer;
                var profile=ScriptableObject.CreateInstance<EnemyAIProfile>();profile.detectionRange=small?260:320;profile.engageEnterRange=small?120:180;profile.engageExitRange=small?160:220;
                profile.preferredCombatRange=small?85:130;profile.combatRangeTolerance=20;profile.decisionInterval=small?.5f:1;profile.minStateTime=2;profile.targetForgetDelay=5;profile.retreatHealthRatio=0;
                profile.patrolSpeed=small?9:6;profile.approachSpeed=small?14:10;profile.combatSpeed=small?10:7;profile.reactionDelay=1;profile.aimErrorRadius=small?3:5;
                AssetDatabase.CreateAsset(profile,"Assets/_Game/Battle2D/Profiles/"+ship.name+".asset");
                var ai=ship.gameObject.AddComponent<EnemyAI2D>();ai.player=input.ship;ai.profile=profile;weapon.reload=small?4:5;weapon.salvo=small?2:4;
                ship.GetComponent<ShipMovement2D>().settings.maxForwardSpeed=small?14:10;
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
    }
}
