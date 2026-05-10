using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace GreenleafAdminCmds;

public class Commands {
    public Commands() {
        Main.API.RegisterCommand("tpback", "Teleports you back to where you were before initiating a successful /tp command.", "", new ServerChatCommandDelegate(this.OnTPBackCommand), Privilege.tp);
        Main.API.RegisterCommand("tpentityid", "Teleports an entity by ID to your position.", "-e ENTITYID", new ServerChatCommandDelegate(this.OnTPEntityID), Privilege.tp);
        Main.API.RegisterCommand("tagboatcreator", "Tags a boat as being created by a certain player's username.", "-p USERNAME", new ServerChatCommandDelegate(this.OnTagBoatCreator), Privilege.ban);
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
}
