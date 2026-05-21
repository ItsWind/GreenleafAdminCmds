using HarmonyLib;
using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace GreenleafAdminCmds;

public class Commands {
    public Commands() {
        Main.API.RegisterCommand("tpback", "Teleports you back to where you were before initiating a successful /tp command.", "", new ServerChatCommandDelegate(this.OnTPBackCommand), Privilege.tp);
        Main.API.RegisterCommand("tpentityid", "Teleports an entity by ID to your position.", "-e ENTITYID", new ServerChatCommandDelegate(this.OnTPEntityID), Privilege.tp);
        Main.API.RegisterCommand("tagboatcreator", "Tags a boat as being created by a certain player's username.", "-p USERNAME", new ServerChatCommandDelegate(this.OnTagBoatCreator), Privilege.ban);
        Main.API.RegisterCommand("forcegroupop", "Forces a group to accept a player within the group to be OP.", "-p USERNAME -g GROUPNAME", new ServerChatCommandDelegate(this.OnForceGroupOP), Privilege.controlserver);
        Main.API.RegisterCommand("forcegroupowner", "Forces a group to accept a player within the group to be the new Owner.", "-p USERNAME -g GROUPNAME", new ServerChatCommandDelegate(this.OnForceGroupOwner), Privilege.controlserver);
    }

    private void OnTPBackCommand(IServerPlayer player, int groupId, CmdArgs args) {
        if (!Main.SavedTPBackPositions.ContainsKey(player.PlayerUID)) {
            Main.API.SendMessage(player, groupId, "No saved position found for you to teleport back to.", EnumChatType.CommandError);
            return;
        }

        player.Entity.TeleportTo(Main.SavedTPBackPositions[player.PlayerUID]);
        Main.API.SendMessage(player, groupId, "Teleported back to saved position.", EnumChatType.CommandSuccess);
    }

    private void OnTPEntityID(IServerPlayer player, int groupId, CmdArgs args) {
        long entityID = 0;
        while (args.Length > 0) {
            string argFlag = args.PopWord();
            switch (argFlag) {
                case "-e":
                    entityID = Convert.ToInt64(args.PopWord());
                    break;
            }
        }
        if (entityID == 0) {
            Main.API.SendMessage(player, groupId, "Could not convert to proper entity ID. Proper usage: /tpentityid -e ENTITYID", EnumChatType.CommandError);
            return;
        }
        if (!Main.API.World.LoadedEntities.ContainsKey(entityID)) {
            Main.API.SendMessage(player, groupId, "No entity found with that ID. It may not be loaded, or you entered the ID in wrong.", EnumChatType.CommandError);
            return;
        }

        Entity entityToTP = Main.API.World.LoadedEntities[entityID];
        entityToTP.TeleportTo(player.Entity.Pos.XYZ);
        Main.API.SendMessage(player, groupId, "Teleported entity with ID " + entityID + " to your position.", EnumChatType.CommandSuccess);
    }

    private void OnTagBoatCreator(IServerPlayer player, int groupId, CmdArgs args) {
        string? playerName = null;
        while (args.Length > 0) {
            string argFlag = args.PopWord();
            switch (argFlag) {
                case "-p":
                    playerName = args.PopWord();
                    break;
            }
        }
        if (playerName == null) {
            Main.API.SendMessage(player, groupId, "You must specify a player's username. Correct usage: /tagboatcreator -p USERNAME", EnumChatType.CommandError);
            return;
        }

        IPlayer? playerFound = Main.API.World.AllPlayers.FirstOrDefault(x => x.PlayerName == playerName, null);
        if (playerFound == null) {
            Main.API.SendMessage(player, groupId, "No player with that username found in server data. Cannot retrieve UID. Did you enter in the right username?", EnumChatType.CommandError);
            return;
        }

        EntitySelection entSel = player.CurrentEntitySelection;
        if (entSel == null) {
            Main.API.SendMessage(player, groupId, "You must look at a boat to tag it.", EnumChatType.CommandError);
            return;
        }

        if (entSel.Entity is EntityBoat boat) {
            boat.WatchedAttributes.SetString("createdByPlayername", playerFound.PlayerName);
            boat.WatchedAttributes.SetString("createdByPlayerUID", playerFound.PlayerUID);
            Main.API.SendMessage(player, groupId, "Boat successfully tagged as created by " + playerFound.PlayerName + "!", EnumChatType.CommandSuccess);
        }
        else {
            Main.API.SendMessage(player, groupId, "That entity is not a boat.", EnumChatType.CommandError);
        }
    }

