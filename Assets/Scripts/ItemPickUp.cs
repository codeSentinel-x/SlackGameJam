using System.Collections;
using UnityEngine;

public enum ItemType {
    Ammo,
    Healing,
    Weapon,
    Blank,
    Special,
    Key,
}
public class ItemPickUp : MonoBehaviour {
    public string _name;
    public int _amount;
    public ItemType _itemType;
    public Transform _pickupParticle;
    void Awake() {
        if (_itemType == ItemType.Weapon) {
            GetComponent<SpriteRenderer>().sprite = AssetManager.LoadWeaponByName(_name)._sprite;
        }
    }
    void Start() {
        switch (_itemType) {
            case ItemType.Weapon: {
                    GetComponent<TooltipShower>().Setup(AssetManager.LoadWeaponByName(_name));
                    break;
                }
            case ItemType.Special: {
                    GetComponent<TooltipShower>().Setup(AssetManager.LoadItemByName(_name));
                    break;
                }

        }
    }
    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            var p = other.gameObject.GetComponent<PlayerController>();
            p._currentItemInRange = this;
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            var p = other.gameObject.GetComponent<PlayerController>();
            if (p._currentItemInRange == this) p._currentItemInRange = null;
        }
    }
    void OnDestroy() {
        _ = Instantiate(_pickupParticle, transform.position, Quaternion.identity);
    }

}
public class Source<T> {
    public T value;
}
