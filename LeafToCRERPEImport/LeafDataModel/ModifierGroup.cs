﻿using System.Collections.Generic;

namespace LeafToCRERPEImport.LeafDataModel
{
    public class ModifierGroup
    {
        public string id { get; set; }
        public string groupName { get; set; }
        public string groupDesc { get; set; }

        public List<ModifierGroupSubItem> modifier_group_sub_items { get; set; }
        public ModifierGroupRule modifier_group_rule { get; set; }
    }
}