#region ================= Namespaces

using System.Windows;
using System.Windows.Documents;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Controls
{
	public class PreviewRun : Run
	{
		#region ================= Variables

		private readonly bool editable;
		private readonly ItemType itemtype;
		private readonly AbstractItem item;

		#endregion

		#region ================= Properties

		public bool IsEditable => editable; // Doesn't actually block text editability...
		public ItemType ItemType => itemtype;
		public AbstractItem Item => item;

		#endregion

		#region ================= Constructor

		public PreviewRun() { }

		public PreviewRun(string text, AbstractItem item, ItemType itemtype, bool editable)
		{
			base.SetValue(Run.TextProperty, text);
			this.itemtype = itemtype;
			this.item = item;
			this.editable = editable;

			if(editable)
			{
				this.SetValue(TextElement.ForegroundProperty, SystemColors.HotTrackBrush);
				this.SetValue(TextElement.BackgroundProperty, SystemColors.GradientInactiveCaptionBrush);
			}
		}

		#endregion

		#region ================= Methods

		public override string ToString()
		{
			return this.Text;
		}

		#endregion
	}
}
