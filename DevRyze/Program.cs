using DevCommom;
using LeagueSharp;
using LeagueSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

/*
 * ##### DevRyze Mods #####
 * + SBTW with Q/W/E/R
 * + Wave/Jungle Clear
 * + Harras/WaveClear with Min Mana Slider
 * + Barrier GapCloser when LowHealth
 * + Interrupt Spell with W
 * + W Gapcloser
 * + Skin Hack

*/

namespace DevRyze
{
    class Program
    {
        public const string ChampionName = "ryze";

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
        public static AssemblyUtil assemblyUtil;

        private static List<Obj_AI_Base> MinionListToIgnore;

        private static bool mustDebug = false;


        static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            try
            {
                Player = ObjectManager.Player;

                if (!Player.ChampionName.ToLower().Contains(ChampionName))
                    return;

                InitializeSpells();

                InitializeSkinManager();

                InitializeMainMenu();

                InitializeAttachEvents();

                Game.PrintChat(string.Format("<font color='#fb762d'>DevRyze Loaded v{0}</font>", Assembly.GetExecutingAssembly().GetName().Version));

                assemblyUtil = new AssemblyUtil();
                assemblyUtil.onGetVersionCompleted += AssemblyUtil_onGetVersionCompleted;
                assemblyUtil.GetLastVersionAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                if (mustDebug)
                    Game.PrintChat(ex.Message);
            }
        }

        static void AssemblyUtil_onGetVersionCompleted(OnGetVersionCompletedArgs args)
        {
            if (args.IsSuccess)
            {
                if (args.CurrentVersion == Assembly.GetExecutingAssembly().GetName().Version.ToString())
                    Game.PrintChat(string.Format("<font color='#fb762d'>DevRyze You have the lastest version. {0}</font>", Assembly.GetExecutingAssembly().GetName().Version));
                else
                    Game.PrintChat(string.Format("<font color='#fb762d'>DevRyze NEW VERSION available! Tap F8 to update!</font>", Assembly.GetExecutingAssembly().GetName().Version));
            }
        }

        private static void InitializeSpells()
        {
            if (mustDebug)
                Game.PrintChat("InitializeSpells Start");

            IgniteManager = new IgniteManager();
            BarrierManager = new BarrierManager();
            MinionListToIgnore = new List<Obj_AI_Base>();

            Q = new Spell(SpellSlot.Q, 630);
            Q.SetTargetted(0.2f, float.MaxValue);

            W = new Spell(SpellSlot.W, 600);

            E = new Spell(SpellSlot.E, 600);
            E.SetTargetted(0.2f, float.MaxValue);

            R = new Spell(SpellSlot.R);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            if (mustDebug)
                Game.PrintChat("InitializeSpells Finish");
        }

        private static void InitializeSkinManager()
        {
            if (mustDebug)
                Game.PrintChat("InitializeSkinManager Start");

            SkinManager = new SkinManager();
            SkinManager.Add("Classic Ryze");
            SkinManager.Add("Human Ryze");
            SkinManager.Add("Tribal Ryze");
            SkinManager.Add("Uncle Ryze");
            SkinManager.Add("Triumphant Ryze");
            SkinManager.Add("Professor Ryze");
            SkinManager.Add("Zombie Ryze");
            SkinManager.Add("Dark Crystal Ryze");
            SkinManager.Add("Pirate Ryze");

            if (mustDebug)
                Game.PrintChat("InitializeSkinManager Finish");
        }

        private static void InitializeAttachEvents()
        {
            if (mustDebug)
                Game.PrintChat("InitializeAttachEvents Start");

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;

            Config.Item("ComboDamage").ValueChanged += (object sender, OnValueChangeEventArgs e) => { Utility.HpBarDamageIndicator.Enabled = e.GetNewValue<bool>(); };
            if (Config.Item("ComboDamage").GetValue<bool>())
            {
                Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                Utility.HpBarDamageIndicator.Enabled = true;
            }

            if (mustDebug)
                Game.PrintChat("InitializeAttachEvents Finish");
        }

