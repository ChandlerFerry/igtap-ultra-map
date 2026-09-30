using System;

namespace Box2D
{
    public static class TOI
    {
        public static void TimeOfImpact(out b2TOIOutput output, in b2TOIInput input)
        {
            output.t = input.tMax;
            output.state = TOIOutputState.Unknown;

            b2DistanceProxy proxyA = input.proxyA;
            b2DistanceProxy proxyB = input.proxyB;

            b2Sweep sweepA = input.sweepA;
            b2Sweep sweepB = input.sweepB;

            sweepA.Normalize();
            sweepB.Normalize();

            float tMax = input.tMax;

            float totalRadius = proxyA._radius + proxyB._radius;
            float target = MathF.Max(b2Settings.linearSlop, totalRadius - 3.0f * b2Settings.linearSlop);
            float tolerance = 0.25f * b2Settings.linearSlop;

            var t1 = 0.0f;
            var iter = 0;

            var cache = new b2SimplexCache();
            b2DistanceInput distanceInput;
            distanceInput.proxyA = input.proxyA;
            distanceInput.proxyB = input.proxyB;
            distanceInput.useRadii = false;

            b2SeparationFunction fcn = default;
            for (; ; )
            {
                sweepA.GetTransform(out b2Transform xfA, t1);
                sweepB.GetTransform(out b2Transform xfB, t1);

                distanceInput.transformA = xfA;
                distanceInput.transformB = xfB;
                b2Contact.Distance(out b2DistanceOutput distanceOutput, ref cache, in distanceInput);

                if (distanceOutput.distance <= 0.0f)
                {
                    output.state = TOIOutputState.Overlapped;
                    output.t = 0.0f;
                    break;
                }

                if (distanceOutput.distance < target + tolerance)
                {
                    output.state = TOIOutputState.Touching;
                    output.t = t1;
                    break;
                }

                fcn.Initialize(cache, proxyA, sweepA, proxyB, sweepB, t1);

                var done = false;
                float t2 = tMax;
                var pushBackIter = 0;
                for (; ; )
                {
                    float s2 = fcn.FindMinSeparation(out int indexA, out int indexB, t2);

                    if (s2 > target + tolerance)
                    {
                        output.state = TOIOutputState.Separated;
                        output.t = tMax;
                        done = true;
                        break;
                    }

                    if (s2 > target - tolerance)
                    {
                        t1 = t2;
                        break;
                    }

                    float s1 = fcn.Evaluate(indexA, indexB, t1);

                    if (s1 < target - tolerance)
                    {
                        output.state = TOIOutputState.Failed;
                        output.t = t1;
                        done = true;
                        break;
                    }

                    if (s1 <= target + tolerance)
                    {
                        output.state = TOIOutputState.Touching;
                        output.t = t1;
                        done = true;
                        break;
                    }

                    float a1 = t1, a2 = t2;
                    for (var rootIterCount = 0; rootIterCount < 50; ++rootIterCount)
                    {
                        float t;
                        if ((rootIterCount & 1) > 0)
                        {
                            t = a1 + (target - s1) * (a2 - a1) / (s2 - s1);
                        }
                        else
                        {
                            t = 0.5f * (a1 + a2);
                        }

                        float s = fcn.Evaluate(indexA, indexB, t);

                        if (MathF.Abs(s - target) < tolerance)
                        {
                            t2 = t;
                            break;
                        }

                        if (s > target)
                        {
                            a1 = t;
                            s1 = s;
                        }
                        else
                        {
                            a2 = t;
                            s2 = s;
                        }
                    }

                    ++pushBackIter;

                    if (pushBackIter == b2Settings.MaxPolygonVertices)
                    {
                        break;
                    }
                }

                ++iter;

                if (done)
                {
                    break;
                }

                if (iter == b2Settings.MaxTOIIterations)
                {
                    output.state = TOIOutputState.Failed;
                    output.t = t1;
                    break;
                }
            }
        }
    }
}