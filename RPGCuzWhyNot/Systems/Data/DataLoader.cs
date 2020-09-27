using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGCuzWhyNot.Systems.Data.JsonConverters;
using RPGCuzWhyNot.Systems.Data.Prototypes;
using RPGCuzWhyNot.Things;
using RPGCuzWhyNot.Things.Characters.NPCs;
using RPGCuzWhyNot.Things.Item;

namespace RPGCuzWhyNot.Systems.Data {
	public static class DataLoader {
		private static readonly string dataPath = "GameData" + Path.DirectorySeparatorChar;
		private static readonly string locationsPath = dataPath + "location";
		private static readonly string itemsPath = dataPath + "item";
		private static readonly string npcsPath = dataPath + "npc";

		private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions {
			ReadCommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true,
			Converters = {
				new JsonStringEnumConverter(),
				new JsonMaybeListConverter()
			}
		};

		private static Dictionary<string, Prototype> prototypes;
		private static Dictionary<string, Location> locations;
		private static Dictionary<string, NPC> npcs;
		private static Dictionary<string, Type> npcTypeMap = new Dictionary<string, Type>();

		public static ReadOnlyDictionary<string, Prototype> Prototypes { get; private set; }
		public static ReadOnlyDictionary<string, Location> Locations { get; private set; }
		public static ReadOnlyDictionary<string, NPC> NPCs { get; private set; }

		private static bool loadError;

		static DataLoader() {
			FindRegisteredNPCs();
		}

		/// <summary>
		/// Load all the game data files into the registry.
		/// </summary>
		public static bool LoadGameData() {
			loadError = false;

			prototypes = new Dictionary<string, Prototype>();
			Prototypes = new ReadOnlyDictionary<string, Prototype>(prototypes);
			LoadPrototypesFromPath<ItemPrototype>(itemsPath);
			LoadPrototypesFromPath<LocationPrototype>(locationsPath);
			LoadPrototypesFromPath<NpcPrototype>(npcsPath);

			ValidatePrototypes();

			locations = new Dictionary<string, Location>();
			Locations = new ReadOnlyDictionary<string, Location>(locations);

			// Create the locations.
			foreach (LocationPrototype prototype in prototypes.Values.OfType<LocationPrototype>()) {
				Location location = prototype.Create();
				locations.Add(prototype.Id, location);
			}

			SetupLocations();

			npcs = new Dictionary<string, NPC>();
			NPCs = new ReadOnlyDictionary<string, NPC>(npcs);

			// Create the NPCs.
			foreach (NpcPrototype proto in prototypes.Values.OfType<NpcPrototype>()) {
				NPC npc = CreateNpc(proto);
				if (npc != null) npcs.Add(proto.Id, npc);
			}

			return !loadError;
		}

		/// <summary>
		/// Construct an item from the prototype registry.
		/// </summary>
		/// <param name="id">The id of the item</param>
		public static IItem CreateItem(string id) {
			if (prototypes.TryGetValue(id, out Prototype prototype) && prototype is ItemPrototype itemPrototype)
				return itemPrototype.Create();

			throw new Exception($"An item prototype with the id '{id}' was not found.");
		}

		/// <summary>
		/// Get an existing location from the registry.
		/// </summary>
		/// <param name="id">The id of the location.</param>
		public static Location GetLocation(string id) {
			if (!locations.TryGetValue(id, out Location location))
				throw new Exception($"A location with the id '{id} was not found.");

			return location;
		}

		private static void FindRegisteredNPCs() { // Find all registered NPCs (those marked with UniqueNpcAttribute).
			foreach (TypeInfo type in typeof(NPC).Assembly.DefinedTypes) {
				UniqueNpcAttribute attribute = type.GetCustomAttribute<UniqueNpcAttribute>();
				if (attribute != null) {
					if (!typeof(NPC).IsAssignableFrom(type)) {
						Error($"UniqueNpcAttribute used on a non NPC type '{type}'.");
						continue;
					}

					if (type.GetConstructor(Array.Empty<Type>()) == null) {
						Error($"Type marked with UniqueNpcAttribute '{type}' does not have a public parameterless constructor.");
						continue;
					}

					if (!npcTypeMap.TryAdd(attribute.Id, type)) {
						Error($"Duplicate UniqueNpcAttribute with id '{attribute.Id}' on type '{type}'.");
						continue;
					}
				}
			}
		}

		private static NPC CreateNpc(NpcPrototype proto) {
			if (!npcTypeMap.TryGetValue(proto.Id, out Type type))
				return null;

			if (!locations.TryGetValue(proto.Location, out Location location))
				return null;

			NPC npc = (NPC)Activator.CreateInstance(type);
			if (npc == null) {
				Debug.Assert(false);
				return null;
			}

			npc.Name = proto.Name;
			npc.CallName = proto.CallName;
			npc.location = location;
			location.AddNPC(npc, proto.GlanceDescription, proto.ApproachDescription);

			return npc;
		}

		private static void LoadPrototypesFromPath<TProto>(string path) where TProto : Prototype {
			string[] dataFiles = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);

