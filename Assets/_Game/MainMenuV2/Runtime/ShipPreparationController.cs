using UnityEngine;
using UnityEngine.Events;
namespace GrandNavigation.MainMenuV2
{
    public sealed class ShipPreparationController : MonoBehaviour
    {
        public Transform shipModelSlot;
        public UnityEvent preparationOpened=new UnityEvent();
        public UnityEvent preparationClosed=new UnityEvent();
        public void SetActive(bool active)
        {
            if(active) preparationOpened.Invoke(); else preparationClosed.Invoke();
        }
    }
}
