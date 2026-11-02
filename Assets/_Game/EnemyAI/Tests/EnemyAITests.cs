using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
namespace GrandNavigation.Gameplay.AI.Tests
{
    public class EnemyAIPlayTests
    {
        EnemyShipAI[] enemies;PlayerShip player;EnemyAIDebug debug;float oldScale;
        [UnitySetUp]public IEnumerator Setup()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/EnemyAIGreybox.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
            yield return null;Application.runInBackground=true;oldScale=Time.timeScale;Time.timeScale=4;
            player=Object.FindFirstObjectByType<PlayerShip>();player.GetComponent<PlayerShipInput>().enabled=false;
            Object.FindFirstObjectByType<PlayerCombatInput>().enabled=false;
            debug=Object.FindFirstObjectByType<EnemyAIDebug>();debug.showHUD=false;
            enemies=Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.InstanceID);
            foreach(var e in enemies){e.profile=Object.Instantiate(e.profile);e.Ship.Weapon.dispersionEnabled=false;}
            yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=oldScale;foreach(var e in enemies)if(e && e.profile)Object.Destroy(e.profile);yield return null;}
        static void Place(Ship s,Vector3 position,float yaw=0)
        {var b=s.GetComponent<Rigidbody>();b.linearVelocity=Vector3.zero;b.angularVelocity=Vector3.zero;b.position=position;b.rotation=Quaternion.Euler(0,yaw,0);s.transform.SetPositionAndRotation(position,b.rotation);Physics.SyncTransforms();}
        void Isolate(EnemyShipAI ai)
        {foreach(var e in enemies)if(e!=ai)e.gameObject.SetActive(false);player.Movement.enabled=false;ai.Ship.Movement.enabled=false;}
        [UnityTest]public IEnumerator InitializationPatrolApproachEngageAndRealDamage()
        {
            Assert.AreEqual(3,enemies.Length);Assert.AreEqual(1,enemies.Count(e=>e.Ship is Battleship));Assert.AreEqual(2,enemies.Count(e=>e.Ship is Destroyer));
            var ai=enemies.First(e=>e.Ship is Battleship);Isolate(ai);ai.profile.aimErrorRadius=0;
            Place(ai.Ship,new Vector3(0,1,-900));Place(player,new Vector3(600,0,-900));
            int before=player.Health.CurrentHealth;float deadline=Time.time+25;bool observedReload=false;
            while(Time.time<deadline && player.Health.CurrentHealth==before){observedReload|=ai.Ship.Weapon.turrets.Any(t=>t.ReloadRemaining>0);yield return null;}
            Assert.AreEqual(EnemyState.Engage,ai.State);Assert.Less(player.Health.CurrentHealth,before,"Real NPC shells must damage the player");
            Assert.IsTrue(ai.Ship.Weapon.LastSalvoCount<=6);Assert.IsTrue(observedReload,"Fired turrets must reload, independently of impact time");
            var fire=Object.FindFirstObjectByType<CombatFireControl>();fire.Lock(ai.Ship);
            // Actual player projectile uses existing hit/owner path to finish the enemy.
            var shell=Object.Instantiate(player.Weapon.projectile,ai.transform.position+Vector3.right*30,Quaternion.identity);
            shell.Launch(ai.transform.position+Vector3.right*30,Vector3.left*140,9.81f,player,1000,false);
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(EnemyState.Destroyed,ai.State);Assert.IsFalse(ai.Ship.IsAlive);Assert.IsNull(fire.Target);
            int spawned=Projectile.SpawnedCount;yield return new WaitForSeconds(.5f);Assert.AreEqual(spawned,Projectile.SpawnedCount);
        }
        [UnityTest]public IEnumerator DamageAcquisitionRetreatAndIndependentReset()
        {
            var ai=enemies.First(e=>e.Ship is Destroyer);Isolate(ai);
            Place(ai.Ship,new Vector3(-950,1,-900));Place(player,new Vector3(950,0,-900));
            yield return new WaitForSeconds(ai.profile.targetForgetDelay+1);
            Assert.AreEqual(EnemyState.Patrol,ai.State);
            ai.Ship.TakeDamage(1);Assert.IsNull(ai.Target,"Anonymous damage does not acquire");
            Assert.IsFalse(EnemyAILogic.HostilePlayer(ai.Ship,ai.Ship));
            ai.Ship.TakeDamage(1,player);Assert.AreSame(player,ai.Target);Assert.AreEqual(EnemyState.Approach,ai.State);
            Assert.AreEqual(player.WorldPosition,ai.LastKnownPosition);
            int leave=Mathf.RoundToInt(ai.Ship.Health.maximumHealth*ai.profile.retreatHealthRatio);
            ai.Ship.TakeDamage(ai.Ship.Health.CurrentHealth-leave,player);
            Assert.AreEqual(EnemyState.Retreat,ai.State);Assert.IsNull(ai.Target);
            ai.Ship.Health.Restore();yield return new WaitForSeconds(1);Assert.AreEqual(EnemyState.Retreat,ai.State);
            int slot=System.Array.FindIndex(debug.slots,s=>s.instance==ai);var untouched=debug.slots[(slot+1)%3].instance;
            debug.ResetEnemy(slot);yield return null;yield return null;
            Assert.AreNotSame(ai,debug.slots[slot].instance);Assert.AreSame(untouched,debug.slots[(slot+1)%3].instance);
            Assert.IsTrue(debug.slots[slot].instance.Ship.IsAlive);Assert.AreNotEqual(EnemyState.Retreat,debug.slots[slot].instance.State);
        }
        [UnityTest]public IEnumerator OcclusionMemoryReactionAndTargetLoss()
        {
            var ai=enemies.First(e=>e.Ship is Battleship);Isolate(ai);
            Place(ai.Ship,new Vector3(0,1,-900));Place(player,new Vector3(600,0,-900));yield return new WaitForSeconds(4.5f);
            Assert.AreEqual(EnemyState.Engage,ai.State);var known=ai.LastKnownPosition;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(300,20,-900);wall.transform.localScale=new Vector3(10,50,200);Physics.SyncTransforms();
            yield return null;int count=Projectile.SpawnedCount;
            Place(player,new Vector3(650,0,-900));yield return new WaitForSeconds(2);
            Assert.IsFalse(ai.Visible);Assert.AreEqual(known,ai.LastKnownPosition);Assert.AreEqual(count,Projectile.SpawnedCount);
            wall.SetActive(false);yield return null;yield return new WaitForSeconds(.5f);
            Assert.IsTrue(ai.Visible);Assert.AreEqual(count,Projectile.SpawnedCount,"Reacquisition reaction delay");
            player.gameObject.SetActive(false);yield return new WaitForSeconds(1.2f);Assert.IsNull(ai.Target);Assert.AreEqual(EnemyState.Patrol,ai.State);Object.Destroy(wall);
        }
        [UnityTest]public IEnumerator SmallShipMovingCombatAndBoundedRecovery()
        {
            var ai=enemies.First(e=>e.Ship is Destroyer);foreach(var other in enemies)if(other!=ai)other.gameObject.SetActive(false);
            player.Movement.enabled=false;Place(ai.Ship,new Vector3(-500,1,-900),90);Place(player,new Vector3(-200,0,-900));
            ai.profile.aimErrorRadius=0;Vector3 start=ai.transform.position;int hp=player.Health.CurrentHealth;bool engaging=false;bool fired=false;
            float until=Time.time+25;
            while(Time.time<until && player.IsAlive)
            {
                engaging|=ai.State==EnemyState.Engage;fired|=ai.Ship.Weapon.turrets.Any(t=>t.ReloadRemaining>0);
                yield return null;
            }
            Assert.IsTrue(engaging);Assert.IsTrue(fired);Assert.Greater(EnemyAILogic.Distance(start,ai.transform.position),50);
            Assert.Less(player.Health.CurrentHealth,hp,"Moving destroyer must hit using inherited platform velocity");
            player.gameObject.SetActive(false);yield return new WaitForSeconds(ai.profile.targetForgetDelay+1);
            var goal=new GameObject("Recovery goal");goal.transform.position=ai.transform.position+ai.transform.forward*400;ai.waypoints=new[]{goal.transform};
            var body=ai.GetComponent<Rigidbody>();body.constraints=RigidbodyConstraints.FreezeAll;
            Vector3 fixedPosition=ai.transform.position;bool recovering=false;until=Time.time+ai.profile.stuckTimeout+5;
            while(Time.time<until){recovering|=ai.Navigation.Status.Contains("RECOVERY");yield return null;}
            Assert.IsTrue(recovering);Assert.Less(Vector3.Distance(fixedPosition,ai.transform.position),.1f,"Recovery cannot teleport");Object.Destroy(goal);
        }
        [UnityTest]public IEnumerator RetreatSelectsNearestAllyAndNeverReturnsToCombat()
        {
            var ai=enemies.First(e=>e.Ship is Battleship);var allies=enemies.Where(e=>e!=ai).ToArray();
            player.gameObject.SetActive(false);foreach(var e in enemies)e.Ship.Movement.enabled=false;
            Place(ai.Ship,new Vector3(0,1,-900));Place(allies[0].Ship,new Vector3(200,1,-900));Place(allies[1].Ship,new Vector3(-300,1,-900));
            ai.Ship.TakeDamage(360);Assert.AreEqual(EnemyState.Retreat,ai.State);Assert.AreSame(allies[0].Ship,ai.RetreatAlly);
            allies[0].Ship.TakeDamage(10000);yield return new WaitForSeconds(1.2f);Assert.AreSame(allies[1].Ship,ai.RetreatAlly);
            Place(ai.Ship,allies[1].transform.position+Vector3.right*100);yield return null;
            Assert.That(ai.Ship.Movement.TargetSpeed,Is.LessThan(ai.profile.retreatSpeed));Assert.IsFalse(ai.CanFire);
            allies[1].Ship.TakeDamage(10000);ai.Ship.Health.Restore();yield return new WaitForSeconds(1.2f);
            Assert.IsNull(ai.RetreatAlly);Assert.AreEqual(EnemyState.Retreat,ai.State);
        }
        [UnityTest]public IEnumerator PatrolCompletesConfiguredLoopWithoutPlayer()
        {
            var ai=enemies.First(e=>e.Ship is Destroyer);foreach(var e in enemies)if(e!=ai)e.gameObject.SetActive(false);player.gameObject.SetActive(false);
            Place(ai.Ship,new Vector3(800,1,-1000));
            var root=new GameObject("Patrol loop test");
            Vector3[] positions={new Vector3(800,0,-650),new Vector3(1100,0,-650),new Vector3(1100,0,-1000),new Vector3(800,0,-1000)};
            ai.waypoints=positions.Select((v,i)=>{var t=new GameObject("P"+i).transform;t.SetParent(root.transform);t.position=v;return t;}).ToArray();
            ai.enabled=false;ai.enabled=true;int visited=0;float until=Time.time+180;
            while(Time.time<until && visited<4)
            {
                if(EnemyAILogic.Distance(ai.transform.position,positions[visited])<ai.profile.waypointRadius+2)visited++;
                yield return new WaitForFixedUpdate();
            }
            Assert.AreEqual(EnemyState.Patrol,ai.State);Assert.AreEqual(4,visited,"All four waypoints must be reached without teleporting");Object.Destroy(root);
        }
        [UnityTest]public IEnumerator SavedThreeShipLayoutKeepsNavigating()
        {
            var previous=enemies.Select(e=>e.transform.position).ToArray();var travelled=new float[enemies.Length];
            var stationary=new float[enemies.Length];var longestStop=new float[enemies.Length];float until=Time.time+120;
            while(Time.time<until)
            {
                yield return new WaitForFixedUpdate();
                for(int i=0;i<enemies.Length;i++)
                {
                    var ai=enemies[i];float step=EnemyAILogic.Distance(previous[i],ai.transform.position);travelled[i]+=step;previous[i]=ai.transform.position;
                    stationary[i]=step<.004f?stationary[i]+Time.fixedDeltaTime:0;longestStop[i]=Mathf.Max(longestStop[i],stationary[i]);
                    Assert.Less(Mathf.Abs(ai.transform.position.x),1280);Assert.Less(Mathf.Abs(ai.transform.position.z),1280);
                }
            }
            for(int i=0;i<enemies.Length;i++)
            {
                Debug.Log(enemies[i].name+" travelled="+travelled[i]+" longest stop="+longestStop[i]);
                Assert.Greater(travelled[i],150,enemies[i].name+" must navigate the actual saved layout");
                Assert.Less(longestStop[i],30,enemies[i].name+" cannot remain stalled");
            }
        }
        [UnityTest]public IEnumerator PhysicalPatrolAvoidanceAndBoundary()
        {
            var ai=enemies.First(e=>e.Ship is Destroyer);foreach(var e in enemies)if(e!=ai)e.gameObject.SetActive(false);
            player.gameObject.SetActive(false);
            var island=Object.FindObjectsByType<GrandNavigation.Gameplay.Levels.LevelIsland>(FindObjectsSortMode.None).First();var box=island.solid.GetComponent<Collider>();
            var b=box.bounds;Place(ai.Ship,new Vector3(b.center.x,1,b.min.z-250));
            var waypoint=new GameObject("Navigation test goal");waypoint.transform.position=new Vector3(b.center.x,0,b.max.z+350);ai.waypoints=new[]{waypoint.transform};
            ai.Ship.Movement.SetTargetSpeed(25);bool avoiding=false;Vector3 start=ai.transform.position;
            float until=Time.time+35;
            while(Time.time<until)
            {
                avoiding|=ai.Navigation.Avoiding;Assert.Less(Mathf.Abs(ai.transform.position.x),1280);Assert.Less(Mathf.Abs(ai.transform.position.z),1280);
                if(Physics.ComputePenetration(ai.GetComponent<Collider>(),ai.transform.position,ai.transform.rotation,box,box.transform.position,box.transform.rotation,out _,out float depth))Assert.Less(depth,1,"Island penetration");
                yield return new WaitForFixedUpdate();
            }
            Assert.IsTrue(avoiding);Assert.Greater(EnemyAILogic.Distance(start,ai.transform.position),30,"NPC must physically navigate");
            Place(ai.Ship,new Vector3(0,1,1160));waypoint.transform.position=new Vector3(0,0,1500);until=Time.time+20;
            while(Time.time<until){Assert.Less(ai.transform.position.z,1270);yield return new WaitForFixedUpdate();}
            Assert.IsTrue(ai.Navigation.Avoiding);
            Place(ai.Ship,new Vector3(b.center.x,1,b.max.z+30));waypoint.transform.position=ai.transform.position+Vector3.forward*400;
            start=ai.transform.position;yield return new WaitForSeconds(15);
            Assert.Greater(EnemyAILogic.Distance(start,ai.transform.position),30,"Must escape an initial safety-margin overlap");Object.Destroy(waypoint);
        }
    }
}
