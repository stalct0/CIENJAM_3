using System.Collections.Generic;
using UnityEngine;

public class AudioManager3D : MonoBehaviour
{
    public static AudioManager3D I { get; private set; }

    [Header("Data")]
    [SerializeField] private SfxLibrary library;

    [Header("Pooling")]
    [SerializeField] private int initialPoolSize = 16;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    
    private readonly Queue<AudioSource> pool = new();
    private readonly Dictionary<(SfxId, Transform), AudioSource> followLoops = new();

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        if (!bgmSource)
        {
            var go = new GameObject("[BGM]");
            go.transform.SetParent(transform);
            bgmSource = go.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f; // 항상 2D
        }
        WarmPool();
    }

    private void WarmPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            pool.Enqueue(CreateSourceObject());
        }
    }

    private AudioSource CreateSourceObject()
    {
        var go = new GameObject("[PooledAudioSource]");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        go.SetActive(false);
        return src;
    }

    private AudioSource Rent()
    {
        var src = (pool.Count > 0) ? pool.Dequeue() : CreateSourceObject();
        src.gameObject.SetActive(true);
        return src;
    }

    private void Return(AudioSource src)
    {
        if (!src) return;
        src.Stop();
        src.clip = null;
        src.loop = false;
        src.transform.SetParent(transform);
        src.gameObject.SetActive(false);
        pool.Enqueue(src);
    }

    private void ApplySettings(AudioSource src, SfxLibrary.Entry e)
    {
        src.volume = e.volume;

        float pitchRand = Random.Range(-e.randomPitch, e.randomPitch);
        src.pitch = Mathf.Max(0.01f, e.pitch + pitchRand);

        src.spatialBlend = e.spatialBlend; // 1이면 3D
        src.minDistance = e.minDistance;
        src.maxDistance = e.maxDistance;
        src.rolloffMode = AudioRolloffMode.Logarithmic;

        src.loop = e.loop;
    }

    private AudioClip PickClip(SfxLibrary.Entry e)
    {
        if (e.clips == null || e.clips.Length == 0) return null;
        if (e.clips.Length == 1) return e.clips[0];
        return e.clips[Random.Range(0, e.clips.Length)];
    }

    /// <summary>
    /// 위치에서 1회 재생(원샷). 3D라면 거리 감쇠 적용됨.
    /// </summary>
    public void Play(SfxId id, Vector3 position)
    {
        if (!library) { Debug.LogWarning("AudioManager3D: library not set"); return; }
        var e = library.Get(id);
        if (e == null) { Debug.LogWarning($"AudioManager3D: missing entry {id}"); return; }

        var clip = PickClip(e);
        if (!clip) return;

        var src = Rent();
        src.transform.position = position;
        ApplySettings(src, e);

        // 원샷이면 loop 강제 false
        src.loop = false;

        src.clip = clip;
        src.Play();

        // 클립 재생 끝나면 반납
        StartCoroutine(ReturnWhenDone(src, clip.length / Mathf.Max(0.01f, src.pitch)));
    }

    private System.Collections.IEnumerator ReturnWhenDone(AudioSource src, float duration)
    {
        yield return new WaitForSeconds(duration);
        Return(src);
    }

    /// <summary>
    /// 특정 Transform을 따라다니며 재생. 루프용으로 사용.
    /// </summary>
    public void PlayFollow(SfxId id, Transform followTarget)
    {
        if (!followTarget) return;
        if (!library) { Debug.LogWarning("AudioManager3D: library not set"); return; }

        var key = (id, followTarget);
        if (followLoops.ContainsKey(key))
        {
            // 이미 있으면 재시작/유지(원하면 재시작)
            var existing = followLoops[key];
            if (existing && !existing.isPlaying) existing.Play();
            return;
        }

        var e = library.Get(id);
        if (e == null) { Debug.LogWarning($"AudioManager3D: missing entry {id}"); return; }

        var clip = PickClip(e);
        if (!clip) return;

        var src = Rent();
        src.transform.SetParent(followTarget, worldPositionStays: false);
        src.transform.localPosition = Vector3.zero;

        ApplySettings(src, e);

        // Follow는 기본적으로 loop를 기대. 엔트리에서 loop 체크 안 했으면 강제 loop 켜도 됨.
        src.loop = true;
        src.clip = clip;
        src.Play();

        followLoops.Add(key, src);
    }

    public void StopFollow(SfxId id, Transform followTarget)
    {
        if (!followTarget) return;

        var key = (id, followTarget);
        if (!followLoops.TryGetValue(key, out var src)) return;

        followLoops.Remove(key);
        Return(src);
    }
    
    public void PlaySkillSfx(SkillDefinition def, Vector3 position)
    {
        if (!def || !def.sfxClip) return;

        var src = Rent();                 // 풀링 쓴다면
        src.transform.position = position;

        src.volume = def.sfxVolume;
        src.spatialBlend = def.sfxSpatialBlend;
        src.minDistance = def.sfxMinDistance;
        src.maxDistance = def.sfxMaxDistance;

        float pitchRand = Random.Range(-def.sfxRandomPitch, def.sfxRandomPitch);
        src.pitch = Mathf.Max(0.01f, def.sfxPitch + pitchRand);

        src.loop = false;
        src.clip = def.sfxClip;
        src.Play();

        StartCoroutine(ReturnWhenDone(src, def.sfxClip.length / Mathf.Max(0.01f, src.pitch)));
    }
    public void PlayBgm(AudioClip clip, float volume = 1f, bool restart = false)
    {
        if (!clip) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying && !restart)
            return;

        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource.isPlaying)
            bgmSource.Stop();
    }
}