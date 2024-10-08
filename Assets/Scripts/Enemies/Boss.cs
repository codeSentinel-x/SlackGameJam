using System.Collections;
using MyUtils.Classes;
using MyUtils.Functions;
using MyUtils.Interfaces;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class Boss : MonoBehaviour, IDamageable {
    public static Boss _I;
    public RoomController _currentRoom;
    public BossSO[] _defaultSetting;
    public Weapon _weapon;
    public Transform _target;
    public Transform _firePoint;
    public Transform _weaponHolder;
    public SpriteRenderer _weaponSR;
    public Transform[] _objectToSpawn;
    public Vector2 _moveDirection;
    public float _nextMoveDirectionChange;
    public float _minPlayerDist;
    private Rigidbody2D _rgb;
    private bool _isReloading;
    public Transform _spriteRenderer;
    private int _delayIndex;
    private float _nextShootTime;
    private float _currentHealth;
    private float _currentSpeed;
    public int _stage = 0;
    private BossSO _currentStageSO;
    public SpriteRenderer[] _sprites;

    private AssetManager _gDM;
    private AudioManager _gAM;
    private GameManager _gM;
    void Awake() {
        _gDM = AssetManager._I;
        _gAM = AudioManager._I;
    }
    public void Start() {
        _I = this;
        NextStage(false);
        Timer._objectToDestroy.Add(gameObject);
        _sprites = GetComponentsInChildren<SpriteRenderer>();
        ParticleAssetManager._I.InstantiateParticles(ParticleType.BossSpawn, transform.position);
        AudioManager._I.PlaySoundEffect(AudioType.BossSpawn, transform.position);
        _target = PlayerController._I.transform;
    }
    public int _enemiesCount;
    public void NextStage(bool increase = true) {
        if (increase) _stage++;
        if (_stage == 5) { Die(); return; }
        _delayIndex = 0;
        _currentStageSO = _defaultSetting[_stage];
        StartInvincible();
        _weapon = new(_currentStageSO._defaultWeapon);
        _weapon.Setup(null, _weaponSR);
        _currentHealth = _currentStageSO._maxHealth;
        _rgb = GetComponent<Rigidbody2D>();
        _weapon._bulletsInMagazine = _weapon._defaultSettings._maxBullet;
        _nextShootTime = Time.time + _currentStageSO._firstShootDelay.GetValue();
        _currentSpeed = _currentStageSO._speed.GetValue();
        BossUI._I.UpdateHealth(_currentHealth, _currentStageSO._maxHealth);
        _ = StartCoroutine(SpawnEnemies());

    }
    public IEnumerator SpawnEnemies() {
        for (int i = 0; i < _currentStageSO._countOfEnemiesToSpawn; i++) {
            var e = Instantiate(MyRandom.GetFromArray<Transform>(_currentStageSO._enemyToSpawn), transform.position + new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f)), Quaternion.identity).GetComponentInChildren<Enemy>();
            e._currentRoom = _currentRoom;
            e._spawnedByBoss = true;
            _enemiesCount += 1;
            BossUI._I.ChangeName(true, _enemiesCount, _stage);
            yield return new WaitForSeconds(0.5f);
        }
    }
    bool _isInvincible = false;
    public void ChangeName() {
        if (_enemiesCount > 0) BossUI._I.ChangeName(true, _enemiesCount, _stage);
    }
    public void StartInvincible() {
        _isInvincible = true;
        foreach (var r in _sprites) {
            r.color = new Color(r.color.r, r.color.g, r.color.b, 0.1f);
        }
        BossUI._I.ChangeName(true, _enemiesCount, _stage);
    }
    public void StopInvincible() {
        _isInvincible = false;
        ParticleAssetManager._I.InstantiateParticles(ParticleType.BossInvincibleStop, transform.position);
        foreach (var r in _sprites) {
            r.color = new Color(r.color.r, r.color.g, r.color.b, 1f);
        }
        BossUI._I.ChangeName(false, 0, _stage);

    }
    void Update() {
        RotateWeaponToPlayer();
        if (_isInvincible && _enemiesCount <= 0) StopInvincible();
        if (!_isInvincible && _enemiesCount > 0) StartInvincible();
        if (_nextShootTime < Time.time) Shoot();
        if (_nextMoveDirectionChange > Time.time) return;
        if (Vector2.Distance(_target.position, transform.position) > _currentStageSO._playerDist) {
            _moveDirection = _target.position - transform.position;
            _nextMoveDirectionChange = Time.time + Random.Range(1f, 2f);
        } else {
            Vector2 newVec = new(Mathf.Clamp(transform.position.x - Random.Range(-6f, 6f), _currentRoom.transform.position.x - 10, _currentRoom.transform.position.x + 10), Mathf.Clamp(transform.position.y - Random.Range(-6f, 6f), _currentRoom.transform.position.y - 6, _currentRoom.transform.position.y + 6));
            _moveDirection = newVec - (Vector2)transform.position;
            _nextMoveDirectionChange = Time.time + Random.Range(1f, 2f);
        }
    }
    void FixedUpdate() {
        _rgb.velocity = _moveDirection.normalized * _currentSpeed;
    }
    public void Shoot() {
        if (_isReloading) return;
        if (_weapon._nextShoot > Time.time) return;
        if (_weapon._bulletsInMagazine <= 0) { _ = StartCoroutine(Reload()); _weapon._allBullets += 30; Debug.Log("No bullets"); return; }
        // Debug.Log("Piu");
        float sp = Random.Range(0f, _weapon._defaultSettings._spread) * (Random.Range(0, 2) == 1 ? 1 : -1);
        Quaternion spread = Quaternion.Euler(_weaponHolder.rotation.eulerAngles + new Vector3(0, 0, sp));
        var b = Instantiate(_weapon._defaultSettings._bulletPref, _firePoint.position, spread).GetComponentInChildren<BulletMono>();
        b.Setup(_weapon._defaultSettings._bulletSetting, 1, gameObject.layer, gameObject.tag, GetComponent<Collider2D>());
        b._bulletDamage = _currentStageSO._baseDamage.GetValue();
        _weapon.Shoot(1);
        _nextShootTime = Time.time + _currentStageSO._shootDelays[_delayIndex].GetValue();
        _delayIndex++;
        if (_delayIndex >= _currentStageSO._shootDelays.Count) _delayIndex = 0;
        AudioManager._I.PlaySoundEffect(AudioType.BossShoot, WeaponType.Single, transform.position); //TODO apply actual weapon type

    }
    private IEnumerator Reload() {
        if (_isReloading) yield return null;
        _isReloading = true;
        yield return new WaitForSeconds(_currentStageSO._reloadSpeed.GetValue());
        _weapon.Reload();
        Debug.Log("Reloaded");
        _isReloading = false;
        AudioManager._I.PlaySoundEffect(AudioType.PlayerReloadEnd, transform.position); //Todo change to actual boss reload sound and apply on start too
    }
    private void RotateWeaponToPlayer() {

        Vector2 direction = _target.transform.position - transform.position;
        // direction.Normalize();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < -90 || angle > 90) ChangeLocalScale(-1);
        else ChangeLocalScale(1);
        angle -= 90;
        Quaternion rot = Quaternion.AngleAxis(angle, Vector3.forward);
        _weaponHolder.transform.rotation = Quaternion.Lerp(_weaponHolder.transform.rotation, rot, 100 * Time.deltaTime);
    }
    void ChangeLocalScale(int x) {
        _weaponHolder.transform.GetChild(0).localScale = new(x, 1, 1);
        _spriteRenderer.localScale = new(Mathf.Abs(_spriteRenderer.localScale.x) * x, _spriteRenderer.localScale.y, 1);
    }

    public void Damage(float v) {
        if (_isInvincible) return;
        _currentHealth -= v;
        ParticleAssetManager._I.InstantiateParticles(ParticleType.BossDamage, transform.position);
        if (_currentHealth <= 0) {
            if (_stage < 5) {
                NextStage();
                ParticleAssetManager._I.InstantiateParticles(ParticleType.BossStageChange, transform.position);
            } else Die();
        }
        AudioManager._I.PlaySoundEffect(AudioType.BossDamage, transform.position);
        BossUI._I.UpdateHealth(_currentHealth, _currentStageSO._maxHealth);

    }
    public void Die() {
        ParticleAssetManager._I.InstantiateParticles(ParticleType.BossDie, transform.position);
        _currentRoom.OnClear();
        // foreach (var d in _currentRoom._doors) d._door._uncloseDoor?.Invoke();
        AudioManager._I.PlaySoundEffect(AudioType.BossDie, transform.position);
        Destroy(transform.parent.gameObject);
    }
}
