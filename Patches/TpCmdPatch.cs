using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

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
