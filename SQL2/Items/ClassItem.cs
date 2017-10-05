namespace mxd.SQL2.Items
{
	public class ClassItem : AbstractItem
	{
		#region ================= Default items

		public static readonly ClassItem Default = new ClassItem(NAME_DEFAULT, NAME_DEFAULT, true);
		public static readonly ClassItem Random = new ClassItem(NAME_RANDOM, NAME_RANDOM);

		#endregion

		#region ================= Properties

		public override ItemType Type => ItemType.CLASS;

		#endregion

		#region ================= Constructors

		public ClassItem(string title, string value, bool isdefault = false) : base(title, value)
		{
			this.isdefault = isdefault;
			if(isdefault) this.value = NAME_DEFAULT;
		}

		#endregion
	}
}
