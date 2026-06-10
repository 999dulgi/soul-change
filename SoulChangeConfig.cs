using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Rooms;

namespace SoulChange;

public static class SoulChangeConfig
{
    // 스왑을 발동시키는 방 타입. 기본값: 일반 전투, 엘리트, 보스
    public static HashSet<RoomType> TriggerRooms { get; set; } = new HashSet<RoomType>
    {
        RoomType.Monster,
        RoomType.Elite,
        RoomType.Boss,
    };

    // true: 보스방에서 원래 캐릭터로 원복. false: 보스방도 일반 방처럼 처리
    public static bool RestoreOnBoss { get; set; } = true;

    // 트리거 방을 몇 번 지나야 스왑할지. 1 = 매번, 2 = 2번마다, ...
    public static int SwapEveryNFloors { get; set; } = 1;

    public static int ModVersion { get; set;} = 103;

    public static SoulChangeSettingsMessage ToMessage()
    {
        int flags = TriggerRooms.Aggregate(0, (acc, r) => acc | (1 << (int)r));
        return new SoulChangeSettingsMessage { RestoreOnBoss = RestoreOnBoss, TriggerRoomFlags = flags, SwapEveryNFloors = SwapEveryNFloors, ModVersion = ModVersion };
    }

    public static void ApplyMessage(SoulChangeSettingsMessage msg)
    {
        RestoreOnBoss = msg.RestoreOnBoss;
        SwapEveryNFloors = msg.SwapEveryNFloors;
        TriggerRooms = Enum.GetValues<RoomType>()
            .Where(r => (msg.TriggerRoomFlags & (1 << (int)r)) != 0)
            .ToHashSet();
    }
}
