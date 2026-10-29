using UnityEngine;
namespace GrandNavigation.Gunnery
{
    [RequireComponent(typeof(AudioSource))]
    public class CombatAudio : MonoBehaviour
    {
        static CombatAudio instance;
        AudioSource source; AudioClip shot,hit,splash;
        void Awake()
        {
            instance=this; source=GetComponent<AudioSource>(); source.spatialBlend=0; source.volume=.3f;
            shot=Make("Cannon",.55f,55); hit=Make("Steel hit",.18f,230); splash=Make("Splash",.4f,110);
        }
        AudioClip Make(string name,float seconds,float frequency)
        {
            int count=(int)(22050*seconds); var data=new float[count]; var noise=new System.Random(41);
            for(int i=0;i<count;i++)
            {
                float t=i/22050f, envelope=Mathf.Exp(-t*8)*Mathf.Clamp01((seconds-t)*25);
                data[i]=((float)(noise.NextDouble()*2-1)*.65f+Mathf.Sin(t*frequency*2*Mathf.PI)*.35f)*envelope;
            }
            var clip=AudioClip.Create(name,count,1,22050,false); clip.SetData(data,0); return clip;
        }
        public static void PlayShot() { if(instance) instance.source.PlayOneShot(instance.shot); }
        public static void PlayImpact(bool water) { if(instance) instance.source.PlayOneShot(water?instance.splash:instance.hit,.55f); }
        void OnDestroy()
        {
            if(instance==this) instance=null;
            if(shot) Destroy(shot); if(hit) Destroy(hit); if(splash) Destroy(splash);
        }
    }
}