#region ================= Namespaces

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using mxd.SQL2.Data;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Controls
{
	public class PreviewTextBox : RichTextBox
	{
		#region ================= Variables

		private const string spacer = "  ";
		private readonly Paragraph paragraph;

		#endregion

		#region ================= Constructor

		public PreviewTextBox()
		{
			paragraph = (Paragraph)this.Document.Blocks.FirstBlock;

			// Bind events
			DataObject.AddPastingHandler(this, OnPaste);
			this.PreviewKeyDown += OnPreviewKeyDown;
			this.KeyUp += OnKeyUp;
			this.SelectionChanged += OnSelectionChanged;
			this.MouseUp += OnMouseUp;
		}

		#endregion

		#region ================= Arguments handling

		public void SetArguments(Dictionary<ItemType, AbstractItem> args, bool clearcustomargs = false)
		{
			// Store and reset data
			if(clearcustomargs)
				Configuration.ExtraArguments.Clear();
			else
				StoreCustomArguments();

			// Create runs
			var inlines = new List<PreviewRun>();
			foreach(var group in args)
			{
				var arg = group.Value;
				var argtype = group.Key;

				// Add arg?
				if(arg != null && !arg.IsDefault)
					inlines.Add(new PreviewRun(arg.ArgumentPreview, arg, argtype, false));

				// Add custom arg? Add when extraarg is not empty
				if(Configuration.ExtraArguments.ContainsKey(argtype))
					inlines.Add(new PreviewRun(" " + Configuration.ExtraArguments[argtype] + " ", arg, argtype, true));
			}

			// No data? (unlikely to happen)
			if(inlines.Count == 0)
			{
				paragraph.Inlines.Clear();
				return;
			}

			// Add spacers...
			PreviewRun previous = null;
			var inlineswithspacers = new List<PreviewRun>();
			foreach(var current in inlines)
			{
				// Insert spacer between non - editable runs
				if(!current.IsEditable && previous != null && !previous.IsEditable)
					inlineswithspacers.Add(new PreviewRun(spacer, previous.Item, previous.ItemType, true));

				// Add current arg
				inlineswithspacers.Add(current);

				// Store previous arg
				previous = current;
			}

			// Add spacer after the last arg?
			var lastarg = inlines[inlines.Count - 1];
			if(!lastarg.IsEditable) inlineswithspacers.Add(new PreviewRun(spacer, lastarg.Item, lastarg.ItemType, true));

			// Clear current text
			paragraph.Inlines.Clear();

			// Set new text
			paragraph.Inlines.AddRange(inlineswithspacers);
		}

		// Command line without engine name
		public string GetCommandLine()
		{
			if(paragraph.Inlines.Count == 0) return string.Empty;

			StoreCustomArguments();

			var result = new List<string>();
			foreach(PreviewRun run in paragraph.Inlines)
			{
				// Custom args
				if(run.IsEditable)
				{
					string text = run.Text.Trim();
					if(!string.IsNullOrEmpty(text)) result.Add(text);
				}
				else // Pre-generated args
				{
					// Skip engine arg
					if(run.ItemType != ItemType.ENGINE) result.Add(run.Item.Argument);
				}
			}

			return string.Join(" ", result);
		}

		private void StoreCustomArguments()
		{
			if(paragraph.Inlines.Count == 0) return;

			// Get custom args from current text...
			foreach(PreviewRun run in paragraph.Inlines)
			{
				if(run.IsEditable)
				{
					// Don't store empty strings
					string text = run.Text.Trim();
					if(string.IsNullOrEmpty(text))
						Configuration.ExtraArguments.Remove(run.ItemType);
					else
						Configuration.ExtraArguments[run.ItemType] = text;
				}
			}
		}

		#endregion

		#region ================= Text handling methods

		private List<PreviewRun> GetSelectedRuns(TextSelection selection)
		{
			var result = new List<PreviewRun>();
			if(paragraph.Inlines.Count > 0)
			{
				var startrun = (PreviewRun)selection.Start.Parent;
				result.Add(startrun);

				// Add the rest of the runs, if necessary
				if(!selection.IsEmpty)
				{
					var endrun = selection.End.Parent as PreviewRun;
					if(startrun != endrun)
					{
						var startfound = false;
						foreach(PreviewRun run in paragraph.Inlines)
						{
							// Skip until startrun...
							if(!startfound && !run.Equals(startrun)) continue;

							if(!startfound) // First run was already added, so skip it as well...
							{
								startfound = true;
								continue;
							}

							result.Add(run);
							if(run.Equals(endrun)) break;
						}
					}
				}
			}

			return result;
		}

		#endregion

		#region ================= Events

		private void OnPreviewKeyDown(object sender, KeyEventArgs e)
		{
			if(this.IsReadOnly) return; // Can't be edited
			
			// Delete and Backspace require special handling...
			if(e.Key == Key.Delete || e.Key == Key.Back)
			{
				// Ignore Delete when at the end of editable run, ignore Backspace when at the start of editable run
				var currun = (PreviewRun)this.Selection.Start.Parent;
				var targetpos = (e.Key == Key.Delete ? currun.ContentEnd : currun.ContentStart);
				if(this.Selection.IsEmpty && this.Selection.Start.GetOffsetToPosition(targetpos) == 0)
				{
					e.Handled = true;
					return;
				}

				// Don't allow to completely delete editable runs
				if(currun.Text.Length < 2) e.Handled = true;
			}
		}

		private void OnKeyUp(object sender, KeyEventArgs e)
		{
			var runs = GetSelectedRuns(this.Selection);
			if(runs.Count == 1)
			{
				var currun = (PreviewRun)this.Selection.Start.Parent;

				// Move selection forward when at the end of non-editable run and next run is editable...
				if(e.Key == Key.Back && this.Selection.IsEmpty && !currun.IsEditable && currun.NextInline != null
					&& ((PreviewRun)currun.NextInline).IsEditable && ((PreviewRun)currun.NextInline).Text.Length > 0
					&& this.Selection.Start.GetOffsetToPosition(currun.ContentEnd) == 0)
				{
					var nextsel = currun.ContentEnd.GetPositionAtOffset(1, LogicalDirection.Forward);
					this.Selection.Select(nextsel, nextsel);
					currun = (PreviewRun)currun.NextInline;
				} 
				// Move selection backward when at the start of non-editable run and previous run is editable...
				else if(e.Key == Key.Delete && this.Selection.IsEmpty && !currun.IsEditable && currun.PreviousInline != null
					&& ((PreviewRun)currun.PreviousInline).IsEditable && ((PreviewRun)currun.PreviousInline).Text.Length > 0
					&& this.Selection.Start.GetOffsetToPosition(currun.ContentStart) == 0)
				{
					var prevsel = currun.ContentStart.GetPositionAtOffset(-1, LogicalDirection.Backward);
					this.Selection.Select(prevsel, prevsel);
					currun = (PreviewRun)currun.PreviousInline;
				}

				// Add some padding to editable runs...
				if(currun.IsEditable)
				{
					var text = currun.Text;
					if(text.Length < 2)
					{
						currun.Text = "  ";
						var newsel = currun.ContentStart.GetPositionAtOffset(1, LogicalDirection.Forward);
						this.Selection.Select(newsel, newsel);
						return;
					}

					bool startspaceneeded = !text.StartsWith(" ");
					bool endspaceneeded = !text.EndsWith(" ");
					if(startspaceneeded || endspaceneeded)
					{
						int curoffset = currun.ContentStart.GetOffsetToPosition(this.Selection.Start);
						if(startspaceneeded) curoffset += 1;
						currun.Text = (startspaceneeded ? " " : "") + text + (endspaceneeded ? " " : "");
						var newsel = currun.ContentStart.GetPositionAtOffset(curoffset, LogicalDirection.Forward);
						this.Selection.Select(newsel, newsel);
					}
				}
			}
		}

		// Clear custom args on MMB click
		private void OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			if(e.ChangedButton != MouseButton.Middle) return;

			// Editable run clicked?
			var run = this.GetPositionFromPoint(e.GetPosition(this), true)?.Parent as PreviewRun;
			if(run == null || !run.IsEditable || run.Text == spacer) return;

			// Update text
			run.Text = spacer;

			// Update selection
			var newsel = run.ContentStart.GetPositionAtOffset(1, LogicalDirection.Forward);
			this.Selection.Select(newsel, newsel);

			// Update stored args
			StoreCustomArguments();
		}

		// Toggle textbox editability based on selection
		private void OnSelectionChanged(object sender, RoutedEventArgs e)
		{
			var runs = GetSelectedRuns(this.Selection);

			// Prevent editing when non-editable runs are selected...
			foreach(var run in runs)
			{
				if(!run.IsEditable)
				{
					this.IsReadOnly = true;
					return;
				}
			}

			this.IsReadOnly = false;
		}

		private void OnPaste(object sender, DataObjectPastingEventArgs e)
		{
			// Native implementation creates a copy of PreviewRun using parameterless constructor, without needed properties, so cancel that...
			e.CancelCommand();

			var runs = GetSelectedRuns(this.Selection);
			if(runs.Count != 1 || !runs[0].IsEditable) return;

			string pasted = Clipboard.GetText();
			if(!string.IsNullOrEmpty(pasted))
			{
				// Delete selected text
				if(!this.Selection.IsEmpty)
					this.Selection.Start.DeleteTextInRun(this.Selection.Start.GetOffsetToPosition(this.Selection.End));

				// Instert text manually...
				this.Selection.Start.InsertTextInRun(pasted);
			}
		}

		#endregion
	}
}
