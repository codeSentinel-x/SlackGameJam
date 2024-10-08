using System;
using System.Collections;
using System.Collections.Generic;
using MyUtils.Classes;
using MyUtils.Enums;
using MyUtils.Interfaces;
using UnityEngine;

public class PlayerCombat : MonoBehaviour, IDamageable {

    [Header("Setup")]
    public Transform _weaponHolder;
    public Transform _firePoint;
    public Transform _spriteRenderer;
    public SpriteRenderer _weaponSpriteR;
    public string _defaultWeaponName;
    public int _blankAmount;
    public float _rotSpeed;
    public static Action<float> _onPlayerHealthChange;
    private Weapon _currentWeapon;
    private int _currentWeaponIndex = 0;
    private List<Weapon> _weapons = new();
    private float _maxHealth;
    private float _damageIgnore;
    private float _damageReduction;
    private float _currentHealth;
    private bool _isReloading;
    private float _reloadSpeedMult;
    private float _bulletSpeedMult;
    private float _shootDelayMult;
    private float _invincibleTime;
    private float _invincibleAfterDash;
    private float _currentHealthRatio;
    private AudioManager _gAM;
    private bool _shootPressed;
    private PlayerAnimations _pAnim;
    void Awake() {
        _gAM = AudioManager._I;
        SubscribeStats();
    }

    void Start() {
        _pAnim = GetComponent<PlayerAnimations>();
        InitializeWeapon();
        MyLog();
        ParticleAssetManager._I.InstantiateParticles(ParticleType.PlayerSpawn, transform.position);
        ReloadKeyBinds();
    }
    KeyCode _shoot, _reload, _useBlank, _changeWeapon;
    private void ReloadKeyBinds() {
        _shoot = InputManager._I.GetKey(KeyBindType.Shoot);
        _useBlank = InputManager._I.GetKey(KeyBindType.UseBlank);
        _reload = InputManager._I.GetKey(KeyBindType.Reload);
        _changeWeapon = InputManager._I.GetKey(KeyBindType.ChangeWeapon);
    }

    public void Update() {
        HandleInput();
    }
    void LateUpdate() {
        RotateWeaponToMouse();
    }
    void SubscribeStats() {
        var d = GetComponent<PlayerController>()._data;
        d._maxHealth._OnStatValueChanged += (x) => { _maxHealth = x * GameManager._gSettings._playerMaxHealthMultiplier; _currentHealth = _maxHealth * _currentHealthRatio; _onPlayerHealthChange?.Invoke(_currentHealth); };
        d._damageIgnore._OnStatValueChanged += (x) => _damageIgnore = x;
        d._damageReduction._OnStatValueChanged += (x) => _damageReduction = x;
        d._reloadSpeedMult._OnStatValueChanged += (x) => _reloadSpeedMult = x;
        d.__bulletSpeedMult._OnStatValueChanged += (x) => _bulletSpeedMult = x;
        d._shootDelayMultiplier._OnStatValueChanged += (x) => _shootDelayMult = x;
        d._invincibleAfterDash._OnStatValueChanged += (x) => _invincibleAfterDash = x;
        PlayerMovement._onDashStart += RefreshInvincible;

        _onPlayerHealthChange += (x) => { _currentHealthRatio = _currentHealth / _maxHealth; PlayerUI._I.RefreshHealth(_currentHealth, _maxHealth); };
    }

    void RefreshInvincible() {
        _invincibleTime = Time.time + _invincibleAfterDash;
    }

