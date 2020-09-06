using System;

namespace RPGCuzWhyNot.Utilities {
	public static class ExtraMath {
		public static float Clamp(float value, float min, float max) {
			return Math.Min(Math.Max(value, min), max);
		}
	}
}