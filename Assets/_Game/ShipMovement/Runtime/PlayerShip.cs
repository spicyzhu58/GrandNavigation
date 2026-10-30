namespace GrandNavigation.Gameplay
{
    public sealed class PlayerShip : Ship
    {
        public bool FireWeapon() => Weapon && Weapon.TryFire();
    }
}