    private void ForceGroupPlayerAction(IServerPlayer player, int groupId, CmdArgs args, Func<IServerPlayer, string, ServerySystemPlayerGroups, ServerPlayerData, int, ServerMain, PlayerGroup, string, bool> action) {
        string? playerName = null;
        string? groupName = null;
        while (args.Length > 0) {
            string argFlag = args.PopWord();
            switch (argFlag) {
                case "-p":
                    playerName = args.PopWord();
                    break;
                case "-g":
                    groupName = args.PopWord();
                    break;
            }
        }
        if (playerName == null || groupName == null) {
            Main.API.SendMessage(player, groupId, "You must specify a player's username and a group name. Correct usage: /forcegroupop -p USERNAME -g GROUPNAME", EnumChatType.CommandError);
            return;
        }

        ServerMain server = Traverse.Create(player).Field("server").GetValue<ServerMain>();
        ServerSystem[] serverSystems = Traverse.Create(server).Field("Systems").GetValue<ServerSystem[]>();
        ServerySystemPlayerGroups playerGroups = serverSystems.OfType<ServerySystemPlayerGroups>().FirstOrDefault();

        var getGroupIDFromNameMethod = AccessTools.Method(playerGroups.GetType(), "GetgroupId");
        int forceGroupID = (int)getGroupIDFromNameMethod.Invoke(playerGroups, new object[] {
            groupName
        });
        if (forceGroupID <= 0) {
            Main.API.SendMessage(player, groupId, "That group name does not exist.", EnumChatType.CommandError);
            return;
        }

        PlayerGroup forceGroup = playerGroups.PlayerGroupsByUid[forceGroupID];
        string ownerUID = forceGroup.OwnerUID;

        foreach (ServerPlayerData playerData in server.PlayerDataManager.PlayerDataByUid.Values) {
            if (playerData.LastKnownPlayername != playerName)
                continue;

            if (action(player, groupName, playerGroups, playerData, forceGroupID, server, forceGroup, ownerUID))
                return;
        }

        Main.API.SendMessage(player, groupId, "Could not find player with username " + playerName + ".", EnumChatType.CommandError);
    }
    private void OnForceGroupOP(IServerPlayer player, int groupId, CmdArgs args) {
        ForceGroupPlayerAction(player, groupId, args, (callingPlayer, groupName, playerGroups, playerData, forceGroupID, server, forceGroup, ownerUID) => {
            EnumPlayerGroupMemberShip membership = playerGroups.GetGroupMemberShip(playerData.PlayerUID, forceGroupID).Level;
            if (membership == EnumPlayerGroupMemberShip.None) {
                Main.API.SendMessage(callingPlayer, groupId, "That player is not a part of that group.", EnumChatType.CommandError);
                return true;
            }
            if (membership == EnumPlayerGroupMemberShip.Op || membership == EnumPlayerGroupMemberShip.Owner) {
                Main.API.SendMessage(callingPlayer, groupId, "That player is already an OP, or the owner.", EnumChatType.CommandError);
                return true;
            }

            playerData.PlayerGroupMemberShips[forceGroupID].Level = EnumPlayerGroupMemberShip.Op;
            server.PlayerDataManager.playerDataDirty = true;

            Main.API.SendMessage(callingPlayer, groupId, "Successfully made " + playerData.LastKnownPlayername + " an OP of group " + groupName + "!", EnumChatType.CommandSuccess);
            return true;
        });
    }
    private void OnForceGroupOwner(IServerPlayer player, int groupId, CmdArgs args) {
        ForceGroupPlayerAction(player, groupId, args, (callingPlayer, groupName, playerGroups, playerData, forceGroupID, server, forceGroup, ownerUID) => {
            EnumPlayerGroupMemberShip membership = playerGroups.GetGroupMemberShip(playerData.PlayerUID, forceGroupID).Level;
            if (membership == EnumPlayerGroupMemberShip.None) {
                Main.API.SendMessage(callingPlayer, groupId, "That player is not a part of that group.", EnumChatType.CommandError);
                return true;
            }
            if (membership == EnumPlayerGroupMemberShip.Owner) {
                Main.API.SendMessage(callingPlayer, groupId, "That player is already the owner.", EnumChatType.CommandError);
                return true;
            }

            // Make new player the owner
            playerData.PlayerGroupMemberShips[forceGroupID].Level = EnumPlayerGroupMemberShip.Owner;
            forceGroup.OwnerUID = playerData.PlayerUID;

            // Make old owner an OP
            server.PlayerDataManager.PlayerDataByUid[ownerUID].PlayerGroupMemberShips[forceGroupID].Level = EnumPlayerGroupMemberShip.Op;

            server.PlayerDataManager.playerDataDirty = true;

            Main.API.SendMessage(callingPlayer, groupId, "Successfully made " + playerData.LastKnownPlayername + " owner of group " + groupName + "!", EnumChatType.CommandSuccess);
            return true;
        });
    }
}
