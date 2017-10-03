namespace mxd.SQL2.Items
{
    public class SkillItem : AbstractItem
    {
        #region ================= Default items

        public static readonly SkillItem Default = new SkillItem(NAME_DEFAULT, NAME_DEFAULT, true);
        public static readonly SkillItem Random = new SkillItem(NAME_RANDOM, NAME_RANDOM);

        #endregion

        #region ================= Properties

        public override ItemType Type => ItemType.SKILL;

        #endregion

        #region ================= Constructor

        public SkillItem(string title, string value, bool isdefault = false) : base(title, value)
        {
            this.isdefault = isdefault;
            if(isdefault) this.value = NAME_DEFAULT;
        }

        #endregion
    }
}
