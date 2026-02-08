using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/SFX Library")]
public class SfxLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public SfxId id;
        public AudioClip[] clips;

        [Header("Playback")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.25f, 3f)] public float pitch = 1f;
        [Range(0f, 1f)] public float randomPitch = 0.05f;

        [Header("3D")]
        [Range(0f, 1f)] public float spatialBlend = 1f; // 1 = 3D
        public float minDistance = 1.5f;
        public float maxDistance = 25f;

        [Header("Loop")]
        public bool loop = false;
    }

    public List<Entry> entries = new();

    private Dictionary<SfxId, Entry> _map;

    public Entry Get(SfxId id)
    {
        if (_map == null)
        {
            _map = new Dictionary<SfxId, Entry>(entries.Count);
            foreach (var e in entries)
            {
                if (_map.ContainsKey(e.id)) continue;
                _map.Add(e.id, e);
            }
        }

        _map.TryGetValue(id, out var entry);
        return entry;
    }
}