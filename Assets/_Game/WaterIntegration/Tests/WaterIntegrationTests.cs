using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using GrandNavigation.Gameplay.AI;
using Object=UnityEngine.Object;

namespace GrandNavigation.Gameplay.Water.Tests
{
    public class WaterIntegrationTests
    {
        WaterSceneContext water;PlayerShip player;Camera cam;RenderTexture batchTarget;
        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.IsTrue(SystemInfo.supportsComputeShaders,"Run these tests with graphics enabled");
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/WaterIntegrationTest.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null;water=Object.FindFirstObjectByType<WaterSceneContext>();player=Object.FindFirstObjectByType<PlayerShip>();cam=water.view;
            foreach(var ai in Object.FindObjectsByType<EnemyShipAI>(FindObjectsSortMode.None))ai.enabled=false;
            Object.FindFirstObjectByType<PlayerShipInput>().enabled=false;Object.FindFirstObjectByType<PlayerCombatInput>().enabled=false;Object.FindFirstObjectByType<CombatAimController>().enabled=false;
            Object.FindFirstObjectByType<ShipFollowCamera>().enabled=false;Object.FindFirstObjectByType<CombatCamera>().enabled=false;Object.FindFirstObjectByType<CombatHUD>().enabled=false;Object.FindFirstObjectByType<EnemyAIDebug>().showHUD=false;
            foreach(var ship in Object.FindObjectsByType<Ship>(FindObjectsSortMode.None))ship.Movement.SetTargetSpeed(0);
            cam.transform.position=player.transform.position+new Vector3(23,12,25);cam.transform.LookAt(player.transform.position+Vector3.up*2);cam.fieldOfView=48;
            if(Application.isBatchMode)
            {
                batchTarget=new RenderTexture(640,360,24);cam.targetTexture=batchTarget;
                cam.gameObject.AddComponent<BatchWaterCamera>().view=cam;
            }
            yield return new WaitForSeconds(1);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(cam){var driver=cam.GetComponent<BatchWaterCamera>();if(driver)Object.Destroy(driver);cam.targetTexture=null;}
            if(batchTarget){batchTarget.Release();Object.Destroy(batchTarget);}yield return null;
        }
        static IEnumerator Read(RenderTexture texture,Action<Vector4[]> complete)
        {
            var request=AsyncGPUReadback.Request(texture,0,TextureFormat.RGBAFloat);yield return new WaitUntil(()=>request.done);
            Assert.IsFalse(request.hasError);complete(request.GetData<Vector4>().ToArray());
        }
        static float HeightAt(Vector4[] values,Vector3 point,Vector4 domain,int size)
        {int x=Mathf.Clamp(Mathf.FloorToInt((point.x-domain.x)/domain.z*size),0,size-1),z=Mathf.Clamp(Mathf.FloorToInt((point.z-domain.y)/domain.w*size),0,size-1);return values[z*size+x].x;}
        void Capture(string name,Vector3 position,Vector3 target)
        {
            cam.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            var oldTarget=cam.targetTexture;var rt=new RenderTexture(1600,900,24);cam.targetTexture=rt;cam.Render();cam.Render();var prior=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();Directory.CreateDirectory("Artifacts/WaterIntegration");File.WriteAllBytes("Artifacts/WaterIntegration/"+name+".png",texture.EncodeToPNG());
            RenderTexture.active=prior;cam.targetTexture=oldTarget;Object.Destroy(texture);rt.Release();Object.Destroy(rt);
        }
        [UnityTest] public IEnumerator SourceAppearanceFFTAndReflectionsRenderOnCurrentURP()
        {
            Assert.AreSame(water.pipeline,QualitySettings.renderPipeline);Assert.IsTrue(water.waterMaterial.shader.isSupported);
            Assert.IsFalse(ShaderUtil.GetShaderMessages(water.waterMaterial.shader).Any(m=>m.severity.ToString()=="Error"));
            Assert.IsTrue(water.fft.Ready);Assert.Greater(water.fft.FrameCount,1);
            Vector4[] first=null,second=null,normals=null;
            yield return Read(water.fft.Displacement,v=>first=v);yield return new WaitForSeconds(.3f);yield return Read(water.fft.Displacement,v=>second=v);yield return Read(water.fft.NormalFoam,v=>normals=v);
            float max=second.Max(v=>Mathf.Abs(v.y)),change=first.Zip(second,(a,b)=>Mathf.Abs(a.y-b.y)).Max();
            Assert.Greater(max,.001f);Assert.Less(max,5,"Waves must remain compatible with compact gameplay ship scale");Assert.Greater(change,.0001f);
            Assert.IsTrue(second.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)));Assert.IsTrue(normals.All(v=>Mathf.Abs(new Vector3(v.x,v.y,v.z).magnitude-1)<.02f));
            water.fft.useFFT=false;yield return null;Capture("01-source-appearance",cam.transform.position,player.transform.position+Vector3.up*2);
            water.fft.useFFT=true;yield return null;Capture("02-integrated-fft",cam.transform.position,player.transform.position+Vector3.up*2);
            Assert.That(Shader.GetGlobalFloat("_WaterSSREnabled"),Is.EqualTo(1));Assert.IsNotNull(Shader.GetGlobalTexture("_WaterSSRTexture"));
            var island=GameObject.Find("SandyCoastAndHighland").transform;cam.transform.position=island.position+new Vector3(120,35,-110);cam.transform.LookAt(island.position+Vector3.up*5);yield return null;
            Capture("03-shoreline",cam.transform.position,island.position+Vector3.up*5);
            File.WriteAllText("Artifacts/WaterIntegration/gpu-report.txt",$"GPU: {SystemInfo.graphicsDeviceName}\nUnity: {Application.unityVersion}\nFFT max height: {max:F5}\nFFT temporal change: {change:F5}\nSSR enabled; source shader compiled; normals finite and unit length.\n");
        }
        [UnityTest] public IEnumerator ShipWakeImpulseAndScrollingPreserveWorldSpaceHistory()
        {
            Assert.AreEqual(4,water.ShipCount);Assert.IsTrue(water.simulation.StateTexture);Assert.Greater(water.simulation.LastSimulatedDelta,0);
            Vector4[] contact=null;yield return Read(water.simulation.LatestContactTexture,v=>contact=v);Assert.Greater(contact.Max(v=>v.x),.005f,"Ship proxy captured by imported solver");
            Vector3 p=water.simulation.transform.position+new Vector3(20,0,15);Assert.IsTrue(water.simulation.AddImpulse(p,5,.18f));
            yield return null;yield return null;Vector4[] state=null;yield return Read(water.simulation.StateTexture,v=>state=v);
            Assert.Greater(HeightAt(state,p,water.simulation.Domain,water.simulation.resolution),.02f,"GPU solver receives impact pulse");
            int scroll=water.simulation.ScrollCount;var a=water.simulation.StateTexture;var b=water.simulation.StateScratchTexture;
            cam.transform.position+=Vector3.right*48;yield return null;yield return null;
            yield return Read(water.simulation.StateTexture,v=>state=v);Assert.Greater(water.simulation.ScrollCount,scroll);Assert.IsTrue(water.simulation.StateTexture==a||water.simulation.StateTexture==b,"Scrolling must reuse GPU resources");
            Assert.Greater(HeightAt(state,p,water.simulation.Domain,water.simulation.resolution),.005f,"World ripple survives window shift");
            cam.transform.position=player.transform.position+new Vector3(12,7,-18);cam.transform.LookAt(player.transform.position);player.Movement.SetTargetSpeed(8);yield return new WaitForSeconds(3);
            Assert.Greater(player.Movement.ActualSpeed,1);Assert.AreEqual(RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ,player.GetComponent<Rigidbody>().constraints);
            Capture("04-ship-interaction",cam.transform.position,player.transform.position);
        }
        [UnityTest] public IEnumerator RealSalvoTriggersWaterInteractionsWithoutChangingDamageOrReload()
        {
            var weapon=player.Weapon;Assert.AreEqual(4,weapon.turrets.Length);Assert.AreEqual(140,weapon.muzzleSpeed);Assert.AreEqual(1200,weapon.maximumRange);
            weapon.SetAim(player.transform.position+Vector3.right*120);for(int i=0;i<4;i++)foreach(var turret in weapon.turrets)turret.Track(60);
            int start=water.WaterImpactCount,spawned=Projectile.SpawnedCount;Assert.IsTrue(weapon.TryFire());Assert.AreEqual(8,Projectile.SpawnedCount-spawned);Assert.IsFalse(weapon.TryFire());
            yield return new WaitForSeconds(2);Assert.Greater(water.WaterImpactCount,start);Assert.Greater(water.AcceptedImpulses,0);
            Capture("05-shell-interaction",water.LastImpact+new Vector3(12,8,-14),water.LastImpact);
            var debug=Object.FindFirstObjectByType<EnemyAIDebug>();debug.ResetEnemy(0);yield return new WaitForSeconds(.6f);
            Assert.IsNotNull(debug.slots[0].instance.transform.Find("WaterContactProxy"));
        }
        [UnityTest] public IEnumerator LeavingWaterSceneRestoresOriginalPipelineAndHooks()
        {
            var custom=water.pipeline;
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/TropicalNavalBattle.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;
            Assert.AreNotSame(custom,QualitySettings.renderPipeline);Assert.IsNull(CombatEffects.WaterVisualPosition);Assert.AreEqual(0,Shader.GetGlobalFloat("_NavalFFTEnabled"));Assert.IsNull(Object.FindFirstObjectByType<WaterSceneContext>());
        }
    }
}
