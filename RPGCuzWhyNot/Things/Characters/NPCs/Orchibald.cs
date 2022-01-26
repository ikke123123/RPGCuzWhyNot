using RPGCuzWhyNot.Systems;
using RPGCuzWhyNot.Systems.AttackSystem;
using RPGCuzWhyNot.Systems.Data;
using RPGCuzWhyNot.Systems.Data.Prototypes;
using RPGCuzWhyNot.Systems.HealthSystem;
using RPGCuzWhyNot.Systems.Inventory;
using RPGCuzWhyNot.Things.Characters.Races.Humanoids;
using RPGCuzWhyNot.Things.Item;
using RPGCuzWhyNot.Utilities;

namespace RPGCuzWhyNot.Things.Characters.NPCs
{
	[UniqueNpc("orchibald")]
	public class Orchibald : NPC, IWieldable
	{
		private const int voiceFrequency = 400;

		public int HandsRequired { get; set; } = 1;
		public Requirements UsageRequirements { get; set; }

		public virtual string ListingWithStats => WieldableExt.DefaultListingWithStats(this);

		public string DescriptionInInventory => "Orchibald";

		public string DescriptionOnGround => "Orchibald";

		public IInventory ContainedInventory { get; set; }
		public ItemPrototype Prototype { get; set; }
		public ItemAction[] ItemActions { get; set; }



		public Orchibald() : base(new Dwarf(Humanoid.Gender.Male))
		{
			health = new Health(100);

			ItemActions = new ItemAction[]
			{
				new ItemAction(this, true, new string[]{ "orchibald" }, "The GREAT Orchibald", "He's truly great, I swear.", "Hmmm, using Orchibald aren't we?", new Requirements(new Stats(10, 10, 10, 10), new System.Collections.Generic.Dictionary<string, int>()), new Effects(new Stats(10, 10, 10, 10), 100, 0, 0.5f, true, new System.Collections.Generic.Dictionary<string, int>(), null, new System.Collections.Generic.Dictionary<string, Effects.ItemTransferEntry>(), 0, 0))
			};
		}

		public override void Converse(Character character, string response)
		{
			Terminal.Write("Hello", voiceFrequency, 50);
			Terminal.WriteLine(".....", voiceFrequency, 10);
			Utils.Sleep(1000);
			Terminal.WriteLine("Anyways, i wasn't being suspicious at all just now...", voiceFrequency);
			Utils.Sleep(200);
			Terminal.WriteLine("Just so you know *cough*", voiceFrequency);
		}

		public override void DoTurn(Fight fight)
		{
			foreach (Character combatant in fight.Combatants)
			{
				if (combatant == this) continue;

				combatant.health.TakeDamage(20, this); //temp
			}
		}

		public override bool WantsToHarm(Character character)
		{
			return true;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}

