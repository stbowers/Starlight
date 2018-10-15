using System;

namespace StarlightEngine.Math
{
    /// <summary>
    /// Represents a ray in R^3
    /// </summary>
    public class Ray
    {
        // a line in r3 is described as: <r>(t) = <r0> + t<v>
        // a ray is then defined on that line segment for all t >= 0
        FVec3 m_origin;
        FVec3 m_direction;
        float m_tmin = float.NegativeInfinity;
        float m_tmax = float.PositiveInfinity;

        // pre-calculated components of 1/(m_direction), used to speed up clipping calculations
        // may be +- infinity in the case of divide by zero
        float m_invDirectionX;
        float m_invDirectionY;
        float m_invDirectionZ;

        public Ray(FVec3 origin, FVec3 direction)
        {
            m_origin = origin;
            m_direction = direction;
            m_direction.Normalize();

            m_invDirectionX = 1 / m_direction.X();
            m_invDirectionY = 1 / m_direction.Y();
            m_invDirectionZ = 1 / m_direction.Z();

            if (float.IsNaN(m_invDirectionX)){
                m_invDirectionX = float.PositiveInfinity;
            }
            if (float.IsNaN(m_invDirectionY)){
                m_invDirectionY = float.PositiveInfinity;
            }
            if (float.IsNaN(m_invDirectionZ)){
                m_invDirectionZ = float.PositiveInfinity;
            }
        }

        /// <summary>
        /// returns true if a line segment still exists after being clipped
        /// </summary>
        public bool IsSegment()
        {
            return m_tmin < m_tmax;
        }

        /// <summary>
        /// resets the min and max t values after being clipped
        /// </summary>
        public void ResetSegment()
        {
            m_tmin = 0.0f;
            m_tmax = float.PositiveInfinity;
        }

        public void ClipX(float x0, float x1)
        {
            // calculate intersections
            float t0 = (x0 - m_origin.X()) * m_invDirectionX;
            float t1 = (x1 - m_origin.X()) * m_invDirectionX;

            // calculate new tmin and tmax (line segment between x0 and x1)
            m_tmin = System.Math.Max(m_tmin, System.Math.Min(t0, t1));
            m_tmax = System.Math.Min(m_tmax, System.Math.Max(t0, t1));
        }

        public void ClipY(float y0, float y1)
        {
            // calculate intersections
            float t0 = (y0 - m_origin.Y()) * m_invDirectionY;
            float t1 = (y1 - m_origin.Y()) * m_invDirectionY;

            // calculate new tmin and tmax (line segment between y0 and y1)
            m_tmin = System.Math.Max(m_tmin, System.Math.Min(t0, t1));
            m_tmax = System.Math.Min(m_tmax, System.Math.Max(t0, t1));
        }

        public void ClipZ(float z0, float z1)
        {
            // calculate intersections
            float t0 = (z0 - m_origin.Z()) * m_invDirectionZ;
            float t1 = (z1 - m_origin.Z()) * m_invDirectionZ;

            // calculate new tmin and tmax (line segment between z0 and z1)
            m_tmin = System.Math.Max(m_tmin, System.Math.Min(t0, t1));
            m_tmax = System.Math.Min(m_tmax, System.Math.Max(t0, t1));
        }

        public static Ray operator *(FMat4 matrix, Ray ray)
        {
            // transform origin
            FVec3 newOrigin = (matrix * new FVec4(ray.m_origin.X(), ray.m_origin.Y(), ray.m_origin.Z(), 1.0f)).XYZ();

            // transform direction (no w component, so it doesn't get shifted)
            FVec3 newDirection = (matrix * new FVec4(ray.m_direction.X(), ray.m_direction.Y(), ray.m_direction.Z(), 0.0f)).XYZ();

            return new Ray(newOrigin, newDirection);
        }

        public FVec3 Origin
        {
            get
            {
                return m_origin;
            }
        }

        public FVec3 Direction
        {
            get
            {
                return m_direction;
            }
        }

        public float TMin
        {
            get
            {
                return m_tmin;
            }
        }

        public float TMax
        {
            get
            {
                return m_tmax;
            }
        }
    }
}