using LeagueSharp;
using LeagueSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevCommom;

namespace DevDebug
{
    class Program
    {
        public static Menu Config;
        public static Obj_AI_Hero Player;

        static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            InitializeMainMenu();

            InitializeAttachEvents();

            Game.PrintChat("DevDebug Loaded");
        }

        private static void InitializeAttachEvents()
        {
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            
        }


        static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("DrawBuffs").GetValue<bool>())
                DrawBuffs();
        }

        static void Game_OnUpdate(EventArgs args)
        {
            
        }


        private static void DrawBuffs()
        {
            float xAlly = 60;
            float xEnemy = 320;
            float yAlly = 0;
            float yEnemy = 0;

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                System.Drawing.Color color = hero.IsEnemy ? System.Drawing.Color.Red : System.Drawing.Color.Blue;

                LeagueSharp.Drawing.DrawText(hero.IsEnemy ? xEnemy : xAlly, hero.IsEnemy ? yEnemy : yAlly, color, string.Format("{0} range{1}", hero.ChampionName, Orbwalking.GetRealAutoAttackRange(hero)));
                
                if (hero.IsEnemy)
                    yEnemy += 16;
                else
                    yAlly += 16;

                foreach (var buff in hero.Buffs)
                {
                    if (buff.IsActive)
                        LeagueSharp.Drawing.DrawText(hero.IsEnemy ? xEnemy + 10 : xAlly + 10, hero.IsEnemy ? yEnemy : yAlly, color, string.Format("{0} {1}", buff.DisplayName, buff.Count));
                    
                    if (hero.IsEnemy)
                        yEnemy += 16;
                    else
                        yAlly += 16;
                }

            }
        }

        private static void InitializeMainMenu()
        {
            Config = new Menu("DevDebug", "DevDebug", true);

            Config.AddSubMenu(new Menu("Draw", "Draw"));
            Config.SubMenu("Draw").AddItem(new MenuItem("DrawBuffs", "Draw Buffs").SetValue(true));

            Config.AddToMainMenu();
        }
    }
}
