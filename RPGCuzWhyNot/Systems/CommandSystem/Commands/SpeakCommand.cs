using System;
using RPGCuzWhyNot.Things;
using RPGCuzWhyNot.Things.Characters;
using RPGCuzWhyNot.Things.Characters.NPCs;

namespace RPGCuzWhyNot.Systems.CommandSystem.Commands {
	public class SpeakCommand : Command {
		public override string[] CallNames { get; } = {"speak", "talk", "converse"};
		public override string HelpText { get; } = "Begin a conversation with someone";

		public override void Execute(CommandArguments args) {
			if (args.FirstArgument == "") {
				Terminal.WriteLine($"{args.CommandName} with who?");
				return;
			}

			string callName = args.FirstArgument;
			if (NumericCallNames.Get(callName, out Character conversationPartner)
			|| Program.player.location.GetCharacterByCallName(callName, out conversationPartner)) {
				Terminal.WriteLine($"{{fg:Cyan}}(A conversation with [{conversationPartner.Name}] has begun:)");

				if (conversationPartner is NPC npc)
				{
					npc.Converse(Program.player, "I want to say...");
				}
			} else {
				Terminal.WriteLine("Who now?");
			}
		}
	}
}