using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace GreenleafAdminCmds.Patches;

[HarmonyPatch]
public class TpCmdPatch {
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod() {
        var type = AccessTools.TypeByName("Vintagestory.Server.CmdTp");
        return AccessTools.Method(type, "handleTp");
    }

    [HarmonyPrefix]
    public static void Prefix(Entity e) {
        if (e is EntityPlayer player && player.Player.HasPrivilege(Privilege.tp))
            Main.SavedTPBackPositions[player.PlayerUID] = player.Pos.XYZ.Clone();
    }
}

[HarmonyPatch(typeof(GenStoryStructures), "OnTpStoryLoc")]
public class TpStoryLocPatch {
    [HarmonyPrefix]
    public static void Prefix(TextCommandCallingArgs args) {
        Entity e = args.Caller.Entity;

        if (e is EntityPlayer player && player.Player.HasPrivilege(Privilege.tp))
            Main.SavedTPBackPositions[player.PlayerUID] = player.Pos.XYZ.Clone();
    }
}