    void InitializeWeapon() {
        _currentWeapon = new(AssetManager.LoadWeaponByName(_defaultWeaponName));
        _currentWeapon.Setup(_firePoint, _weaponSpriteR);
        _weapons.Add(_currentWeapon);
        _weapons.Add(new(AssetManager.LoadWeaponByName("Eagle")));

        _currentWeaponIndex = _weapons.Count - 1;
        _currentHealth = _maxHealth;
        _currentHealthRatio = 1;
        PlayerUI._I.RefreshHealth(_currentHealth, _maxHealth);
        PlayerUI._I.ResetBlanks(_blankAmount);
        PlayerUI._I.ChangeWeapon(_currentWeapon._defaultSettings._sprite);
        ResetBulletDisplay();
    }
    private void HandleInput() {
        if (_currentWeapon._defaultSettings._auto) {
            if (Input.GetKey(_shoot)) Shoot();
        } else {
            if (Input.GetKeyDown(_shoot)) Shoot();
        }
        if (Input.GetKeyDown(_reload) && !_isReloading) StartCoroutine(Reload());
        if (Input.GetKeyDown(_changeWeapon)) NextWeapon();
        // if (Input.GetKeyDown(KeyCode.LeftControl)) PreviousWeapon();
        if (Input.GetKeyDown(_useBlank)) UseBlank();
    }
    public void Shoot() {
        if (_isReloading) return;
        if (_currentWeapon._nextShoot > Time.time) return;
        if (_currentWeapon._bulletsInMagazine <= 0) { _ = StartCoroutine(Reload()); /*Debug.Log("No bullets");*/ return; }
        // Debug.Log("Piu");
        _pAnim.ChangeState(PlayerAnimations.AnimState.attack);
        float sp = UnityEngine.Random.Range(0f, _currentWeapon._defaultSettings._spread) * (UnityEngine.Random.Range(0, 2) == 1 ? 1 : -1);

        Quaternion spread = Quaternion.Euler(_weaponHolder.rotation.eulerAngles + new Vector3(0, 0, sp));
        var b = Instantiate(_currentWeapon._defaultSettings._bulletPref, _firePoint.position, spread).GetComponentInChildren<BulletMono>();
        b.Setup(_currentWeapon._defaultSettings._bulletSetting, _bulletSpeedMult, gameObject.layer, gameObject.tag, GetComponent<Collider2D>());
        b._bulletDamage *= GameManager._gSettings._playerDamageMultiplier;

        Physics2D.IgnoreCollision(b.GetComponent<Collider2D>(), GetComponent<Collider2D>());
        _currentWeapon.Shoot(_shootDelayMult);
        PlayerUI._I.DecaresBullet(1, _currentWeapon._bulletsInMagazine, _currentWeapon._allBullets);
        AudioManager._I.PlaySoundEffect(AudioType.PlayerShoot, WeaponType.Single, transform.position); //TODO apply weapon type

    }
    public void NextWeapon() {
        if (_isReloading) return;
        AudioManager._I.PlaySoundEffect(AudioType.PlayerWeaponChange, transform.position);
        _currentWeaponIndex += 1;
        if (_currentWeaponIndex >= _weapons.Count) _currentWeaponIndex = 0;
        _currentWeapon = _weapons[_currentWeaponIndex];
        _currentWeapon.Setup(_firePoint, _weaponSpriteR);
        ResetBulletDisplay();
        PlayerUI._I.ChangeWeapon(_currentWeapon._defaultSettings._sprite);


    }
    public void PreviousWeapon() {
        if (_isReloading) return;
        AudioManager._I.PlaySoundEffect(AudioType.PlayerWeaponChange, transform.position);
        _currentWeaponIndex -= 1;
        if (_currentWeaponIndex < 0) _currentWeaponIndex = _weapons.Count - 1;
        _currentWeapon = _weapons[_currentWeaponIndex];
        _currentWeapon.Setup(_firePoint, _weaponSpriteR);
        ResetBulletDisplay();
        PlayerUI._I.ChangeWeapon(_currentWeapon._defaultSettings._sprite);

    }


