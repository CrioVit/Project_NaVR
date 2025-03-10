// Perfect Culling (C) 2021 Patrick König
//

using UnityEngine;

namespace Koenigz.PerfectCulling
{
    public static class DefaultSamplingProvider
    {
        private static PerfectCullingExcludeVolume[] m_excludeVolumes;
        private static PerfectCullingAlwaysIncludeVolume[] m_alwaysIncludeVolumes;

        public enum Result
        {
            None,
            
            IncludeCell,
            ExcludeCell,
        }

        public static void InitializeSamplingProvider()
        {
            m_excludeVolumes = Object.FindObjectsOfType<PerfectCullingExcludeVolume>();
            m_alwaysIncludeVolumes = Object.FindObjectsOfType<PerfectCullingAlwaysIncludeVolume>();
        }
        
        public static Result IsSamplingPositionActive(PerfectCullingBakingBehaviour bakingBehaviour, Vector3 pos)
        {
            // m_alwaysIncludeVolumes takes over priority as it allows to pull cells back in.
            foreach (PerfectCullingAlwaysIncludeVolume alwaysIncludeVolume in m_alwaysIncludeVolumes)
            {
                if (alwaysIncludeVolume.IsPositionActive(bakingBehaviour, pos))
                {
                    return Result.IncludeCell;
                }
            }

            foreach (var bound in m_excludeVolumes)
            {
                if (bound.IsPositionActive(bakingBehaviour, pos))
                {
                    return Result.ExcludeCell;
                }
            }

            return Result.None;
        }
    }
}