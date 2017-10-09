#region ================= Namespaces

using System.Windows.Media;

#endregion

namespace mxd.SQL2.Items
{
	public class SkillItem : AbstractItem
	{
		#region ================= Default items

		public static readonly SkillItem Default = new SkillItem(NAME_DEFAULT, NAME_DEFAULT, true);
		public static readonly SkillItem Random = new SkillItem(NAME_RANDOM, NAME_RANDOM);

		#endregion

		#region ================= Variables

		protected override ItemType type => ItemType.SKILL;

		#endregion

		#region ================= Constructor

		public SkillItem(string title, string value, bool isdefault = false, bool isnightmare = false) : base(title, value)
		{
			this.isdefault = isdefault;
			if(isdefault) this.value = NAME_DEFAULT;
			if(isnightmare) foreground = Brushes.DarkRed;
		}

		#endregion
	}
}