        // same logic of Orbwalking_AfterAttack but without callback.. TODO: test it
        private static void CheckAALastHit()
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (Environment.TickCount < Orbwalking.LastAATick + (Player.AttackDelay * 1000) + (Game.Ping / 2))
            {
                int timeNextAA = (int)(Orbwalking.LastAATick + (Player.AttackDelay * 1000) + (Game.Ping / 2)) - Environment.TickCount;

                var MinionList = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health)
                    .Where(x => !x.IsDead && HealthPrediction.LaneClearHealthPrediction(x, (int)(timeNextAA * 1.1)) <= 0).ToList();

                if (MinionList.Count() > 0)
                {
                    var mob = MinionList.First();
                    if (Q.IsReady() && mob.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(mob, packetCast);
                        MinionList.Remove(mob);
                    }
                }

                if (MinionList.Count() > 0)
                {
                    var mob = MinionList.First();
                    if (E.IsReady() && mob.IsValidTarget(E.Range))
                    {
                        E.CastOnUnit(mob, packetCast);
                        MinionList.Remove(mob);
                    }
                }

            }
        }

        static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit ||
                Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                if (target.IsMinion)
                {
                    var MinionList = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health)
                        .Where(x => 
                            !x.IsDead && target.NetworkId != x.NetworkId && !MinionListToIgnore.Contains(x) &&
                            HealthPrediction.LaneClearHealthPrediction(x, (int)(Player.AttackDelay * 1000 * 1.1)) <= 0).ToList();

                    if (MinionList.Count() > 0)
                    { 
                        var mob = MinionList.First();
                        if (Q.IsReady() && mob.IsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(mob, packetCast);
                            MinionListToIgnore.Add(mob);
                            MinionList.Remove(mob);
                        }
                    }

                    if (MinionList.Count() > 0)
                    {
                        var mob = MinionList.First();
                        if (E.IsReady() && mob.IsValidTarget(E.Range))
                        {
                            E.CastOnUnit(mob, packetCast);
                            MinionListToIgnore.Add(mob);
                        }
                    }
                }
            }
        }

        private static float GetComboDamage(Obj_AI_Hero enemy)
        {
            IEnumerable<SpellSlot> spellCombo = new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
            return (float)Damage.GetComboDamage(Player, enemy, spellCombo);
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                var useQ = Config.Item("UseQCombo").GetValue<bool>();
                var useW = Config.Item("UseWCombo").GetValue<bool>();
                var useE = Config.Item("UseQCombo").GetValue<bool>();

                if (Player.GetNearestEnemy().IsValidTarget(W.Range) && ((useQ && Q.IsReady()) || (useW && W.IsReady() || useE && E.IsReady())))
                    args.Process = false;
            }
            else
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                var useQ = Config.Item("UseQHarass").GetValue<bool>();
                var useW = Config.Item("UseWHarass").GetValue<bool>();
                var useE = Config.Item("UseEHarass").GetValue<bool>();

                if (Player.GetNearestEnemy().IsValidTarget(W.Range) && ((useQ && Q.IsReady()) || (useW && W.IsReady() || useE && E.IsReady())))
                    args.Process = false;
            }
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var WInterruptSpell = Config.Item("WInterruptSpell").GetValue<bool>();

            if (WInterruptSpell && W.IsReady() && unit.IsValidTarget(W.Range))
            {
                W.CastOnUnit(unit, packetCast);
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
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            try
            {
                switch (Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        BurstCombo();
                        Combo();
                        break;
                    case Orbwalking.OrbwalkingMode.Mixed:
                        Harass();
                        break;
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        WaveClear();
                        break;
                    case Orbwalking.OrbwalkingMode.LastHit:
                        Freeze();
                        break;
                    default:
                        {
                            if (Config.Item("UseHarassAlways").GetValue<bool>())
                            {
                                Harass();
                            }
                            break;
                        }
                       
                }

                SkinManager.Update();

            }
            catch (Exception ex)
            {
                Console.WriteLine("OnTick e:" + ex.ToString());
                if (mustDebug)
                    Game.PrintChat("OnTick e:" + ex.Message);
            }
        }

        public static void BurstCombo()
        {
            var eTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>() || Config.Item("UseRComboToggle").GetValue<KeyBind>().Active;
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            // Cast R if will hit 1+ enemies
            if (useR && R.IsReady() && DevHelper.CountEnemyInTargetRange(eTarget, 300) > 1)
            {
                if (packetCast)
                    Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, SpellSlot.R)).Send();
                else
                    R.Cast();
            }

            // Cast R for Killable Combo
            IEnumerable<SpellSlot> spellCombo = new[] { SpellSlot.Q, SpellSlot.R, SpellSlot.E, SpellSlot.Q, SpellSlot.W, SpellSlot.Q };
            if (useR && R.IsReady() && Player.IsKillable(eTarget, spellCombo))
            {
                if (packetCast)
                    Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, SpellSlot.R)).Send();
                else
                    R.Cast();
            }

            // Cast on W
            IEnumerable<SpellSlot> spellComboHard = new[] { SpellSlot.Q, SpellSlot.R, SpellSlot.E, SpellSlot.Q, SpellSlot.W, SpellSlot.Q, SpellSlot.Q };
            if (useR && R.IsReady() && eTarget.HasBuff("Rune Prision") && Player.IsKillable(eTarget, spellComboHard))
            {
                if (packetCast)
                    Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, SpellSlot.R)).Send();
                else
                    R.Cast();
            }
        }

        public static void Combo()
        {
            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            if (mustDebug)
                Game.PrintChat("Combo Start");

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>() || Config.Item("UseRComboToggle").GetValue<KeyBind>().Active;
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                Q.CastOnUnit(eTarget, packetCast);
            }

            if (Player.Distance(eTarget) >= 575 && !eTarget.IsFacing(Player) && W.IsReady() && useW)
            {
                W.CastOnUnit(eTarget, packetCast);
                return;
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastOnUnit(eTarget, packetCast);
                return;
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                E.CastOnUnit(eTarget, packetCast);
                return;
            }

            if (IgniteManager.CanKill(eTarget))
            {
                if (IgniteManager.Cast(eTarget))
                    Game.PrintChat(string.Format("Ignite Combo KS -> {0} ", eTarget.SkinName));
            }
        }

        public static void Harass()
        {
            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            if (mustDebug)
                Game.PrintChat("Harass Start");

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                Q.CastOnUnit(eTarget, packetCast);
            }

            if (Player.Distance(eTarget) > 500 && eTarget.IsValidTarget(W.Range) && !eTarget.IsFacing(Player) && W.IsReady() && useW)
            {
                W.CastOnUnit(eTarget, packetCast);
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastOnUnit(eTarget, packetCast);
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                E.CastOnUnit(eTarget, packetCast);
            }
        }

        public static void WaveClear()
        {
            var MinionList = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health)
                .Where(x => !MinionListToIgnore.Contains(x)).ToList();

            var JungleList = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mustDebug)
                Game.PrintChat("WaveClear Start");

            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("UseWLaneClear").GetValue<bool>();
            var useE = Config.Item("UseELaneClear").GetValue<bool>();
            var ManaLaneClear = Config.Item("ManaLaneClear").GetValue<Slider>().Value;
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (Q.IsReady() && useQ && Player.GetManaPerc() > ManaLaneClear)
            {
                var queryJungle = JungleList.Where(x => x.IsValidTarget(Q.Range));
                if (queryJungle.Count() > 0)
                {
                    var mob = queryJungle.First();
                    Q.CastOnUnit(mob, packetCast);
                }

                var queryMinion = MinionList.Where(x => x.IsValidTarget(Q.Range) && HealthPrediction.LaneClearHealthPrediction(x, (int)Q.Delay * 1000) < Player.GetSpellDamage(x, SpellSlot.Q) * 0.9);
                if (queryMinion.Count() > 0)
                {
                    var mob = queryMinion.First();
                    Q.CastOnUnit(mob, packetCast);
                    MinionListToIgnore.Add(mob);
                    MinionList.Remove(mob);
                }
            }

            if (W.IsReady() && useW && Player.GetManaPerc() > ManaLaneClear)
            {
                var queryJungle = JungleList.Where(x => x.IsValidTarget(W.Range));
                if (queryJungle.Count() > 0)
                {
                    var mob = queryJungle.First();
                    W.CastOnUnit(mob, packetCast);
                }

                var query = MinionList.Where(x => x.IsValidTarget(W.Range) && HealthPrediction.LaneClearHealthPrediction(x, (int)W.Delay * 1000) < Player.GetSpellDamage(x, SpellSlot.W) * 0.9);
                if (query.Count() > 0)
                {
                    var mob = query.First();
                    W.CastOnUnit(mob, packetCast);
                    MinionListToIgnore.Add(mob);
                    MinionList.Remove(mob);
                }
            }

            if (E.IsReady() && useE && Player.GetManaPerc() > ManaLaneClear)
            {
                var queryJungle = JungleList.Where(x => x.IsValidTarget(E.Range));
                if (queryJungle.Count() > 0)
                {
                    var mob = queryJungle.First();
                    E.CastOnUnit(mob, packetCast);
                }

                var query = MinionList.Where(x => x.IsValidTarget(E.Range) && HealthPrediction.LaneClearHealthPrediction(x, (int)E.Delay * 1000) < Player.GetSpellDamage(x, SpellSlot.E) * 0.9);
                if (query.Count() > 0)
                {
                    var mob = query.First();
                    E.CastOnUnit(mob, packetCast);
                    MinionListToIgnore.Add(mob);
                    MinionList.Remove(mob);
                }
            }

        }

        public static void Freeze()
        {
            var MinionList = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health)
                .Where(x => !MinionListToIgnore.Contains(x));

            if (mustDebug)
                Game.PrintChat("Freeze Start");

            var useQ = Config.Item("UseQFreeze").GetValue<bool>();
            var ManaLaneClear = Config.Item("ManaFreeze").GetValue<Slider>().Value;
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (Q.IsReady() && useQ && Player.GetManaPerc() > ManaLaneClear)
            {
                var queryMinion = MinionList.Where(x => x.IsValidTarget(Q.Range) && HealthPrediction.LaneClearHealthPrediction(x, (int)Q.Delay * 1000) < Player.GetSpellDamage(x, SpellSlot.Q) * 0.9);
                if (queryMinion.Count() > 0)
                {
                    var mob = queryMinion.First();
                    Q.CastOnUnit(mob, packetCast);
                    MinionListToIgnore.Add(mob);
                }
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                {
                    if (spell.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, System.Drawing.Color.Green);
                    else
                        Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, System.Drawing.Color.Red);
                }
            }
        }

        private static void InitializeMainMenu()
        {
            if (mustDebug)
                Game.PrintChat("InitializeMainMenu Start");

            Config = new Menu("DevRyze", "DevRyze", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRComboToggle", "Use R (toggle)").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Toggle)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseHarassAlways", "Keep Harras Always ON").SetValue(false));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("ManaLaneClear", "Min Mana LaneClear").SetValue(new Slider(40, 1, 100)));

            Config.AddSubMenu(new Menu("Freeze", "Freeze"));
            Config.SubMenu("Freeze").AddItem(new MenuItem("UseQFreeze", "Use Q LastHit").SetValue(false));
            Config.SubMenu("Freeze").AddItem(new MenuItem("ManaFreeze", "Min Mana Q").SetValue(new Slider(40, 1, 100)));

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

            if (mustDebug)
                Game.PrintChat("InitializeMainMenu Finish");
        }
    }
}
