namespace MyUtils.Enums {
    public enum RoomType {
        RandomGenerated,
        StartRoom,
        ResetRoom,
        BossRoom,
        ExitRoom,
    }
    public enum DoorOpenType {
        OpenOnShoot,
        OpenOnBlank,
        OpenOnDestroyAllItemOfType,
        OpenOnTime,
        OpenOnButtonClick,
        OpenOnPlayerStat,
        OpenOnDefeatEnemy,
        OpenOnCustomItemHold,
    }
    public enum DoorState {
        Hidden,
        Visible,
        Opened,
    }

}