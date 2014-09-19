using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;
using Igniter;

/*
 * #### DevCassio ####
 * 
 * InjectionDev GitHub: https://github.com/InjectionDev/LeagueSharp/
 * Script Based GitHub: https://github.com/fueledbyflux/LeagueSharp-Public/tree/master/SigmaCass/ - Credits to fueledbyflux
* /

/*
 * ##### DevCassio Mods #####
 * 
 * + AntiGapCloser with R when LowHealth
 * + LastHit E On Posioned Minions
 * + LastHit E On Non-Posioned Minions, if no enemy near
 * + Ignite KS
 * + Menu No-Face Exploit (PacketCast)
 * + Skin Hack
 * + Show E Damage on Enemy HPBar
 * + Assited Ult
 * + Block Ult if will not hit
*/

namespace DevCassio
{
    class Program
    {
        public const string ChampionName = "Cassiopeia";

        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();
        public static Obj_AI_Hero Player;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Ignite IgniteSpell;
        public static List<Obj_AI_Base> MinionList;
        public static DevCommom.SkinManager SkinManager;

        public static bool mustDebug = Environment.MachineName == "daniel";

        static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += onGameLoad;
        }

        private static void OnTick(EventArgs args)
        {
            MinionList = MinionManager.GetMinions(ObjectManager.Player.Position, E.Range, MinionTypes.All);

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                BurstCombo();
                Combo();
            }
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                Harass();
            }
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                WaveClear();
            }
            if (Config.Item("FreezeActive").GetValue<KeyBind>().Active)
            {
                Freeze();
            }

            SkinManager.Update();
        }

        public static void BurstCombo()
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var useIgnite = Config.Item("UseIgnite").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            var eTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

            IEnumerable<DamageLib.SpellType> spellCombo = new[]
                {
                    DamageLib.SpellType.Q, 
                    DamageLib.SpellType.E, 
                    DamageLib.SpellType.E,
                    DamageLib.SpellType.R, 
                    DamageLib.SpellType.IGNITE
                };

            bool igniteReady = Player.SummonerSpellbook.CanUseSpell(IgniteSpell.GetSpell().Slot) == SpellState.Ready;

            if (Q.IsReady(2000) && E.IsReady(2000) && R.IsReady() && useR && igniteReady)
            {
                if (DamageLib.IsKillable(eTarget, spellCombo))
                {
                    R.CastIfWillHit(eTarget, 1, packetCast);

                    IgniteSpell.Cast(eTarget);
                }
            }
        }

        public static void Combo()
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var useIgnite = Config.Item("UseIgnite").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);


            if (eTarget.IsValidTarget(R.Range) && R.IsReady() && useR)
            {
                R.CastIfWillHit(eTarget, Config.Item("rCount").GetValue<Slider>().Value, true);
                return;
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                if (eTarget.HasBuffOfType(BuffType.Poison) || DamageLib.getDmg(eTarget, DamageLib.SpellType.E) > eTarget.Health)
                {
                    E.CastOnUnit(eTarget, packetCast);
                    return;
                }
            }

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                return;
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                return;
            }

            if (IgniteSpell.CanKill(eTarget))
            {
                IgniteSpell.Cast(eTarget);
                Game.PrintChat(string.Format("Ignite Combo KS -> {0} ", eTarget.SkinName));
            }

        }

        public static void Harass()
        {
            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                if (eTarget.HasBuffOfType(BuffType.Poison) || DamageLib.getDmg(eTarget, DamageLib.SpellType.E) > eTarget.Health)
                {
                    E.CastOnUnit(eTarget, packetCast);
                    return;
                }
            }

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                return;
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                return;
            }
        }

        public static void WaveClear()
        {
            if (MinionList.Count == 0)
                return;

            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            foreach (var minion in MinionList)
            {
                var predHP = HealthPrediction.GetHealthPrediction(minion, (int)E.Delay);

                if (E.IsReady() && minion.HasBuffOfType(BuffType.Poison) && E.GetDamage(minion) > minion.Health && predHP > 0 && minion.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(minion, packetCast);
                }

                if (Q.IsReady() && minion.IsValidTarget(Q.Range))
                {
                    Q.Cast(Q.GetCircularFarmLocation(MinionList).Position, packetCast);
                }

                if (W.IsReady() && minion.IsValidTarget(Q.Range))
                {
                    W.Cast(W.GetCircularFarmLocation(MinionList).Position, packetCast);
                }
            }
        }

        public static void Freeze()
        {
            if (MinionList.Count == 0)
                return;

            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var nearestTarget = DevCommom.DevCommom.GetNearestEnemy();

            foreach (var minion in MinionList)
            {
                var predHP = HealthPrediction.GetHealthPrediction(minion, (int)E.Delay);

                if (E.IsReady() && E.GetDamage(minion) > minion.Health && predHP > 0 && minion.IsValidTarget(E.Range))
                {
                    if (minion.HasBuffOfType(BuffType.Poison))
                    {
                        E.CastOnUnit(minion, packetCast);
                    }
                    else if (Player.ServerPosition.Distance(nearestTarget.ServerPosition) > Q.Range + 50)
                    {
                        E.CastOnUnit(minion, packetCast);
                    }
                }
            }
        }

        public static void CastAssistedUlt()
        {
            var eTarget = DevCommom.DevCommom.GetNearestEnemy();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (eTarget.IsValidTarget(R.Range) && R.IsReady())
            {
                R.CastIfWillHit(eTarget, 1, packetCast);
                Game.PrintChat(string.Format("AssistedUlt fired"));
                return;
            }
        }

        private static void onGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.ChampionName != ChampionName)
                return;

            InitializeSpells();

            InitializeMainMenu();

            InitializeSkinManager();

            InitializeAttachEvents();

            Game.PrintChat(string.Format("<font color='#F7A100'>DevCassio Loaded v{0}</font>", Assembly.GetExecutingAssembly().GetName().Version));
        }

        private static void InitializeAttachEvents()
        {
            Game.OnGameUpdate += OnTick;
            Game.OnGameSendPacket += Game_OnGameSendPacket;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            IgniteSpell.CanKillstealEnemies += IgniteSpell_CanKillstealEnemies;
        }

        private static void InitializeSpells()
        {
            float extraHitBox = 75; // TODO: check

            Q = new Spell(SpellSlot.Q, 850 + extraHitBox);
            Q.SetSkillshot(0.6f, 140, float.MaxValue, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 850 + extraHitBox);
            W.SetSkillshot(0.5f, 210, 2500, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 700);
            E.SetTargetted(0.1f, float.MaxValue);

            R = new Spell(SpellSlot.R, 800 + extraHitBox);
            R.SetSkillshot(0.5f, 210, float.MaxValue, false, SkillshotType.SkillshotCone);

            IgniteSpell = new Ignite();

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
        }

        private static void InitializeSkinManager()
        {
            SkinManager = new DevCommom.SkinManager();
            SkinManager.Add("Classic");
            SkinManager.Add("Desperada");
            SkinManager.Add("Siren");
            SkinManager.Add("Mythic");
            SkinManager.Add("Jade Fang");
        }

        static void IgniteSpell_CanKillstealEnemies(object sender, IgniteEventArgs e)
        {
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                IgniteSpell.Cast(e.Enemies.FirstOrDefault());
                Game.PrintChat(string.Format("Ignite KS -> {0} ", e.Enemies.FirstOrDefault().SkinName));
            }
        }

        static void Game_OnGameSendPacket(GamePacketEventArgs args)
        {
            if (Config.Item("BlockUlt").GetValue<KeyBind>().Active && args.PacketData[0] == Packet.C2S.Move.Header)
            {
                var decodedPacket = Packet.C2S.Cast.Decoded(args.PacketData);
                if (decodedPacket.SourceNetworkId == Player.NetworkId && decodedPacket.Slot == SpellSlot.R)
                {
                    var query = DevCommom.DevCommom.GetEnemyList().Where(x => R.WillHit(x, R.GetPrediction(x).CastPosition));
                    if (query.Count() == 0)
                    {
                        args.Process = false;
                        Game.PrintChat(string.Format("Ult Blocked"));
                    }
                }
            }
        }

        static void Game_OnWndProc(WndEventArgs args)
        {
            if (Config.Item("UseAssistedUlt").GetValue<KeyBind>().Active && args.Msg == Config.Item("AssistedUltKey").GetValue<KeyBind>().Key)
            {
                args.Process = false;
                CastAssistedUlt();
            }
        }

        static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            Game.PrintChat(string.Format("OnPosibleToInterrupt -> {0} cast {1}", unit.SkinName, spell.SpellName));
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            Game.PrintChat(string.Format("OnEnemyGapcloser -> {0}", gapcloser.Sender.SkinName));

            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var RAntiGapcloser = Config.Item("RAntiGapcloser").GetValue<bool>();
            var RAntiGapcloserMinHealth = Config.Item("RAntiGapcloserMinHealth").GetValue<Slider>().Value;

            if (RAntiGapcloser && DevCommom.DevCommom.GetHealthPerc() < RAntiGapcloserMinHealth && gapcloser.Sender.IsValidTarget(R.Range))
            {
                R.Cast(gapcloser.Sender.ServerPosition, packetCast);
                Game.PrintChat(string.Format("OnEnemyGapcloser -> RAntiGapcloser on {0} !", gapcloser.Sender.SkinName));
            }
        }

        private static float GetEDamage(Obj_AI_Hero hero)
        {
            return (float)DamageLib.getDmg(hero, DamageLib.SpellType.E);
        }

        private static void DrawDebug()
        {
            float y = 0;

            // Buff Draw
            foreach (var t in ObjectManager.Player.Buffs.Select(b => b.DisplayName + " - " + b.IsActive + " - " + (b.EndTime > Game.Time) + " - " + b.IsPositive))
            {
                LeagueSharp.Drawing.DrawText(0, y, System.Drawing.Color.Wheat, t);
                y += 16;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.IsReady())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
                }
            }

            if (Config.Item("EDamage").GetValue<KeyBind>().Active)
                Utility.HpBarDamageIndicator.DamageToUnit = GetEDamage;

            if (mustDebug)
                DrawDebug();
        }

        private static void InitializeMainMenu()
        {
            Config = new Menu("DevCassio", "DevCassio", true);

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
            Config.SubMenu("Combo").AddItem(new MenuItem("rCount", "Min R Count").SetValue(new Slider(2, 1, 5)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("FreezeActive", "Freeze!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("EDamage", "E Damage on HPBar").SetValue(true));

            Config.AddSubMenu(new Menu("AntiGapcloser", "Anti Gapcloser"));
            Config.SubMenu("AntiGapcloser").AddItem(new MenuItem("RAntiGapcloser", "R AntiGapcloser").SetValue(true));
            Config.SubMenu("AntiGapcloser").AddItem(new MenuItem("RAntiGapcloserMinHealth", "R AntiGapcloser Min Health").SetValue(new Slider(60, 0, 100)));

            Config.AddSubMenu(new Menu("Exploit", "Exploit"));
            Config.SubMenu("Exploit").AddItem(new MenuItem("PacketCast", "No-Face Exploit (PacketCast)").SetValue(true));

            Config.AddSubMenu(new Menu("Ultimate", "Ultimate"));
            Config.SubMenu("Ultimate").AddItem(new MenuItem("UseAssistedUlt", "Use AssistedUlt").SetValue(true));
            Config.SubMenu("Ultimate").AddItem(new MenuItem("AssistedUltKey", "Assisted Ult Key").SetValue((new KeyBind("R".ToCharArray()[0], KeyBindType.Press))));
            Config.SubMenu("Ultimate").AddItem(new MenuItem("BlockUlt", "Block Ult will Not Hit").SetValue(true));

            SkinManager.AddToMenu(ref Config);

            Game.PrintChat(string.Format("set menu 4"));

            Config.AddToMainMenu();
        }
    }
}