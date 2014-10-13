using DevCommom;
using LeagueSharp;
using LeagueSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Evade;

/*
 * ##### DevLulu Mods #####
 * 
 * + Support Mode and AP Carry Mode with separated logic
 * + E + Q Combo ! (Use enemy/ally/minion to hit target)
 * + E/R Interrupt Spells
 * + W Interrupt/Gapcloser
 * + Skin Hack
 * + Help Ally W/E/R
 * + Auto Shield Ally (Evade integrated)

*/

namespace DevLulu
{
    public class Program
    {
        public const string ChampionName = "lulu";

        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();
        public static Obj_AI_Hero Player;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static SkinManager SkinManager;
        public static IgniteManager IgniteManager;
        public static BarrierManager BarrierManager;

        private static bool mustDebug = true;

        static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (!Player.ChampionName.ToLower().Contains(ChampionName))
                return;

            InitializeSpells();

            InitializeSkinManager();

            InitializeMainMenu();

            InitializeAttachEvents();

            //Game.PrintChat(string.Format("<font color='#F7A100'>DevLulu Loaded v{0}</font>", Assembly.GetExecutingAssembly().GetName().Version));

            Game.PrintChat(string.Format("<font color='#FF0000'>DevLulu: THIS ASSEMBLY IS NOT FINISHED YET!!!</font>"));
        }

        private static void InitializeAttachEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;

            Game.OnGameProcessPacket += Game_OnGameProcessPacket;

            Evade.SkillshotDetector.OnDetectSkillshot += SkillshotDetector_OnDetectSkillshot;

