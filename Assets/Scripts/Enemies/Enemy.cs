using System.Collections;
using MyUtils.Classes;
using MyUtils.Functions;
using MyUtils.Interfaces;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable {
    public RoomController _currentRoom;
    public EnemySO _defaultSetting;
    public Weapon _weapon;
    public Transform _target;
    public Transform _firePoint;
    public Transform _weaponHolder;
    public SpriteRenderer _weaponSR;
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
    public bool _spawnedByBoss;
    private AssetManager _gDM;
    private AudioManager _gAM;
    private GameManager _gM;

    public void Awake() {
        _gDM = AssetManager._I;
        _gAM = AudioManager._I;
        Timer._objectToDestroy.Add(transform.parent.gameObject);

        _weapon = new(_defaultSetting._defaultWeapon);
        _weapon.Setup(null, _weaponSR);
        _weapon._bulletsInMagazine = _weapon._defaultSettings._maxBullet;
        _nextShootTime = Time.time + _defaultSetting._firstShootDelay.GetValue();

        _target = PlayerController._I.transform;
        _currentHealth = _defaultSetting._maxHealth * GameManager._gSettings._enemyMaxHealthMultiplier;
        _currentSpeed = _defaultSetting._speed.GetValue() * GameManager._gSettings._enemySpeedMultiplier;

        _rgb = GetComponent<Rigidbody2D>();

        ParticleAssetManager._I.InstantiateParticles(ParticleType.EnemySpawn, transform.position);
        AudioManager._I.PlaySoundEffect(AudioType.EnemySpawn, transform.position);
    }
    void Update() {
        RotateWeaponToPlayer();
        if (_nextShootTime < Time.time) Shoot();
        if (_nextMoveDirectionChange > Time.time) return;
        if (Vector2.Distance(_target.position, transform.position) > _defaultSetting._playerDist) {
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
        b._bulletDamage = _defaultSetting._baseDamage.GetValue() * GameManager._gSettings._enemyDamageMultiplier;
        _weapon.Shoot(1);
        _nextShootTime = Time.time + _defaultSetting._shootDelays[_delayIndex].GetValue();
        _delayIndex++;
        if (_delayIndex >= _defaultSetting._shootDelays.Count) _delayIndex = 0;
        AudioManager._I.PlaySoundEffect(AudioType.EnemyShoot, WeaponType.Single, transform.position); //TODO apply weapon type

    }
    private IEnumerator Reload() {
        if (_isReloading) yield return null;
        _isReloading = true;
        yield return new WaitForSeconds(_defaultSetting._reloadSpeed.GetValue());
        _weapon.Reload();
        Debug.Log("Reloaded");
        _isReloading = false;
        AudioManager._I.PlaySoundEffect(AudioType.PlayerReloadEnd, transform.position); //Todo maybe change to enemy reload
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
        v -= v * GameManager._gSettings._enemyDamageReductionMultiplier;
        _currentHealth -= v;
        ParticleAssetManager._I.InstantiateParticles(ParticleType.EnemyDamage, transform.position);
        if (_currentHealth <= 0) Die();
        AudioManager._I.PlaySoundEffect(AudioType.EnemyDamage, transform.position);

    }
    public void Die() {
        ParticleAssetManager._I.InstantiateParticles(ParticleType.EnemyDie, transform.position);
        if (Random.Range(0f, 1f) < 0.3f * GameManager._gSettings._specialItemSpawnChange) _ = Instantiate(MyRandom.GetFromArray<Transform>(_gDM._itemsToSpawn), transform.position, Quaternion.identity);
        if (!_spawnedByBoss) _ = _currentRoom._enemies.Remove(this);
        else { Boss._I._enemiesCount -= 1; Boss._I.ChangeName(); };
        Destroy(transform.parent.gameObject);
        AudioManager._I.PlaySoundEffect(AudioType.EnemyDie, transform.position);
    }
    void OnDestroy() {
        if (!_spawnedByBoss) if (_currentRoom != null) _currentRoom.EnemiesDie();

    }
}
