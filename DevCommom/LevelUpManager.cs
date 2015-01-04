using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DevCommom
{
    public class LevelUpManager
    {
        private int[] spellPriorityList;
        private int lastLevel;

        private Dictionary<string, int[]> SpellPriorityList;
        private Menu Menu;
        private int SelectedPriority;

        private AutoLevel AutoLevel;

        public LevelUpManager()
        {
            lastLevel = 0;
            SpellPriorityList = new Dictionary<string, int[]>();

        }


        public void AddToMenu(ref Menu menu)
        {
            Menu = menu;
            if (SpellPriorityList.Count > 0)
            {
                Menu.AddSubMenu(new Menu("Spell Level Up", "LevelUp"));
                Menu.SubMenu("LevelUp").AddItem(new MenuItem("LevelUp_" + ObjectManager.Player.ChampionName + "_enabled", "Enable").SetValue(true));
                Menu.SubMenu("LevelUp").AddItem(new MenuItem("LevelUp_" + ObjectManager.Player.ChampionName + "_select", "").SetValue(new StringList(SpellPriorityList.Keys.ToArray())));
                SelectedPriority = Menu.Item("LevelUp_" + ObjectManager.Player.ChampionName + "_select").GetValue<StringList>().SelectedIndex;
            }
        }

        public void Add(string spellPriorityDesc, int[] spellPriority)
        {
            SpellPriorityList.Add(spellPriorityDesc, spellPriority);

            AutoLevel = new AutoLevel(spellPriority);
            AutoLevel.Enabled(true);
        }


        public void Update()
        {
            if (SpellPriorityList.Count == 0 || !Menu.Item("LevelUp_" + ObjectManager.Player.ChampionName + "_enabled").GetValue<bool>() || this.lastLevel == ObjectManager.Player.Level)
            {
                AutoLevel.Enabled(false);
                return;
            }

            this.spellPriorityList = SpellPriorityList.Values.ElementAt(SelectedPriority);

            int qL = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level;
            int wL = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level;
            int eL = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level;
            int rL = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level;

            if (qL + wL + eL + rL < ObjectManager.Player.Level)
            {
                int[] level = new int[] { 0, 0, 0, 0 };
                for (int i = 0; i < ObjectManager.Player.Level; i++)
                    level[this.spellPriorityList[i] - 1] = level[this.spellPriorityList[i] - 1] + 1;

                if (qL < level[0]) ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.Q);
                if (wL < level[1]) ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.W);
                if (eL < level[2]) ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.E);
                if (rL < level[3]) ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.R);
            }
        }
    }
}
