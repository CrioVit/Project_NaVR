// Perfect Culling (C) 2021 Patrick König
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Koenigz.PerfectCulling
{
    /// <summary>
    /// This is the base class that provides an easy interface for baking.
    /// Hopefully the built-in baking scripts are sufficient but if you need something else you can inherit it and roll your own.
    /// </summary>
    public abstract class PerfectCullingBakingBehaviour : MonoBehaviour
    {
        public enum EOutOfBoundsBehaviour
        {
            ClampToNearestCell,
            Cull,
            IgnoreDoNothing,
        }
        
        [Tooltip("Try to find a non-empty cell if we hit an empty one?")]
        public bool searchForNonEmptyCells = false;
        
        [Tooltip("What should happen if we encounter an empty cell? Cull everything or make everything visible?")]
        public EEmptyCellCullBehaviour emptyCellCullBehaviour = EEmptyCellCullBehaviour.CullEverything;
        
        [Tooltip("Should this volume be culled if the camera is not inside it or should the camera position be clamped to the nearest cell?")]
        public EOutOfBoundsBehaviour outOfBoundsBehaviour = EOutOfBoundsBehaviour.ClampToNearestCell;

        private IActiveSamplingProvider[] m_activeSamplingProviders;

        public IActiveSamplingProvider[] QuerySamplingProviders()
        {
            return GetComponentsInChildren<IActiveSamplingProvider>();
        }
        
        public void InitializeAllSamplingProviders()
        {
            m_activeSamplingProviders = GetComponentsInChildren<IActiveSamplingProvider>();
            
            DefaultSamplingProvider.InitializeSamplingProvider();
            
            foreach (var provider in m_activeSamplingProviders)
            {
                provider.InitializeSamplingProvider();
            }
        }

        public bool SamplingProvidersIsPositionActive(Vector3 pos)
        {
            switch (DefaultSamplingProvider.IsSamplingPositionActive(this, pos))
            {
                case DefaultSamplingProvider.Result.IncludeCell:
                    return true;
                
                case DefaultSamplingProvider.Result.ExcludeCell:
                    return false;
            }
            
            foreach (IActiveSamplingProvider provider in m_activeSamplingProviders)
            {
                if (!provider.IsSamplingPositionActive(this, pos))
                {
                    return false;
                }
            }

            return true;
        }
        
        [SerializeField] public PerfectCullingBakeGroup[] bakeGroups = System.Array.Empty<PerfectCullingBakeGroup>(); // Important to initialize or AddRange will fail

        [SerializeField] public List<Renderer> additionalOccluders = new List<Renderer>();
        
        public virtual PerfectCullingBakeData BakeData { get; } = null;

        [System.NonSerialized] public int TotalVertexCount = 0;

        private bool[] m_renderersState;
        
        public virtual void Start()
        {
            TotalVertexCount = 0;
            m_renderersState = new bool[bakeGroups.Length];
            
            foreach (PerfectCullingBakeGroup group in bakeGroups)
            {
                group.Init();
                
#if UNITY_EDITOR
                TotalVertexCount += group.vertexCount;
#endif
            }
        }


        public void QueueToggleAllRenderers(bool state)
        {
            for (var index = 0; index < bakeGroups.Length; index++)
            {
                m_renderersState[index] = state;
            }
        }

        /// <summary>
        /// Queue up renderer state change. This will not take effect until ExecuteQueue was called.
        /// </summary>
        public void QueueToggleRenderer(int index, bool state, out PerfectCullingBakeGroup modifiedBakeGroup)
        {
            m_renderersState[index] = state;
            modifiedBakeGroup = bakeGroups[index];
        }

        /// <summary>
        /// Applies renderers state changes.
        /// </summary>
        public void ExecuteQueue(bool forceNullCheck = false)
        {
            for (var index = 0; index < bakeGroups.Length; index++)
            {
                PerfectCullingBakeGroup r = bakeGroups[index];
                r.Toggle(m_renderersState[index], forceNullCheck);
            }
        }

        public virtual void SetBakeData(PerfectCullingBakeData bakeData) => throw new System.NotImplementedException();
        
        public virtual List<Vector3> GetSamplingPositions(Space space = Space.Self) => throw new System.NotImplementedException();

        public virtual void GetIndicesForWorldPos(Vector3 worldPos, List<ushort> indices) => throw new System.NotImplementedException();
        public virtual int GetIndexForWorldPos(Vector3 worldPos, out bool isOutOfBounds) => throw new System.NotImplementedException();

        public virtual void GetIndicesForIndex(int index, List<ushort> indices) =>
            BakeData.SampleAtIndex(index, indices);

        public virtual bool PreBake() => throw new System.NotImplementedException();
        public virtual void PostBake() => throw new System.NotImplementedException();
        
        public virtual int GetBakeHash() => throw new System.NotImplementedException();

        public virtual void CullAdditionalOccluders(ref HashSet<Renderer> additionalOccluders) =>
            throw new System.NotImplementedException();
    }
}