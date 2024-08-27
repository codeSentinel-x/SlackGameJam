using UnityEngine;

public class SoundPlayer : MonoBehaviour {
    AudioSource _source;
    public bool _started;
    public float _minLifeTime = 1;
    public void Play(AudioClip clip) {
        _source = GetComponent<AudioSource>();
        _source.volume = GameManager._gSettings._soundsVolume;
        _source.clip = clip;
        _source.Play();
        _started = true;
        _minLifeTime = Time.time + 1;
    }
    void Update() {
        // if (!_started && Time.time < _minLifeTime) return;
        // if (_source.isPlaying) Destroy(gameObject);
    }
}