			foreach (string filePath in dataFiles) {
				// Deserialize the prototype(s).
				List<TProto> prototypeList;
				try {
					string fileContent = File.ReadAllText(filePath);
					prototypeList = JsonSerializer.Deserialize<List<TProto>>(fileContent, serializerOptions);
				}
				catch (IOException e) {
					Error($"Failed to read file \"{filePath}\":\n{e.Message}");
					continue;
				}
				catch (JsonException e) {
					Error($"Failed to deserialize file \"{filePath}\":\n{e.Message}");
					continue;
				}

				// Add the new prototypes.
				foreach (TProto proto in prototypeList) {
					proto.DataFilePath = filePath;
					((IOnDeserialized)proto).OnDeserialized();

					if (proto.Id == null) {
						MissingPropertyError(proto, "id");
						continue;
					}

					if (!prototypes.TryAdd(proto.Id, proto))
						Error($"Duplicate prototype definition of '{proto.Id}' in file \"{proto.DataFilePath}\".");
				}
			}
		}

		private static void SetupLocations() {
			foreach ((string id, Location location) in locations) {
				LocationPrototype locationPrototype = location.Prototype;

				// Add all the paths to the location.
				foreach ((string pathName, string pathDescription) in locationPrototype.Paths) {
					if (!locations.TryGetValue(pathName, out Location destination)) {
						Error($"Location '{pathName}' not found. Referenced by location '{id}'.");
						continue;
					}

					location.AddPathTo(destination, pathDescription);
				}

				// Create the items in the location.
				foreach (string itemName in locationPrototype.Items) {
					if (!prototypes.TryGetValue(itemName, out Prototype proto) || !(proto is ItemPrototype item)) {
						Error($"Item '{itemName}' not found. Referenced by location '{id}'.");
						continue;
					}

					location.items.MoveItem(item.Create());
				}
			}
		}

		private static void ValidatePrototypes() {
			foreach (Prototype proto in prototypes.Values) {
				if (proto.CallName == null) MissingPropertyError(proto, "callName");
				if (proto.Name == null) MissingPropertyError(proto, "name");

				switch (proto) {
					case LocationPrototype locationPrototype:
						ValidateLocationPrototype(locationPrototype);
						break;

					case ItemPrototype itemPrototype:
						ValidateItemPrototype(itemPrototype);
						break;

					case NpcPrototype npcPrototype:
						ValidateNpcPrototype(npcPrototype);
						break;
				}
			}
		}

		private static void ValidateLocationPrototype(LocationPrototype proto) {
			if (proto.Description == null) MissingPropertyError(proto, "description");

			if (proto.Paths.Count == 0)
				LogWarning($"Location '{proto.Id}' has no paths.");

			if (proto.Paths.ContainsKey(proto.Id))
				Error($"Location '{proto.Id}' contains a path to itself.");
		}

		private static void ValidateItemPrototype(ItemPrototype proto) {
			if (proto.DescriptionInInventory == null) MissingPropertyError(proto, "inventoryDescription");
			if (proto.DescriptionOnGround == null) MissingPropertyError(proto, "groundDescription");

			if (proto.IsWieldable) {
				if (proto.HandsRequired == null) MissingPropertyError(proto, "handsRequired");
			}

			if (proto.IsWearable) {
				if (proto.Defense == null) MissingPropertyError(proto, "defense");
				if (proto.CoveredParts == 0) MissingPropertyError(proto, "coveredParts");
				if (proto.CoveredLayers == 0) MissingPropertyError(proto, "coveredLayers");
			}

			if (proto.HasInventory) {
				if (proto.WeightFraction.numerator == 0 && proto.WeightFraction.denominator == 0)
					MissingPropertyError(proto, "weightFraction");
			}
		}

		private static void ValidateNpcPrototype(NpcPrototype proto) {
			if (proto.Location == null) {
				MissingPropertyError(proto, "location");
			}
			else {
				if (!prototypes.TryGetValue(proto.Location, out Prototype locationPrototype) || !(locationPrototype is LocationPrototype))
					Error($"Unknown location '{proto.Location}' referenced by '{proto.Id}' in file \"{proto.DataFilePath}\".");
			}

			if (proto.GlanceDescription == null) MissingPropertyError(proto, "glanceDescription");
			if (proto.ApproachDescription == null) MissingPropertyError(proto, "approachDescription");

			if (!npcTypeMap.ContainsKey(proto.Id))
				Error($"Unknown NPC '{proto.Id}' in file \"{proto.DataFilePath}\".");
		}


		private static void MissingPropertyError(Prototype prototype, string propertyName) {
			Error($"Missing property '{propertyName}' in prototype '{prototype.Id}', in file \"{prototype.DataFilePath}\".");
		}

		private static void Error(string message) {
			loadError = true;
			LogError(message);
		}

		private static void LogError(string message) {
			Terminal.WriteDirect("{red}([ERROR/Data]) ");
			Terminal.WriteLineDirect(message);
		}

		private static void LogWarning(string message) {
			Terminal.WriteDirect("{yellow}([WARN/Data]) ");
			Terminal.WriteLineDirect(message);
		}
	}
}