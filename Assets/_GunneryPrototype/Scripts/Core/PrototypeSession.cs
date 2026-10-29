using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace GrandNavigation.Gunnery
{
    [DefaultExecutionOrder(10)]
    public class PrototypeSession : MonoBehaviour
    {
        public TargetSelectionController selection;
        public LeadPredictionController prediction;
        public ManualAimController aim;
        public PrototypeCameraController cameraRig;
        public TargetMotion target;
        public TargetMotion[] targets;
        public TargetMotion ActiveTarget => selection.HasTarget ? selection.LockedTarget.GetComponent<TargetMotion>() : target;
        public event Action FireRequested; public event Action ResetRequested;
        public string Notice { get; private set; } = "Click orange ship to lock. Right mouse to aim.";
        void Awake() { Application.runInBackground=true; }
        void Update()
        {
            if(aim.IsAiming && !selection.HasTarget) ExitAim();
            var mouse=Mouse.current; var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                if(keyboard.escapeKey.wasPressedThisFrame) { ExitAim(); selection.Clear(); }
                if(keyboard.rKey.wasPressedThisFrame) ResetBattle(); if(keyboard.tabKey.wasPressedThisFrame) CycleTarget();
                if(keyboard.digit1Key.wasPressedThisFrame) ActiveTarget.SetMode(TargetMotion.MotionMode.Stationary,0);
                if(keyboard.digit2Key.wasPressedThisFrame) ActiveTarget.SetMode(TargetMotion.MotionMode.Lateral,8);
                if(keyboard.digit3Key.wasPressedThisFrame) ActiveTarget.SetMode(TargetMotion.MotionMode.Diagonal,12);
                if(keyboard.digit4Key.wasPressedThisFrame) ActiveTarget.SetMode(TargetMotion.MotionMode.Maneuvering,10);
            }
            if(mouse==null) return;
            if(mouse.rightButton.wasPressedThisFrame)
            {
                if(aim.IsAiming) ExitAim();
                else if(aim.Begin()) { Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; Notice="Mouse adjusts SEA IMPACT point. Left click: SALVO."; }
                else Notice=prediction.Status;
            }
            cameraRig.Zoom(mouse.scroll.ReadValue().y);
            if(aim.IsAiming)
            {
                // Ignore the initial cursor-lock delta.
                if(!mouse.rightButton.wasPressedThisFrame) aim.Adjust(mouse.delta.ReadValue());
                if(mouse.leftButton.wasPressedThisFrame) FireRequested?.Invoke();
            }
            else if(mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 pos=mouse.position.ReadValue();
                if(pos.y<Screen.height-220 || pos.x>380) selection.SelectRay(cameraRig.view.ScreenPointToRay(pos));
            }
        }
        public void CycleTarget()
        {
            if(targets==null || targets.Length==0) return;
            ExitAim();
            int current=Array.IndexOf(targets,selection.HasTarget?ActiveTarget:null);
            for(int step=1;step<=targets.Length;step++)
            {
                var candidate=targets[(current+step)%targets.Length];
                if(candidate && candidate.gameObject.activeInHierarchy && candidate.GetComponent<Health>().IsAlive)
                { selection.Select(candidate.GetComponent<Health>()); return; }
            }
            selection.Clear();
        }
        public void ExitAim() { aim.End(); Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        public void ResetBattle()
        {
            ExitAim(); selection.Clear(); aim.ResetAim(); prediction.ClearSamples(); if(targets!=null && targets.Length>0) { foreach(var ship in targets) if(ship && ship.gameObject.activeInHierarchy) ship.ResetTarget(); } else target.ResetTarget();
            foreach(var shell in FindObjectsByType<ProjectileController>(FindObjectsSortMode.None)) Destroy(shell.gameObject);
            ProjectileController.ResetStatistics(); ResetRequested?.Invoke(); Notice="Target reset. Click ship to lock.";
        }
        void OnDisable() { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
    }
}
