using System;
using UnityEngine;

namespace GrandNavigation.Gunnery
{
    /// <summary>Fixed-speed, constant-gravity, low-angle solution. No flight-time tuning.</summary>
    public static class BallisticSolver
    {
        public static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        public static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);

        public static bool TrySolve(Vector3 origin, Vector3 point, float speed, float gravity,
            out Vector3 velocity, out float flightTime)
        {
            velocity = Vector3.zero; flightTime = 0;
            if (!Finite(origin) || !Finite(point) || !Finite(speed) || !Finite(gravity) ||
                speed <= 0 || gravity <= 0) return false;
            Vector3 delta = point - origin;
            double range = Math.Sqrt((double)delta.x * delta.x + (double)delta.z * delta.z);
            if (range < 0.001) return false;
            double v2 = (double)speed * speed;
            double disc = v2 * v2 - gravity * (gravity * range * range + 2 * delta.y * v2);
            if (disc < 0) return false;
            // Rationalized low-angle tangent avoids cancellation near zero elevation.
            double tan = (gravity * range * range + 2 * delta.y * v2) /
                         (range * (v2 + Math.Sqrt(disc)));
            double cos = 1 / Math.Sqrt(1 + tan * tan);
            double t = range / (speed * cos);
            if (double.IsNaN(t) || double.IsInfinity(t) || t <= 0 || t > 120) return false;
            Vector3 horizontal = new Vector3(delta.x, 0, delta.z).normalized;
            velocity = horizontal * (float)(speed * cos) + Vector3.up * (float)(speed * cos * tan);
            flightTime = (float)t;
            return Finite(velocity);
        }

        public static Vector3 Position(Vector3 origin, Vector3 velocity, float gravity, float t) =>
            origin + velocity * t + Vector3.down * (0.5f * gravity * t * t);

        public static bool TryIntercept(Vector3 origin, Vector3 target, Vector3 targetVelocity,
            float speed, float gravity, float maxRange, out Vector3 point, out float time)
        {
            point = new Vector3(target.x, 0, target.z); time = 0;
            if (!Finite(targetVelocity) || !Finite(maxRange) || maxRange <= 0) return false;
            for (int i = 0; i < 32; i++)
            {
                if (!TrySolve(origin, point, speed, gravity, out _, out float next)) return false;
                Vector3 nextPoint = target + targetVelocity * next; nextPoint.y = 0;
                float error = Vector3.Distance(point, nextPoint);
                point = nextPoint; time = next;
                if (error < 0.002f)
                {
                    Vector3 flat = point - origin; flat.y = 0;
                    return flat.magnitude <= maxRange;
                }
            }
            return false;
        }
    }
}