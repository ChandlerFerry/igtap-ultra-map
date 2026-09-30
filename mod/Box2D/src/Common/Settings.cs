using System;

namespace Box2D
{
    public class b2Settings
    {
        public static int MaxTOIIterations = 20;

        public static float aabbMultiplier = 4.0f;
        public static float aabbExtension = 0.1f;
        public static int maxTOIContacts = 32;
        public static int maxSubSteps = 8;

        public static float FLT_EPSILON = 1.192092896e-07f;
        public static float FLT_EPSILON_SQUARED => FLT_EPSILON * FLT_EPSILON;

        public static float Pib2 = 3.14159265359f;
        public static float Pi = 3.14159265359f;
        public static float Pi2 = 3.14159265359f;
        public static float Tau = 2f * Pi;

        public static int MaxManifoldPoints = 2;
        public static int MaxPolygonVertices = 8;

        public static float linearSlop = 0.005f;

        public static float angularSlop = 2.0f / 180.0f * Pi;

        public static float polygonRadius = 2.0f * linearSlop;

        public static int MaxTOIContactsPerIsland = 32;

        public static int MaxTOIJointsPerIsland = 32;

        public static float velocityThreshold = 1.0f;

        public static float maxLinearCorrection = 0.2f;

        public static float maxAngularCorrection = 8.0f / 180.0f * Pi;

        public static float maxTranslation = 2.0f;

        public static float maxTranslationSquared => maxTranslation * maxTranslation;

        public static float maxRotation = 0.5f * Pi;

        public static float maxRotationSquared => maxRotation * maxRotation;

        public static float baumgarte = 0.2f;

        public static float timeToSleep = 0.5f;

        public static float linearSleepTolerance = 0.01f;

        public static float angularSleepTolerance = 2.0f / 180.0f * Pi;

        public static bool BlockSolve = true;
        public static bool useLegacyGravityIntegration = true;
        public static float toiBaumgarte = 0.75f;

        public static float FORCE_SCALE(float x) => x;

        public static float FORCE_INV_SCALE(float x) => x;

        public static float MixFriction(float friction1, float friction2) =>
            MathF.Sqrt(friction1 * friction2);

        public static float MixRestitution(float restitution1, float restitution2) =>
            restitution1 > restitution2 ? restitution1 : restitution2;
    }
}