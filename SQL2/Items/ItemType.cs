#region ================= Namespaces

using System.Collections.Generic;

#endregion

namespace mxd.SQL2.Items
{
    #region ================= ItemType

    public enum ItemType
    {
        UNKNOWN,
        ENGINE,
        RESOLUTION,
        GAME,
        MOD,
        MAP,
        SKILL,
        CLASS,
        DEMO,
    }

    #endregion

    #region ================= ItemTypes

    public static class ItemTypes
    {
        public static readonly Dictionary<ItemType, string> Types;
        public static readonly Dictionary<string, ItemType> Markers;

        static ItemTypes()
        {
            Types = new Dictionary<ItemType, string>
            {
                { ItemType.ENGINE, "%E" },
                { ItemType.RESOLUTION, "%R"},
                { ItemType.GAME, "%G" },
                { ItemType.MOD, "%M" },
                { ItemType.MAP, "%m" },
                { ItemType.SKILL, "%S" },
                { ItemType.CLASS, "%C" },
                { ItemType.DEMO, "%D" }
            };

            Markers = new Dictionary<string, ItemType>();
            foreach(var group in Types) Markers.Add(group.Value, group.Key);
        }
    }

    #endregion
}
