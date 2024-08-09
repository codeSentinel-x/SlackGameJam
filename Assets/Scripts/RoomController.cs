using System.Collections;
using System.Collections.Generic;
using MyUtils.Enums;
using UnityEngine;

public class RoomController : MonoBehaviour {
    public RoomType _roomType;
    public BoxCollider2D _cameraBoundaries;
    public DoorController[] _doors;
}
