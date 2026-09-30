using System.Numerics;

namespace Box2D
{
    public class b2WorldManifold
    {
        public float[] separations = new float[b2Settings.MaxManifoldPoints];

        public b2Vec2 normal;

        public b2Vec2[] points = new b2Vec2[b2Settings.MaxManifoldPoints];

        public void Initialize(
            b2Manifold manifold,
            b2Transform xfA,
            float radiusA,
            b2Transform xfB,
            float radiusB)
        {
            if (manifold.pointCount == 0)
            {
                return;
            }

            switch (manifold.type)
            {
                case b2ManifoldType.Circles:
                    {
                        normal = new b2Vec2(1.0f, 0.0f);
                        b2Vec2 pointA = Math.Mul(xfA, manifold.localPoint);
                        b2Vec2 pointB = Math.Mul(xfB, manifold.points[0].localPoint);
                        if (b2Vec2.DistanceSquared(pointA, pointB) > b2Settings.FLT_EPSILON_SQUARED)
                        {
                            normal = b2Vec2.Normalize(pointB - pointA);
                        }

                        b2Vec2 cA = pointA + radiusA * normal;
                        b2Vec2 cB = pointB - radiusB * normal;
                        points[0] = 0.5f * (cA + cB);
                        separations[0] = b2Vec2.Dot(cB - cA, normal);
                    }
                    break;

                case b2ManifoldType.FaceA:
                    {
                        normal = b2Vec2.Transform(manifold.localNormal, xfA.q);
                        b2Vec2 planePoint = Math.Mul(xfA, manifold.localPoint);

                        for (var i = 0; i < manifold.pointCount; ++i)
                        {
                            b2Vec2 clipPoint = Math.Mul(xfB, manifold.points[i].localPoint);
                            b2Vec2 cA = clipPoint + (radiusA - b2Vec2.Dot(clipPoint - planePoint, normal)) * normal;
                            b2Vec2 cB = clipPoint - radiusB * normal;
                            points[i] = 0.5f * (cA + cB);
                            separations[i] = b2Vec2.Dot(cB - cA, normal);
                        }
                    }
                    break;

                case b2ManifoldType.FaceB:
                    {
                        normal = b2Vec2.Transform(manifold.localNormal, xfB.q);
                        b2Vec2 planePoint = Math.Mul(xfB, manifold.localPoint);

                        for (var i = 0; i < manifold.pointCount; ++i)
                        {
                            b2Vec2 clipPoint = Math.Mul(xfA, manifold.points[i].localPoint);
                            b2Vec2 cB = clipPoint + (radiusB - b2Vec2.Dot(clipPoint - planePoint, normal)) * normal;
                            b2Vec2 cA = clipPoint - radiusA * normal;

                            points[i] = 0.5f * (cA + cB);
                            separations[i] = b2Vec2.Dot(cA - cB, normal);
                        }

                        normal = -normal;
                    }
                    break;
            }
        }
    }
}