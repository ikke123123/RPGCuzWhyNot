﻿using System;

namespace RPGCuzWhyNot {
	public class Player : Character {
		public override void TakeDamage(int damage, Character source) {
			damage = Math.Min(health, damage);
			health -= damage;

			Console.WriteLine($"{source.name} hit you for {damage} damage.");

			if (health <= 0) {
				Console.WriteLine($"{source.name} killed you.");
				// TODO: Die
			}
		}
		
		public void ReactToCommand(string[] args) {
			if (args.Length >= 1) {
				switch (args[0].ToLower()) {
					case "go":
					case "goto":
					case "enter":
						if (args.Length >= 2) {
		                    Location newLocation = Program.world.GetLocationByCallName(args[1]);
		                    if (newLocation != null) {
			                    Console.WriteLine(TryGoto(newLocation) ? "success!" : "can't reach location from here");
		                    } else {
			                    Console.WriteLine("Location not found, does it exist?");
                            }
	                    } else {
		                    Console.WriteLine("No location specified");
	                    }
						break;

					case "where":
						Console.WriteLine($"You are in: {location}");
						break;

					case "ls":
					case "list":
					case "locations":
						Console.WriteLine("Locations:");
						foreach (Location loc in location.Paths) {
							Console.Write(loc == location ? "* " : "  ");
							Console.WriteLine(loc);
						}

						break;

                    case "equip":
	                    if (args.Length >= 2) {
							throw new NotImplementedException();

		                    //Todo: use args[1] to get the item
                            Item item = null;
                            if (item != null) {
	                            if (TryEquip(item)) {
		                            Console.WriteLine("success");
	                            }
                            } else {
								Console.WriteLine("Item not found, does it exist?");
                            }
                        } else {
		                    Console.WriteLine("No item specified");
                        }
						break;

                    default:
						Console.WriteLine("Invalid command");
						break;
                }
            } else {
				Console.WriteLine("No command");
            }
		}

		private bool TryEquip(Item item) {
			throw new NotImplementedException();
        }

		private bool TryGoto(Location newLocation) {
			if (location.HasPathTo(newLocation)) {
				location = newLocation;
				location.PrintInformation();
				return true;
			}

			return false;
		}
	}
}