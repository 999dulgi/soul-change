using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace SoulChange;

public struct SoulChangeSettingsMessage : INetMessage
{
    public bool RestoreOnBoss;
    public int TriggerRoomFlags; // RoomType enum 값의 비트마스크
    public int SwapEveryNFloors;

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;
    public bool ShouldBuffer => true;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteBool(RestoreOnBoss);
        writer.WriteInt(TriggerRoomFlags);
        writer.WriteInt(SwapEveryNFloors);
    }

    public void Deserialize(PacketReader reader)
    {
        RestoreOnBoss = reader.ReadBool();
        TriggerRoomFlags = reader.ReadInt();
        SwapEveryNFloors = reader.ReadInt();
    }
}
