using System;
using System.Collections.Generic;
using System.Linq;
using RPGCuzWhyNot.Primitives;
using RPGCuzWhyNot.Utilities;

namespace RPGCuzWhyNot.Systems.MenuSystem {
	public class Menu {
		private const string shortHands = "123456789abcdefghijklmopqrstuvwxyz";
		private const string unselectedBegin = "  ";
		private const string unselectedEnd = "  ";
		private const string selectedBegin = "> ";
		private const string selectedEnd = " <";
		private const string shortHandPattern = "?. ";
		private const string pathBegin = "{fg:Blue}([)";
		private const string pathSeparator = "{fg:Blue}(->)";
		private const string pathEnd = "{fg:Blue}(])";
		private const string emptyMenuMessage = "{fg:DarkGray}(-- Empty --)";

		public readonly string name;
		public readonly List<MenuItem> items;
		private int lastDrawnIndex = -1;

		private int ItemCount => Math.Max(items.Count, 1);

		private int LongestItemLength {
			get {
				return items.Aggregate(0, (acc, item) => {
					int length = Terminal.GetFormattedLength(item.name);
					return length > acc ? length : acc;
				});
			}
		}

		public int Width {
			get {
				int longestBeginLength = Math.Max(unselectedBegin.Length, selectedBegin.Length);
				int longestEndLength = Math.Max(unselectedEnd.Length, selectedEnd.Length);
				int longestLine = longestBeginLength + LongestItemLength + longestEndLength + shortHandPattern.Length;

				return longestLine;
			}
		}

		public int BaseHeight => ItemCount + 2 + 1; //+2 for the top and bottom borders, +1 for the path display

		public Menu(string name, params MenuItem[] items) {
			this.name = name;
			this.items = items.ToList();
		}

		public void Draw(int selectedIndex, IEnumerable<Menu> path) {
			lastDrawnIndex = selectedIndex;

			Terminal.WriteLineDirect(Stringification.StringifyArray(pathBegin, pathSeparator, pathEnd, path.Select(menu => menu.name).ToArray()));
			Terminal.WriteLineDirect(new string('#', Width));
			if (items.Count > 0) {
				for (int i = 0; i < ItemCount; i++) {
					bool isSelected = i == selectedIndex; //if selectedIndex is null isSelected will go false, hiding the arrows
					char shortHand = i < shortHands.Length ? shortHands[i] : '!';

					Terminal.WriteDirect(isSelected ? selectedBegin : unselectedBegin);
					Terminal.WriteDirect(shortHandPattern.Replace('?', shortHand));
					Terminal.WriteDirect(items[i].name);
					Terminal.WriteDirect(new string(' ', LongestItemLength - Terminal.GetFormattedLength(items[i].name)));
					Terminal.WriteLineDirect(isSelected ? selectedEnd : unselectedEnd);
				}
			} else {
				Terminal.WriteLineDirect(emptyMenuMessage);
			}
			Terminal.WriteLineDirect(new string('#', Width));

			if (selectedIndex < items.Count) {
				Terminal.WriteLine(items[selectedIndex].hoverDescription);
			}
		}

		public void ClearDraw() {
			for (int i = 0; i < BaseHeight; i++) {
				Terminal.ClearLine();
			}

			ClearHoverDescription(false);
		}

		public void ClearHoverDescription(bool offsetToLocation = true) {
			if (offsetToLocation) {
				Terminal.CursorPosition += Vec2.Down * BaseHeight;
			}

			int numOfHoverDescriptionLines = lastDrawnIndex != -1 && lastDrawnIndex < items.Count
				? items[lastDrawnIndex].hoverDescription.Count(c => c == '\n') + 1
				: 0;

			for (int i = 0; i < numOfHoverDescriptionLines; i++) {
				Terminal.ClearLine();
			}
		}

		public void Enter() {
			Stack<Menu> stack = new Stack<Menu>();
			stack.Push(this);

			Enter(stack);
		}

		public void Enter(Stack<Menu> stack) {
			Vec2 drawPos = Terminal.CursorPosition;
			int arrowIndex = 0;
			bool needsRedraw = true;
			MenuEffectContext ctx = new MenuEffectContext(drawPos, stack);

			void OnArrowIndexChange() {
				arrowIndex = ExtraMath.Mod(arrowIndex, items.Count);
				Terminal.CursorPosition = drawPos;
				ClearHoverDescription();
				needsRedraw = true;
			}

			while (stack.Count > 0 && stack.Peek() == this) {
				//draw
				if (needsRedraw) {
					Terminal.CursorPosition = drawPos;
					Draw(arrowIndex, stack.Reverse());
					needsRedraw = false;
				}

				//get input
				ConsoleKeyInfo keyPress = Console.ReadKey(true);

				//try finding and using a short hand
				int shortHandIndex = shortHands.IndexOf(keyPress.KeyChar);
				if (shortHandIndex != -1 && shortHandIndex < items.Count) {
					Terminal.Beep(200, 50);
					items[shortHandIndex].effect(ctx);
					needsRedraw = true;
				} else {
					//else, update the arrow key system
					switch (keyPress.Key) {
						//go up
						case ConsoleKey.UpArrow:
							Terminal.Beep(100, 50);
							arrowIndex--;
							OnArrowIndexChange();
							break;

						//go down
						case ConsoleKey.DownArrow:
							Terminal.Beep(100, 50);
							arrowIndex++;
							OnArrowIndexChange();
							break;

						//go back
						case ConsoleKey.LeftArrow:
						case ConsoleKey.Escape:
						case ConsoleKey.Backspace:
							if (stack.Count > 1) {
								Terminal.Beep(200, 50);
								ctx.ExitMenu();
							} else {
								Terminal.Beep(50, 100);
							}
							break;

						//execute selected menu item
						case ConsoleKey.RightArrow:
						case ConsoleKey.Enter:
							Terminal.Beep(200, 50);
							if (arrowIndex < items.Count) {
								items[arrowIndex].effect(ctx);
								needsRedraw = true;
							}
							break;
					}
				}
			}
		}
	}
}
