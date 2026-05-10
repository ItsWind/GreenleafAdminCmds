using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace GreenleafAdminCmds;

public class Main : ModSystem {
    private Harmony harmony;
    public static ICoreServerAPI API { get; private set; }
    public static Dictionary<string, Vec3d> SavedTPBackPositions = new();

    public override bool ShouldLoad(EnumAppSide forSide) {
        return forSide == EnumAppSide.Server;
    }

    public override void StartServerSide(ICoreServerAPI api) {
        API = api;

        new Commands();

        harmony = new Harmony(Mod.Info.ModID);
        harmony.PatchAll();
    }

    public override void Dispose() {
        harmony.UnpatchAll();
    }
}
