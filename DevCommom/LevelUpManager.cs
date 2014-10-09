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
        private int lastLevel = 0;

        public LevelUpManager(int[] pSpellPriorityList)
        {
            this.spellPriorityList = pSpellPriorityList;

            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            Update();
        }

        public void Update()
        {
            if (this.lastLevel == ObjectManager.Player.Level)
                return;

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
