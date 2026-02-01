using UnityEngine;

public static class GameFeel
{
    public static class Impact
    {
        public static void Light()
        {
            CameraShake.Shake(0.1f, 0.15f);
            HitStop.Stop(0.06f);
        }
        
        public static void Medium()
        {
            CameraShake.Shake(0.2f, 0.3f);
            HitStop.Stop(0.1f);
        }
        
        public static void Heavy()
        {
            CameraShake.Shake(0.3f, 0.5f);
            HitStop.Stop(0.15f);
        }
        
        public static void Massive()
        {
            CameraShake.Shake(0.5f, 0.8f);
            HitStop.Stop(0.25f);
        }
    }
    
    public static class Player
    {
        public static void TakeDamage()
        {
            CameraShake.Shake(0.2f, 0.3f);
            HitStop.Stop(0.15f);
        }
        
        public static void Death()
        {
            CameraShake.Shake(0.4f, 0.6f);
            HitStop.Stop(0.2f);
        }
        
        public static void DashHit()
        {
            CameraShake.Shake(0.12f, 0.2f);
            HitStop.Stop(0.1f);
        }
    }
    
    public static class Boss
    {
        public static void TakeDamage()
        {
            HitStop.Stop(0.1f);
        }
        
        public static void PhaseChange()
        {
            CameraShake.Shake(0.3f, 0.4f);
            HitStop.Stop(0.18f);
        }
        
        public static void Death()
        {
            CameraShake.Shake(0.6f, 0.9f);
            HitStop.Stop(0.3f);
        }
        
        public static void GroundSlam()
        {
            CameraShake.Shake(0.25f, 0.45f);
            HitStop.Stop(0.12f);
        }
        
        public static void WallImpact()
        {
            CameraShake.Shake(0.15f, 0.25f);
            HitStop.Stop(0.08f);
        }
    }
}
