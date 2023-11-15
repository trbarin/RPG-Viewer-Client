using System;
using UnityEngine;

namespace RPG
{
    [Serializable]
    public struct LightingSettings
    {
        public bool enabled;
        public bool globalLighting;
        public Color color;

        public LightingSettings(bool _enabled, bool _globalLighting, Color _color)
        {
            enabled = _enabled;
            globalLighting = _globalLighting;
            color = _color;
        }
    }

    [Serializable]
    public struct LightData
    {
        public string id;
        public Vector2 position;
        public float rotation;
        public bool enabled;

        public LightData(string _id, Vector2 _position, float _rotation, bool _enabled)
        {
            id = _id;
            position = _position;
            rotation = _rotation;
            enabled = _enabled;
        }
    }

    [Serializable]
    public struct NightVisionData
    {
        public float strength;
        public float radius;

        public NightVisionData(float _strength, float _radius)
        {
            strength = _strength;
            radius = _radius;
        }
    }

    [Serializable]
    public struct PresetData
    {
        public string id;
        public string name;
        public float radius;
        public int angle;
        public Color color;
        public LightEffect effect;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public static bool operator ==(PresetData data_1, PresetData data_2)
        {
            return JsonUtility.ToJson(data_1) == JsonUtility.ToJson(data_2);
        }
        public static bool operator !=(PresetData data_1, PresetData data_2)
        {
            return JsonUtility.ToJson(data_1) != JsonUtility.ToJson(data_2);
        }
    }

    [Serializable]
    public struct LightEffect
    {
        public int type;
        public float strength;
        public float frequency;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public static bool operator ==(LightEffect data_1, LightEffect data_2)
        {
            return JsonUtility.ToJson(data_1) == JsonUtility.ToJson(data_2);
        }
        public static bool operator !=(LightEffect data_1, LightEffect data_2)
        {
            return JsonUtility.ToJson(data_1) != JsonUtility.ToJson(data_2);
        }
    }
}