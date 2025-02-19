using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System;

[Serializable]
[CreateAssetMenu(fileName = "ADAS", menuName = "Scriptable Objects/ADAS_MajorFeature", order = 1)]
[GUIColor("@$value.SetGUIColor($value)")]
public class ADAS_MajorFeature : ScriptableObject
{
    [Serializable]
    public abstract class ADAS_Settings { }

    public string Name;
    public string Description;
    [FoldoutGroup ("Base and Major Features")] public List<ADAS_BaseFeature> BaseFeaturesList;
    [FoldoutGroup("Base and Major Features")] public List<ADAS_MajorFeature> MajorFeaturesList;

    public Color SetGUIColor(ADAS_MajorFeature feature)
    {
        if (feature is IActivable activable)
        {
            if (activable.IsOn)
                return Color.green;
            else
                return Color.white;
        }

        if (feature is IActivableByInputAction activableByInputAction)
        {
            if (activableByInputAction.IsOn)
                return Color.green;
            else
                return Color.white;
        }

        return Color.white;

        //return feature ? feature is IActivable activable ? activable.IsOn ? Color.green : Color.white : Color.green : Color.white;
    }
}