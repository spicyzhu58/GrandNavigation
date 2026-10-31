using NUnit.Framework;
using UnityEngine;
namespace GrandNavigation.Gameplay.Tests
{
    public class MovementMathTests
    {
        readonly MovementSettings s=new MovementSettings();
        [TestCase(100,25)][TestCase(-100,-8)]
        public void TargetIsBounded(float axis,float expected)
        {
            float target=0;
            for(int i=0;i<500;i++)target=ShipMovementMath.AdjustTarget(target,axis,.02f,s);
            Assert.That(target,Is.EqualTo(expected));
        }
        [Test] public void ReleaseAndOppositeInputsPreserveTarget()
        {
            Assert.That(ShipMovementMath.AdjustTarget(13,0,10,s),Is.EqualTo(13));
            Assert.That(ShipMovementMath.AdjustTarget(13,1-1,10,s),Is.EqualTo(13));
        }
        [Test] public void ReversalBrakesToZeroBeforeReverseAcceleration()
        {
            float speed=12;
            speed=ShipMovementMath.StepSpeed(speed,-8,1,s);Assert.That(speed,Is.EqualTo(6));
            speed=ShipMovementMath.StepSpeed(speed,-8,1,s);Assert.That(speed,Is.Zero);
            speed=ShipMovementMath.StepSpeed(speed,-8,1,s);Assert.That(speed,Is.EqualTo(-2.5));
            Assert.That(ShipMovementMath.StepSpeed(12,-8,100,s),Is.Zero);
        }
        [Test] public void AccelerationAndBrakingUseIndependentRates()
        {
            Assert.That(ShipMovementMath.StepSpeed(0,25,1,s),Is.EqualTo(4));
            Assert.That(ShipMovementMath.StepSpeed(20,10,1,s),Is.EqualTo(14));
            Assert.That(ShipMovementMath.StepSpeed(-6,-8,1,s),Is.EqualTo(-8));
            Assert.That(ShipMovementMath.StepSpeed(24,100,1,s),Is.EqualTo(25));
        }
        [Test] public void SteeringNeedsSpeedReversesAndHasLimitedAuthority()
        {
            Assert.That(ShipMovementMath.DesiredTurnRate(0,35,s),Is.Zero);
            Assert.That(ShipMovementMath.DesiredTurnRate(25,35,s),Is.EqualTo(16));
            Assert.That(ShipMovementMath.DesiredTurnRate(25,-35,s),Is.EqualTo(-16));
            float ahead=ShipMovementMath.DesiredTurnRate(8,35,s),astern=ShipMovementMath.DesiredTurnRate(-8,35,s);
            Assert.That(astern,Is.EqualTo(-ahead*.6f).Within(.001));
            Assert.That(ShipMovementMath.DesiredTurnRate(1,35,s),Is.LessThan(ahead));
        }
        class FutureCarrier : EnemyShip { }
        [Test] public void InheritanceCompositionDamageAndWeaponDependencyAreUsable()
        {
            Assert.That(typeof(PlayerShip).BaseType,Is.EqualTo(typeof(Ship)));
            Assert.That(typeof(EnemyShip).BaseType,Is.EqualTo(typeof(Ship)));
            Assert.That(typeof(Destroyer).BaseType,Is.EqualTo(typeof(EnemyShip)));
            Assert.That(typeof(Battleship).BaseType,Is.EqualTo(typeof(EnemyShip)));
            Assert.IsTrue(typeof(EnemyShip).IsAssignableFrom(typeof(FutureCarrier)));
            var go=new GameObject("OOP test");
            try
            {
                var player=go.AddComponent<PlayerShip>();
                // EditMode does not run runtime Awake; invoke it only for this component-level contract test.
                typeof(Ship).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(player,null);
                player.Health.Restore();
                ((IDamageable)player).TakeDamage(30);Assert.That(player.Health.CurrentHealth,Is.EqualTo(70));
                player.TakeDamage(-20);Assert.That(player.Health.CurrentHealth,Is.EqualTo(70));
                Assert.That(go.GetComponents<Ship>().Length,Is.EqualTo(1));
                var projectile=go.AddComponent<Projectile>();player.Weapon.projectile=projectile;
                Assert.IsFalse(player.FireWeapon());Assert.IsFalse(projectile.Activated); // An unconfigured weapon must not launch a live projectile.
                projectile.ResolveHit(player,10);Assert.That(player.Health.CurrentHealth,Is.EqualTo(60));
                Assert.That(Projectile.ComposeWorldVelocity(Vector3.forward*65,Vector3.right*8),Is.EqualTo(new Vector3(8,0,65)));
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