            Config.Item("ComboDamage").ValueChanged += (object sender, OnValueChangeEventArgs e) => { Utility.HpBarDamageIndicator.Enabled = e.GetNewValue<bool>(); };
            if (Config.Item("ComboDamage").GetValue<bool>())
            {
                Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                Utility.HpBarDamageIndicator.Enabled = true;
            }
        }

        static void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            if (args.PacketData[0] == 0xB7)
            {
                var packet = new GamePacket(args.PacketData);
                packet.Position = 1;
                var targetbuff = ObjectManager.GetUnitByNetworkId<Obj_AI_Base>(packet.ReadInteger());

                ProcessGainBuff(targetbuff);
                return;
            }
            //if (args.PacketData[0] == 0x7B)
            //{
            //    ProcessLoseBuff();
            //    return;
            //}
        }

        private static void ProcessGainBuff(Obj_AI_Base unit)
        {
            //if (mustDebug)
            //{
            //    Game.PrintChat("ProcessGainBuff -> " + unit.BaseSkinName);
            //    foreach(var buff in unit.Buffs.Where(buff => buff.IsActive))
            //        Game.PrintChat(string.Format("{0} Buff -> {1} {2}", unit.BaseSkinName, buff.Name, buff.Count));
            //}

            var buffList = unit.Buffs.Where(buff => buff.Name.ToLower().StartsWith("lulufae") && buff.IsActive && !unit.IsMe);

            if (buffList.Count() > 0)
            {
                var packetCast = Config.Item("PacketCast").GetValue<bool>();

                var queryEnemyList = DevHelper.GetEnemyList().Where(x => x.Distance(unit) < Q.Range).OrderBy(x => x.Health).ToList();
                if (queryEnemyList.Count() > 0)
                {
                    if (mustDebug)
                        Game.PrintChat(string.Format("Cast Q from -> {0}", unit.BaseSkinName));
                    var enemy = queryEnemyList.First();
                    var pred = Q.GetPrediction(enemy);
                    Q.Cast(pred.CastPosition, packetCast);
                }
            }

        }

        static void SkillshotDetector_OnDetectSkillshot(Evade.Skillshot skillshot)
        {
            if (mustDebug && skillshot.IsDanger(Player.ServerPosition.To2D()))
                Game.PrintChat("OnDetectSkillshot -> IsDanger");

            var UseWHelpAlly = Config.Item("UseWHelpAlly").GetValue<bool>();
            var UseEHelpAlly = Config.Item("UseEHelpAlly").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (skillshot.Unit.IsEnemy)
            {
                if (UseEHelpAlly && E.IsReady())
                {
                    var AllyList = DevCommom.DevHelper.GetAllyList()
                        .Where(ally => Player.Distance(ally) < E.Range && skillshot.IsDanger(ally.ServerPosition.To2D()))
                        .OrderBy(ally => ally.Health);

                    if (AllyList.Count() > 0)
                    {
                        var ally = AllyList.First();
                        E.CastOnUnit(ally, packetCast);
                    }
                }
            }
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var BarrierGapCloser = Config.Item("BarrierGapCloser").GetValue<bool>();
            var WGapCloser = Config.Item("WGapCloser").GetValue<bool>();

            if (BarrierGapCloser && gapcloser.Sender.IsValidTarget(Player.AttackRange))
            {
                if (BarrierManager.Cast())
                    Game.PrintChat(string.Format("OnEnemyGapcloser -> BarrierGapCloser on {0} !", gapcloser.Sender.SkinName));
            }

            if (WGapCloser && W.IsReady() && gapcloser.Sender.IsValidTarget(W.Range))
            {
                W.CastOnUnit(gapcloser.Sender, packetCast);
                if (mustDebug)
                    Game.PrintChat("W OnEnemyGapcloser");
            }
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var WInterruptSpell = Config.Item("WInterruptSpell").GetValue<bool>();

            if (WInterruptSpell && W.IsReady() && unit.IsValidTarget(W.Range))
            {
                W.CastOnUnit(unit, packetCast);
                if (mustDebug)
                    Game.PrintChat("W to Interrup");
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    //BurstCombo();
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    //WaveClear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    //Freeze();
                    break;
                default:
                    break;
            }

            SkinManager.Update();
        }

        public static void UltimateHandler()
        { 
            var UseRAlly = Config.Item("UseRAlly").GetValue<bool>();
            var UseRAllyMinHealth = Config.Item("UseRAllyMinHealth").GetValue<Slider>().Value;
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (UseRAlly && R.IsReady())
            {
                if (Player.GetHealthPerc() < UseRAllyMinHealth && DevHelper.CountEnemyInTargetRange(Player, 500) > 0)
                {
                    R.CastOnUnit(Player, packetCast);
                    return;
                }

                var AllyList = DevHelper.GetAllyList().Where(x => Player.Distance(x.ServerPosition) < R.Range && x.GetHealthPerc() < UseRAllyMinHealth && DevHelper.CountEnemyInTargetRange(x, 500) > 0);
                if (AllyList.Count() > 0)
                {
                    var ally = AllyList.First();
                    R.CastOnUnit(ally, packetCast);
                }
            }
        }

        public static void HelpAlly()
        {
            var UseWHelpAlly = Config.Item("UseWHelpAlly").GetValue<bool>();
            var UseEHelpAlly = Config.Item("UseEHelpAlly").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (UseWHelpAlly || UseEHelpAlly)
            {
                var AllyList = DevHelper.GetAllyList().Where(x => Player.Distance(x.ServerPosition) < W.Range && x.GetHealthPerc() < 50 && DevHelper.CountEnemyInTargetRange(x, 600) > 0).OrderBy(x => x.Health);
                if (AllyList.Count() > 0)
                {
                    var ally = AllyList.First();

                    if (UseWHelpAlly && W.IsReady())
                        W.CastOnUnit(ally, packetCast);
                    if (UseEHelpAlly && E.IsReady())
                        E.CastOnUnit(ally, packetCast);
                }
            }
        }

        public static void CastEQ()
        {
            var eTarget = SimpleTs.GetTarget(Q.Range * 2, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            if (!Q.IsReady() || !E.IsReady())
                return;

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            var enemyList = DevHelper.GetEnemyList()
                .Where(x => eTarget.NetworkId != x.NetworkId && Player.Distance(x) < E.Range && x.Distance(eTarget) < Q.Range).ToList();
            if (enemyList.Count() > 0)
            {
                var unit = enemyList.First();
                E.CastOnUnit(unit, packetCast);
                // Wait for ProcessGainBuff
                if (mustDebug)
                    Game.PrintChat("CastEQ -> E Enemy");
            }

            var allyList = DevHelper.GetAllyList()
                .Where(x => !x.IsMe && Player.Distance(x) < E.Range && x.Distance(eTarget) < Q.Range).ToList();
            if (allyList.Count() > 0)
            {
                var unit = allyList.First();
                E.CastOnUnit(unit, packetCast);
                // Wait for ProcessGainBuff
                if (mustDebug)
                    Game.PrintChat("CastEQ -> E Ally");
            }

            var minionList = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.All, MinionOrderTypes.Health)
                .Where(x => x.Distance(eTarget) < Q.Range).OrderByDescending(x => x.Health).ToList();
            if (minionList.Count() > 0)
            {
                var unit = minionList.First();
                E.CastOnUnit(unit, packetCast);
                // Wait for ProcessGainBuff
                if (mustDebug)
                    Game.PrintChat("CastEQ -> E Minion");
            }
        }

        public static void CastWAlly()
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            var allyList = DevHelper.GetAllyList()
                .Where(ally => Player.Distance(ally) < W.Range && W.IsReady() && ally.GetNearestEnemy().Distance(ally) < ally.AttackRange)
                .OrderBy(ally => ally.Health);

            if (allyList.Count() > 0)
            {
                var ally = allyList.First();
                W.CastOnUnit(ally, packetCast);
            }
        }

        public static void CastWEnemy()
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            var eTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            if (eTarget.IsValidTarget(W.Range) && W.IsReady())
            {
                W.CastOnUnit(eTarget, packetCast);
            }
        }

        public static void CastEAlly()
        {
            // Its on SkillshotDetector_OnDetectSkillshot
        }

        public static void CastEEnemy()
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            if (eTarget.IsValidTarget(E.Range) && E.IsReady())
            {
                E.CastOnUnit(eTarget, packetCast);
            }
        }

        public static void CastQ()
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady())
            {
                Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }
        }

        public static void Combo()
        {
            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var UseEQCombo = Config.Item("UseEQCombo").GetValue<bool>();
            
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (IsSupportMode)
            {
                if (useW)
                    CastWAlly();
                if (useE)
                    CastEAlly();
                if (useQ)
                    CastQ();
                if (UseEQCombo)
                    CastEQ();
            }
            else
            {
                if (useQ)
                    CastQ();
                if (useW)
                    CastWEnemy();
                if (useE)
                    CastEEnemy();
                if (UseEQCombo)
                    CastEQ();

                if (IgniteManager.CanKill(eTarget))
                {
                    if (IgniteManager.Cast(eTarget))
                        Game.PrintChat(string.Format("Ignite Combo KS -> {0} ", eTarget.SkinName));
                }
            }
        }

        public static void Harass()
        {
            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var UseEQHarass = Config.Item("UseEQHarass").GetValue<bool>();
            
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (IsSupportMode)
            {
                if (useW)
                    CastWAlly();
                if (useE)
                    CastEAlly();
                if (useQ)
                    CastQ();
                if (UseEQHarass)
                    CastEQ();
            }
            else
            {
                if (useQ)
                    CastQ();
                if (useW)
                    CastWEnemy();
                if (useE)
                    CastEEnemy();
                if (UseEQHarass)
                    CastEQ();
            }

        }

        private static void InitializeSkinManager()
        {
            SkinManager = new SkinManager();
            SkinManager.Add("Classic Lulu");
            SkinManager.Add("Bittersweet Lulu");
            SkinManager.Add("Wicked Lulu");
            SkinManager.Add("Dragon Trainer Lulu");
            SkinManager.Add("Winter Wonder Lulu");
        }

        private static void InitializeSpells()
        {
            IgniteManager = new IgniteManager();
            BarrierManager = new BarrierManager();

            Q = new Spell(SpellSlot.Q, 950);
            Q.SetSkillshot(0.2f, 60, 1450, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 650);
            W.SetTargetted(0.2f, float.MaxValue);

            E = new Spell(SpellSlot.E, 650);
            E.SetTargetted(0.2f, float.MaxValue);

            R = new Spell(SpellSlot.R, 900);
            R.SetTargetted(0.2f, float.MaxValue);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            //if (args.Target.IsMinion && IsSupportMode)
            //{
            //    var allyADC = Player.GetNearestAlly();
            //    if (!allyADC.IsMe && allyADC.Distance(args.Target) < allyADC.AttackRange * 1.2)
            //        args.Process = false;
            //}
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.IsReady())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
                }
            }
        }

        private static float GetComboDamage(Obj_AI_Hero enemy)
        {
            IEnumerable<SpellSlot> spellCombo = new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
            return (float)Damage.GetComboDamage(Player, enemy, spellCombo);
        }

        private static bool IsSupportMode {
            get { return Config.Item("UseQHarass").GetValue<bool>(); }
        }

        private static void InitializeMainMenu()
        {
            Config = new Menu("DevLulu", "DevLulu", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Mode", "Mode"));
            Config.SubMenu("Mode").AddItem(new MenuItem("SupportMode", "Support Mode").SetValue(true));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseEQCombo", "Use Combo E+Q to reach far enemies").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEQHarass", "Use Combo E+Q to reach far enemies").SetValue(true));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("ManaLaneClear", "Min Mana LaneClear").SetValue(new Slider(25, 1, 100)));

            Config.AddSubMenu(new Menu("Freeze", "Freeze"));
            Config.SubMenu("Freeze").AddItem(new MenuItem("UseQFreeze", "Use Q LastHit").SetValue(true));
            Config.SubMenu("Freeze").AddItem(new MenuItem("ManaFreeze", "Min Mana Q").SetValue(new Slider(25, 1, 100)));

            Config.AddSubMenu(new Menu("Ultimate", "Ultimate"));
            Config.SubMenu("Ultimate").AddItem(new MenuItem("UseRAlly", "Use R Ally").SetValue(true));
            Config.SubMenu("Ultimate").AddItem(new MenuItem("UseRAllyMinHealth", "Ally MinHealth").SetValue(new Slider(30, 1, 100)));

            Config.AddSubMenu(new Menu("Help Ally", "HelpAlly"));
            Config.SubMenu("HelpAlly").AddItem(new MenuItem("UseWHelpAlly", "Use W").SetValue(true));
            Config.SubMenu("HelpAlly").AddItem(new MenuItem("UseEHelpAlly", "Use E").SetValue(true));
            Config.SubMenu("HelpAlly").AddItem(new MenuItem("AllyMinHealth", "Help Ally MinHealth").SetValue(new Slider(50, 1, 100)));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("PacketCast", "Use PacketCast").SetValue(true));

            Config.AddSubMenu(new Menu("GapCloser", "GapCloser"));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("BarrierGapCloser", "Barrier onGapCloser").SetValue(true));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("WGapCloser", "W onGapCloser").SetValue(true));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("WInterruptSpell", "W Interrupt Spell").SetValue(true));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ComboDamage", "Drawings on HPBar").SetValue(true));

            SkinManager.AddToMenu(ref Config);

            Config.AddToMainMenu();
        }
    }
}
