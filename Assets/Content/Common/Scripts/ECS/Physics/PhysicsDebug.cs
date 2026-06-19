namespace LittlePhysics
{
    public static class PhysicsDebug
    {
        [System.Diagnostics.Conditional("LITTLE_PHYSICS_SAFE_MODE")]
        public static void SafeAssert(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.InvalidOperationException(message);
            }
        }
    }
}
