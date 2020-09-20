using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RPGCuzWhyNot.AttackSystem;
using RPGCuzWhyNot.Inventory.Item;
using RPGCuzWhyNot.Systems;
using RPGCuzWhyNot.Systems.Commands;
using RPGCuzWhyNot.Systems.HealthSystem;
using RPGCuzWhyNot.Systems.Inventory;
using RPGCuzWhyNot.Things.Characters.Races;
using RPGCuzWhyNot.Things.Item;
using RPGCuzWhyNot.Utilities;

namespace RPGCuzWhyNot.Things.Characters {
	public class Player : Character, IHasItemInventory, ICanWear, ICanWield {
		public ItemInventory Inventory { get; }
		public WearablesInventory Wearing { get; }
		public WieldablesInventory Wielding { get; }
		private readonly PlayerCommands commands;

		public Player(Race race) : base(race) {
			Inventory = new ItemInventory(this);
			Wearing = new WearablesInventory(this);
			Wielding = new WieldablesInventory(this, 2);

			//init health
			health = new Health(100);
			health.OnDamage += ctx => {
				Terminal.WriteLine($"{ctx.inflictor} hit you for {ctx.Delta} damage");
			};
			health.OnDeath += ctx => {
				Terminal.WriteLine($"{ctx.inflictor} killed you!");
			};

			commands = new PlayerCommands(this);
			commands.LoadCommands();
		}

		bool IHasInventory.ContainsCallName(string callName, out IItem item) => Inventory.ContainsCallName(callName, out item);
		bool IHasInventory.MoveItem(IItem item, bool silent) => Inventory.MoveItem(item, silent);

		IEnumerator<IItem> IEnumerable<IItem>.GetEnumerator() => Inventory.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Inventory).GetEnumerator();

		public override PlanOfAction PlanTurn(params Character[] opponents) {
			PlanOfAction planOfAction = new PlanOfAction(stats);

			//planning phace
			bool isDonePlanningTurn = false;
			Command confirm = new Command(new[] { "confirm", "done", "apply", "execute" }, "Confirm your actions and procced to the next turn.", args => {
				isDonePlanningTurn = true;
			});
			Command undo = new Command(new[] { "undo", "revert" }, "Remove the last move you planned to do from the plan of action.", args => {
				if (planOfAction.plannedActions.Count > 0) {
					IPlannableAction last = planOfAction.plannedActions.Last();
					planOfAction.plannedActions.Remove(last);
					Terminal.WriteLine($"Removed action [{last.Name}] from plan.");
				} else {
					Terminal.WriteLine("You've got no plan of action. There's nothing to regret...");
				}
			});
			Command run = new Command(new[] { "run", "run away" }, "Run away from the fight.", args => {
				Terminal.WriteLine("You run away from the enemy!");
				Program.ExitCombat();
			});
			Command plan = new Command(new[] { "ls", "list", "plan" }, "Remove the last move you planned to do from the plan of action.", args => {
				if (planOfAction.plannedActions.Count > 0) {
					Terminal.WriteLine("{fg:Cyan}(Plan of action:)");
					foreach (IPlannableAction action in planOfAction.plannedActions) {
						Terminal.WriteLine($" - {action.Name}");
					}
				} else {
					Terminal.WriteLine("You've got no plan!");
				}
			});

			while (!isDonePlanningTurn) {
				//find valid commands
				CommandHandler handler = new CommandHandler();
				handler.AddCommand(confirm);
				handler.AddCommand(undo);
				handler.AddCommand(plan);
				handler.AddCommand(run);
				handler.AddCommand(new Command(new[] { "help", "commands" }, "Show this list", args => {
					Terminal.WriteLine("Commands:");
					handler.DisplayHelp();
				}));

				foreach (IWieldable wieldable in Wielding) {
					handler.AddCommand(new Command(new[] { wieldable.CallName }, "Do something with this item.", args => {
						if (args.FirstArgument == "") {
							Terminal.WriteLine("Do what with it?");
							return;
						}

						CommandHandler itemHandler = new CommandHandler();

						foreach (ItemAction itemAction in wieldable.ItemActions) {
							itemHandler.AddCommand(new Command(itemAction.CallNames, itemAction.Description, itemArgs => {
								planOfAction.plannedActions.Add(itemAction);
								Terminal.WriteLine($"Added action [{itemAction.Name}] to plan.");
							}));
						}

						itemHandler.AddCommand(new Command(new[] { "help" }, "Get help for this item.", args => {
							Terminal.WriteLine("Actions:");
							if (wieldable.ItemActions.Any()) {
								itemHandler.DisplayHelp();
							} else {
								Terminal.WriteLine("There's no actions for this item.");
							}
						}));

						if (!itemHandler.TryHandle(args.FirstArgument)) {
							Terminal.WriteLine("No such action exists for this item.");
						}
					}));
				}

				//get next command
				Terminal.WriteLine();
				Terminal.WriteLine($"Points Left: {planOfAction.BudgetLeft}");
				string commandText = ConsoleUtils.Ask("|> ").ToLower();
				Terminal.WriteLine();
				if (!handler.TryHandle(commandText)) {
					Terminal.WriteLine("I don't understand.");
				}
			}
			Terminal.WriteLine("Now Executeing actions...");

			return planOfAction;
		}

		public void Handle(string message) {
			commands.Handle(message);
		}
	}
}

