﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace RPGCuzWhyNot {
	public interface IWearable : IItem {
		int Defense { get; }
		// string WornDescription { get; }
		BodyParts CoverdParts { get; }
		WearableLayers CoverdLayers { get; }
	}

	public static class WearableExt {
		public static bool IsCompatibleWith(this IWearable self, IWearable other) {
			return (self.CoverdLayers & other.CoverdLayers) == 0 || (self.CoverdParts & other.CoverdParts) == 0;
		}

		public static string ListingName(this IWearable self) {
			return $"{self.Name} [{self.CallName}]";
		}

		public static string ListingWithStats(this IWearable self) {
			string name = self.ListingName();
			return self.Defense != 0 ? $"{name} {self.Defense} Defense" : name;
		}

		public static string FancyBitFlagEnum<E>(this E e) where E : Enum => FancyBitFlagEnum(e, out _);

		public static string FancyBitFlagEnum<E>(this E e, out int count) where E : Enum {
			List<string> res = new List<string>();
			foreach (Enum r in Enum.GetValues(typeof(E))) {
				if (e.HasFlag(r)) {
					res.Add(r.ToString());
				}
			}
			count = res.Count;

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < res.Count; ++i) {
				if (i > 0) {
					sb.Append(i < res.Count - 1 ? ", " : "and ");
				}
				sb.Append(res[i]);
			}
			return sb.ToString();
		}
	}
}

