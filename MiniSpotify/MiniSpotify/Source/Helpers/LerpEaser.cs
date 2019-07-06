//Easings taken from https://easings.net/en

using System;

namespace MiniSpotify.HelperScripts
{
    public static class LerpEaser
    {
        public enum EaseType
        {
            Linear,
            SmoothStep,
            SmootherStep,
            EaseIn,
            EaseOut,
            EaseInQuad,
            EaseOutQuad,
            EaseInOutQuad,
            EaseInCubic,
            EaseOutCubic,
            ElasticIn,
            EaseOutBounce,
            EaseOutElastic
        }

        public static float GetLerpT(EaseType a_easing, float a_currentLerpTime, float a_duration)
        {
            float t = a_currentLerpTime;
            
            switch(a_easing)
            {
                case EaseType.Linear:

                    t = a_currentLerpTime / a_duration;

                    break;
                case EaseType.SmoothStep:

                    t = a_currentLerpTime / a_duration;
                    t = t * t * (3f - 2f * t);

                    break;

                case EaseType.SmootherStep:

                    t = a_currentLerpTime / a_duration;
                    t = t * t * t * (t * (6f * t - 15f) + 10f);

                    break;

                case EaseType.EaseIn:

                    t = 1f - (float)(Math.Cos(t * Math.PI * 0.5f));

                    break;

                case EaseType.EaseOut:
                    t = a_currentLerpTime / a_duration;
                    t = (float)Math.Sin(t * Math.PI * 0.5f);

                    break;

                case EaseType.EaseInQuad:

                    t = a_currentLerpTime / a_duration;
                    t = t * t;

                    break;
                case EaseType.EaseOutQuad:

                    t = a_currentLerpTime / a_duration;
                    t = t * (2 - t);

                    break;

                case EaseType.EaseInOutQuad:

                    t = a_currentLerpTime / a_duration;
                    t = t < .5 ? 2 * t * t : -1 + (4 - 2 * t) * t;

                    break;

                case EaseType.EaseInCubic:

                    t = a_currentLerpTime / a_duration;
                    t = t * t * t;

                    break;

                case EaseType.EaseOutCubic:

                    t = a_currentLerpTime / a_duration;
                    t = (--t) * t * t + 1;
                    break;

                case EaseType.EaseOutElastic:
                    t = .04f * t / (--t) * (float)Math.Sin(25 * t);

                    break;

                case EaseType.ElasticIn:

                    float b = a_currentLerpTime;
                    float c = a_duration - a_currentLerpTime;
                    float d = a_duration;

                    t = a_currentLerpTime / a_duration;

                    if (t / d == 1)
                    {
                        t = b + c;
                        break;
                    }

                    float p = d * 0.3f;
                    float s = p / 4;

                    if( t < 1)
                    {
                        float postFix = c * (float)Math.Pow(2, 10 * (t - 1));
                        t = (-0.5f * (postFix * (float)Math.Sin((t * d - s) * (2 * Math.PI) / p)) + b);
                        break;
                    }
                    else
                    {
                        float postFix = c * (float)Math.Pow(2, -10 * (t - 1));
                        t = postFix * (float)Math.Sin((t * d - s) * (2 * Math.PI) / p) * 0.5f + c + b;
                    }

                    break;
                case EaseType.EaseOutBounce:
                    if ((t /= 1) < (1 / 2.75f))
                    {
                        t = 1 * (7.5625f * t * t);
                    }
                    else if (t < (2 / 2.75f))
                    {
                        t = 1 * (7.5625f * (t -= (1.5f / 2.75f)) * t + .75f);
                    }
                    else if (t < (2.5 / 2.75))
                    {
                        t = 1 * (7.5625f * (t -= (2.25f / 2.75f)) * t + .9375f);
                    }
                    else
                    {
                        t= 1 * (7.5625f * (t -= (2.625f / 2.75f)) * t + .984375f);
                    }

                    break;
            }

            //if (t > 1 && a_easing != EaseType.ElasticIn)
            //    t = 1;
            //else if (t < 0 && a_easing != EaseType.ElasticIn)
            //    t = 0;

            return t;
        }
    }
}