    private IEnumerator Reload() {
        if (_isReloading) yield return null;
        if (_currentWeapon._bulletsInMagazine == _currentWeapon._defaultSettings._maxBullet) yield return null;
        _pAnim.ChangeState(PlayerAnimations.AnimState.reload);
        AudioManager._I.PlaySoundEffect(AudioType.PlayerReloadStart, transform.position);
        _isReloading = true;
        _ = StartCoroutine(PlayerUI._I.DisplayReload(_currentWeapon._reloadTime * _reloadSpeedMult));
        yield return new WaitForSeconds(_currentWeapon._reloadTime * _reloadSpeedMult);
        _currentWeapon.Reload();
        Debug.Log("Reloaded");
        _isReloading = false;
        ResetBulletDisplay();
        AudioManager._I.PlaySoundEffect(AudioType.PlayerReloadEnd, transform.position);

    }

    void ResetBulletDisplay() => PlayerUI._I.ResetBullets(_currentWeapon._bulletsInMagazine, _currentWeapon._allBullets);

    private void RotateWeaponToMouse() {

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new(Input.mousePosition.x, Input.mousePosition.y, -10));
        Vector2 direction = mousePos - transform.position;
        // direction.Normalize();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < -90 || angle > 90) ChangeLocalScale(-1);
        else ChangeLocalScale(1);
        angle -= 90;
        Quaternion rot = Quaternion.AngleAxis(angle, Vector3.forward);
        _weaponHolder.transform.rotation = Quaternion.Lerp(_weaponHolder.transform.rotation, rot, _rotSpeed * Time.deltaTime);
    }
    void ChangeLocalScale(int x) {
        _weaponHolder.transform.GetChild(0).localScale = new(x, 1, 1);
        _spriteRenderer.localScale = new(Mathf.Abs(_spriteRenderer.localScale.x) * x, _spriteRenderer.localScale.y, 1);
    }

    public void Damage(float v) {
        if (Time.time < _invincibleTime) { Debug.Log("Player is invincible"); return; }
        var v1 = v - _damageIgnore;
        var v2 = v1 - v1 * _damageReduction;
        var v3 = v2 - v2 * GameManager._gSettings._playerDamageReductionMultiplier;
        _currentHealth -= v3;
        ParticleAssetManager._I.InstantiateParticles(ParticleType.PlayerDamage, transform.position);
        if (_currentHealth <= 0) Die();
        Debug.Log($"Base damage: {v}, After ignore: {v1}, After first reduction {v2}, after second reduction {v3}");
        _onPlayerHealthChange?.Invoke(v);
        AudioManager._I.PlaySoundEffect(AudioType.PlayerDamage, transform.position);
    }
    public void RestoreHealth(float v) {
        _currentHealth += v;
        if (_currentHealth > _maxHealth) _currentHealth = _maxHealth;
        _onPlayerHealthChange?.Invoke(v);
        AudioManager._I.PlaySoundEffect(AudioType.PlayerHeal, transform.position);
    }

    private void Die() {
        Debug.Log("Player died");
        Timer._I.LoadScene();
        Soundtrack._I.CombatEnd();
    }
    public void AddWeapon(WeaponSO _base) {
        _weapons.Add(new(_base));
    }
    public void AddAmmo() {
        _currentWeapon._allBullets += _currentWeapon._defaultSettings._maxBullet;
        ResetBulletDisplay();
    }
    public void RestoreHealth() {
        _currentHealth += _maxHealth / 5;
        if (_currentHealth > _maxHealth) _currentHealth = _maxHealth;
        _onPlayerHealthChange?.Invoke(_currentHealth);

    }
    public void UseBlank() {
        if (_blankAmount <= 0) return;
        _blankAmount -= 1;
        ParticleAssetManager._I.InstantiateParticles(ParticleType.PlayerBlank, transform.position);
        PlayerUI._I.DecaresBlank(1);
        AudioManager._I.PlaySoundEffect(AudioType.PlayerBlank, transform.position);
        foreach (var g in GameObject.FindGameObjectsWithTag("Bullet")) { Destroy(g.transform.parent.gameObject); }
    }
    public void AddBlank(int val = 1) {
        _blankAmount += val;
        PlayerUI._I.IncreaseBlank(val);
    }
    public void MyLog() {
#if UNITY_EDITOR 
        // CustomLog.Log("start", "default", true);
#endif
    }
}
