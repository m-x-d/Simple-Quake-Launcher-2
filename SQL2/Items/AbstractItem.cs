#region ================= Namespaces

using mxd.SQL2.Games;

#endregion

namespace mxd.SQL2.Items
{
	public abstract class AbstractItem
	{
		#region ================= Constants

		protected const string NAME_RANDOM = "[Random]";
		protected const string NAME_DEFAULT = "[Default]";
		protected const string NAME_NONE = "[None]";

		#endregion

		#region ================= Variables

		protected string value; // e1m1
		protected string argument; // +map "e1 m1"
		protected string argumentpreview; // +map ???
		protected string title; // e1m1 | The Underhalls
		protected string param; // +map {0}

		protected bool israndom;
		protected bool isdefault;

		#endregion

		#region ================= Properties

		public abstract ItemType Type { get; }

		public virtual string Value => value;       // e1m1
		public virtual string Argument => (israndom ? GetArgument(GameHandler.Current.GetRandomItem(Type)) : argument); // +map "e1 m1"
		public virtual string ArgumentPreview => argumentpreview; // +map ???
		public virtual string Title => title;       // e1m1 | The Underhalls

		public bool IsRandom => israndom;
		public bool IsDefault => isdefault;

		#endregion

		#region ================= Constructor

		protected AbstractItem(string title, string value)
		{
			this.israndom = (title == NAME_RANDOM);
			this.isdefault = (title == NAME_DEFAULT || title == NAME_NONE);

			this.title = title;
			this.value = GetSafeValue(value.ToLowerInvariant());
			this.param = GameHandler.Current.LaunchParameters[Type];
			this.argument = GetArgument(this.value);
			this.argumentpreview = GetArgument(israndom ? "???" : this.value);
		}

		#endregion

		#region ================= Methods

		protected virtual string GetArgument(string val)
		{
			return (!string.IsNullOrEmpty(param) ? string.Format(param, GetSafeValue(val)) : val);
		}

		protected static string GetSafeValue(string val)
		{
			return (val.Contains(" ") ? "\"" + val + "\"" : val);
		}

		public override string ToString()
		{
			return title;
		}

		#endregion
	}
}
