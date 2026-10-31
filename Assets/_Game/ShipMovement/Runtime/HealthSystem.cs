using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class HealthSystem : MonoBehaviour
    {
        [Min(1)] public int maximumHealth=100;
        public int CurrentHealth {get;private set;}
        public bool IsAlive=>CurrentHealth>0;
        public event System.Action Died;
        void Awake()=>Restore();
        public void Restore()=>CurrentHealth=Mathf.Max(1,maximumHealth);
        public void TakeDamage(int damage)
        {
            if(damage<=0||!IsAlive)return;
            CurrentHealth=(int)System.Math.Max(0L,(long)CurrentHealth-damage);
            if(CurrentHealth==0)Died?.Invoke();
        }
    }
}
