using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;
using DevCommom;
using SharpDX;

/*
 * ##### DevKogMaw Mods #####
 * + Chase Enemy After Death
 * + R KillSteal
 * + W/R Range based on Skill Level
 * + Assisted Ult
 * + Block Ult if will not hit
 * + Cast E with Min Mana Slider
 * + Barrier GapCloser when LowHealth
 * + Skin Hack
 * + Smart W usage
 * + Jungle Steal Alert
 * + Jungle Steal with R (Blue/Red/Dragon/Balron)

*/

namespace DevKogMaw
{
    class Program
    {
        public const string ChampionName = "kogmaw";

        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();
        public static Obj_AI_Hero Player;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static List<Obj_AI_Base> MinionList;
        public static SkinManager SkinManager;
        public static IgniteManager IgniteManager;
        public static BarrierManager BarrierManager;

        private static DateTime dtLastJungleStealAlert = DateTime.Now;
        private static DateTime dtLastJungleSteal = DateTime.Now;

        private static bool mustDebug = false;


        static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += onGameLoad;
        }

        private static void OnTick(EventArgs args)
        {
            try
            {
                switch (Orbwalker.ActiveMode)
                { 
                    case Orbwalking.OrbwalkingMode.Combo:
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
                        break;
                }

                if (Config.Item("RKillSteal").GetValue<bool>())
                    KillSteal();

                if (Config.Item("ChaseEnemyAfterDeath").GetValue<bool>())
                    ChaseEnemyAfterDeath();

                if (Config.Item("JungleStealAlert").GetValue<bool>())
                    JungleStealAlert();

                if (Config.Item("JungleSteal").GetValue<bool>())
                    JungleSteal();

                UpdateSpellsRange();

                SkinManager.Update();
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnTick e:" + ex.ToString());
                if (mustDebug)
                    Game.PrintChat("OnTick e:" + ex.Message);
            }
        }


        public static void Combo()
        {
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            if (mustDebug)
                Game.PrintChat("Combo Start");

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var useIgnite = Config.Item("UseIgnite").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var RMaxStacksCombo = Config.Item("RMaxStacksCombo").GetValue<Slider>().Value;
            var EManaCombo = Config.Item("EManaCombo").GetValue<Slider>().Value;

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

            if (!HasWBuff() && Player.AttackRange < Player.Distance(eTarget) && Player.Distance(eTarget) <= (Orbwalking.GetRealAutoAttackRange(eTarget) + W.Range) && W.IsReady() && useW)
            {
                W.Cast();
                Player.IssueOrder(GameObjectOrder.AttackUnit, eTarget);
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE && Player.GetManaPerc() > EManaCombo)
            {
                E.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

            if (eTarget.IsValidTarget(R.Range) && R.IsReady() && GetRStacks() < RMaxStacksCombo && useR)
            {
                R.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

            if (IgniteManager.CanKill(eTarget))
            {
                if (IgniteManager.Cast(eTarget))
                    Game.PrintChat(string.Format("Ignite Combo KS -> {0} ", eTarget.SkinName));
            }

        }


        public static void Harass()
        {
            if (mustDebug)
                Game.PrintChat("Harass Start");

            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var useR = Config.Item("UseRHarass").GetValue<bool>();
            var RMaxStacksHarass = Config.Item("RMaxStacksHarass").GetValue<Slider>().Value;
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var EManaHarass = Config.Item("EManaHarass").GetValue<Slider>().Value;

            if (mustDebug)
                Game.PrintChat("Harass Target -> " + eTarget.SkinName);

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

            if (!HasWBuff() && Player.AttackRange < Player.Distance(eTarget) && Player.Distance(eTarget) <= (Orbwalking.GetRealAutoAttackRange(eTarget) + W.Range) && W.IsReady() && useW)
            {
                W.Cast();
                Player.IssueOrder(GameObjectOrder.AttackUnit, eTarget);
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE && Player.GetManaPerc() > EManaHarass)
            {
                E.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

            if (eTarget.IsValidTarget(R.Range) && R.IsReady() && GetRStacks() < RMaxStacksHarass && useR)
            {
                R.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

            if (mustDebug)
                Game.PrintChat("Harass Finish");
        }

        public static void WaveClear()
        {
            if (mustDebug)
                Game.PrintChat("WaveClear Start");

            var MinionList = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.Enemy);

            if (MinionList.Count() == 0)
                return;

            var useE = Config.Item("UseELaneClear").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var EManaLaneClear = Config.Item("EManaLaneClear").GetValue<Slider>().Value;

            if (E.IsReady() && useE && Player.GetManaPerc() > EManaLaneClear)
            {
                var farmLocation = E.GetLineFarmLocation(MinionList, E.Width * 0.8f);
                if (farmLocation.MinionsHit >= 6)
                    E.Cast(farmLocation.Position, packetCast);
            }
        }

        public static void Freeze()
        {
            if (mustDebug)
                Game.PrintChat("Freeze Start");

        }

        public static void CastAssistedUlt()
        {
            if (mustDebug)
                Game.PrintChat("CastAssistedUlt Start");

            var eTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (eTarget.IsValidTarget(R.Range) && R.IsReady())
            {
                if (R.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast))
                    Game.PrintChat(string.Format("AssistedUlt fired"));
            }

            if (mustDebug)
                Game.PrintChat("CastAssistedUlt Finish");
        }

        private static void UpdateSpellsRange()
        {
            if (W.Level > 0)
                W.Range = 110 + W.Level * 20;
            if (R.Level > 0)
                R.Range = 900 + R.Level * 300;
        }

        private static void KillSteal()
        {
            var RKillSteal = Config.Item("RKillSteal").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (RKillSteal && R.IsReady())
            {
                foreach (var enemy in DevHelper.GetEnemyList())
                {
                    if (enemy.IsValidTarget(R.Range) && enemy.Health < Player.GetSpellDamage(enemy, SpellSlot.R))
                    {
                        if (R.CastIfHitchanceEquals(enemy, enemy.IsMoving ? HitChance.High : HitChance.Medium, packetCast))
                        {
                            Game.PrintChat("R KillSteal");
                            Utility.DelayAction.Add(0, () => DevHelper.Ping(enemy.ServerPosition));
                            Utility.DelayAction.Add(200, () => DevHelper.Ping(enemy.ServerPosition));
                            Utility.DelayAction.Add(400, () => DevHelper.Ping(enemy.ServerPosition));
                        }
                    }
                }
            }
        }

        private static void ChaseEnemyAfterDeath()
        {
            var ChaseEnemyAfterDeath = Config.Item("ChaseEnemyAfterDeath").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            if (HasPassiveBuff() && eTarget.IsValidTarget() && Player.Distance(eTarget.ServerPosition) > 10)
            {
                Player.SendMovePacket(eTarget.ServerPosition.To2D());
            }
        }

        private static int GetRStacks()
        {
            if (Player.HasBuff("KogMawLivingArtillery"))
            {
                return Player.Buffs
                    .Where(x => x.DisplayName == "KogMawLivingArtillery")
                    .Select(x => x.Count)
                    .First();
            }
            else
            {
                return 0;
            }
        }

        private static void onGameLoad(EventArgs args)
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

                Game.PrintChat(string.Format("<font color='#F7A100'>DevKogMaw Loaded v{0}</font>", Assembly.GetExecutingAssembly().GetName().Version));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                if (mustDebug)
                    Game.PrintChat(ex.Message);
            }
        }

        private static void InitializeAttachEvents()
        {
            if (mustDebug)
                Game.PrintChat("InitializeAttachEvents Start");

            Game.OnGameUpdate += OnTick;
            Game.OnGameSendPacket += Game_OnGameSendPacket;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;

            Config.Item("RDamage").ValueChanged += (object sender, OnValueChangeEventArgs e) => { Utility.HpBarDamageIndicator.Enabled = e.GetNewValue<bool>(); };
            if (Config.Item("RDamage").GetValue<bool>())
            {
                Utility.HpBarDamageIndicator.DamageToUnit = GetRDamage;
                Utility.HpBarDamageIndicator.Enabled = true;
            }

            if (mustDebug)
                Game.PrintChat("InitializeAttachEvents Finish");
        }

        private static void InitializeSpells()
        {
            if (mustDebug)
                Game.PrintChat("InitializeSpells Start");

            IgniteManager = new IgniteManager();
            BarrierManager = new BarrierManager();

            Q = new Spell(SpellSlot.Q, 1000);
            Q.SetSkillshot(0.25f, 70f, 1650f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 130);

            E = new Spell(SpellSlot.E, 1200);
            E.SetSkillshot(0.25f, 120f, 1400f, false, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 1200);
            R.SetSkillshot(1.5f, 225f, float.MaxValue, false, SkillshotType.SkillshotCircle);

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
            SkinManager.Add("Kog'Maw");
            SkinManager.Add("Caterpillar Kog'Maw");
            SkinManager.Add("Sonoran Kog'Maw");
            SkinManager.Add("Monarch Kog'Maw");
            SkinManager.Add("Reindeer Kog'Maw");
            SkinManager.Add("Lion Dance Kog'Maw");
            SkinManager.Add("Deep Sea Kog'Maw");
            SkinManager.Add("Jurassic Kog'Maw");

            if (mustDebug)
                Game.PrintChat("InitializeSkinManager Finish");
        }

        static void Game_OnGameSendPacket(GamePacketEventArgs args)
        {
            var BlockUlt = Config.Item("BlockUlt").GetValue<bool>();
            var ChaseEnemyAfterDeath = Config.Item("ChaseEnemyAfterDeath").GetValue<bool>();

            if (BlockUlt && args.PacketData[0] == Packet.C2S.Cast.Header)
            {
                var decodedPacket = Packet.C2S.Cast.Decoded(args.PacketData);
                if (decodedPacket.SourceNetworkId == Player.NetworkId && decodedPacket.Slot == SpellSlot.R)
                {
                    Vector3 vecCast = new Vector3(decodedPacket.ToX, decodedPacket.ToY, 0);
                    var query = DevHelper.GetEnemyList().Where(x => R.WillHit(x, vecCast));

                    if (query.Count() == 0)
                    {
                        args.Process = false;
                        Game.PrintChat(string.Format("Ult Blocked"));
                    }
                }
            }

            if (ChaseEnemyAfterDeath && args.PacketData[0] == Packet.C2S.Move.Header)
            { 
                if (HasPassiveBuff())
                {
                    var decodedPacket = Packet.C2S.Move.Decoded(args.PacketData);
                    if (decodedPacket.SourceNetworkId == Player.NetworkId)
                        args.Process = false;
                }
            }
        }

        static void Game_OnWndProc(WndEventArgs args)
        {
            if (MenuGUI.IsChatOpen)
                return;

            var UseAssistedUlt = Config.Item("UseAssistedUlt").GetValue<bool>();
            var AssistedUltKey = Config.Item("AssistedUltKey").GetValue<KeyBind>().Key;

            if (UseAssistedUlt && args.WParam == AssistedUltKey)
            {
                if (mustDebug)
                    Game.PrintChat("CastAssistedUlt");

                args.Process = false;
                CastAssistedUlt();
            }
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            Game.PrintChat(string.Format("OnPosibleToInterrupt -> {0} cast {1}", unit.SkinName, spell.SpellName));
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            Game.PrintChat(string.Format("OnEnemyGapcloser -> {0}", gapcloser.Sender.SkinName));

            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var BarrierGapCloser = Config.Item("BarrierGapCloser").GetValue<bool>();
            var BarrierGapCloserMinHealth = Config.Item("BarrierGapCloserMinHealth").GetValue<Slider>().Value;

            if (BarrierGapCloser && Player.GetHealthPerc() < BarrierGapCloserMinHealth && gapcloser.Sender.IsValidTarget(Player.AttackRange))
            {
                if (BarrierManager.Cast())
                    Game.PrintChat(string.Format("OnEnemyGapcloser -> BarrierGapCloser on {0} !", gapcloser.Sender.SkinName));
            }

        }

        private static float GetRDamage(Obj_AI_Hero enemy)
        {
            return (float)Damage.GetSpellDamage(Player, enemy, SpellSlot.R);
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
        }

        private static bool HasWBuff()
        {
            return Player.HasBuff("kogMawBioArcaneBarrage");
        }

        private static bool HasPassiveBuff()
        {
            return Player.HasBuff("KogMawIcathianSurprise");
        }

        private static void JungleStealAlert()
        {
            string[] monsterNames = { "LizardElder", "AncientGolem", "Worm", "Dragon" };
            var mobs = MinionManager.GetMinions(Player.ServerPosition, 5000, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            var query = mobs.Where(x =>
                monsterNames.Any(monster => x.BaseSkinName.Contains(monster)) &&
                x.GetHealthPerc() < 90 &&
                x.IsValid &&
                DevHelper.GetEnemyList().Any(enemy => x.Distance(enemy.ServerPosition) < 2000));

            if (query.Count() > 0)
            {
                var mob = query.FirstOrDefault();

                if (dtLastJungleStealAlert.AddSeconds(5) < DateTime.Now)
                {
                    if (mob.Distance(Player.ServerPosition) > R.Range)
                        Game.PrintChat("Jungle Steal Alert, Get closer!");
                    else
                        Game.PrintChat("Jungle Steal Alert! Wait...");
                    Utility.DelayAction.Add(0, () => DevHelper.Ping(mob.ServerPosition));
                    Utility.DelayAction.Add(200, () => DevHelper.Ping(mob.ServerPosition));
                    Utility.DelayAction.Add(400, () => DevHelper.Ping(mob.ServerPosition));
                    dtLastJungleStealAlert = DateTime.Now;
                }
            }
        }

        private static void JungleSteal()
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            string[] monsterNames = { "LizardElder", "AncientGolem", "Worm", "Dragon" };
            var mobs = MinionManager.GetMinions(Player.ServerPosition, R.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            var query = mobs.Where(x => 
                monsterNames.Any(monster => x.BaseSkinName.Contains(monster)) &&
                x.IsValidTarget(R.Range) && 
                x.Health < Player.GetSpellDamage(x, SpellSlot.R) &&
                DevHelper.GetEnemyList().Any(enemy => x.Distance(enemy.ServerPosition) < 1000));

            if (query.Count() > 0)
            {
                var mob = query.FirstOrDefault();
                R.Cast(mob.ServerPosition, packetCast);

                if (dtLastJungleSteal.AddSeconds(5) < DateTime.Now)
                {
                    Game.PrintChat("Jungle Steal!");
                    Utility.DelayAction.Add(0, () => DevHelper.Ping(mob.ServerPosition));
                    Utility.DelayAction.Add(200, () => DevHelper.Ping(mob.ServerPosition));
                    Utility.DelayAction.Add(400, () => DevHelper.Ping(mob.ServerPosition));
                    dtLastJungleSteal = DateTime.Now;
                }
            }
        }



        private static void InitializeMainMenu()
        {
            if (mustDebug)
                Game.PrintChat("InitializeMainMenu Start");

            Config = new Menu("DevKogMaw", "DevKogMaw", true);

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
            Config.SubMenu("Combo").AddItem(new MenuItem("RMaxStacksCombo", "R Max Stacks").SetValue(new Slider(3, 1, 5)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("EManaCombo", "Min Mana to E").SetValue(new Slider(20, 1, 100)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseRHarass", "Use R").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("RMaxStacksHarass", "R Max Stacks").SetValue(new Slider(1, 1, 5)));
            Config.SubMenu("Harass").AddItem(new MenuItem("EManaHarass", "Min Mana to E").SetValue(new Slider(50, 1, 100)));
            
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("EManaLaneClear", "Min Mana to E").SetValue(new Slider(50, 1, 100)));

            Config.AddSubMenu(new Menu("KillSteal", "KillSteal"));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("RKillSteal", "R KillSteal").SetValue(true));

            Config.AddSubMenu(new Menu("JungleSteal", "JungleSteal"));
            Config.SubMenu("JungleSteal").AddItem(new MenuItem("JungleStealAlert", "JungleSteal Alert").SetValue(true));
            Config.SubMenu("JungleSteal").AddItem(new MenuItem("JungleSteal", "JungleSteal R").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("ChaseEnemyAfterDeath", "Chase After Death").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("PacketCast", "Use PacketCast").SetValue(true));

            Config.AddSubMenu(new Menu("GapCloser", "GapCloser"));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("BarrierGapCloser", "Barrier onGapCloser").SetValue(true));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("BarrierGapCloserMinHealth", "Barrier MinHealth").SetValue(new Slider(40, 0, 100)));

            Config.AddSubMenu(new Menu("Ultimate", "Ultimate"));
            Config.SubMenu("Ultimate").AddItem(new MenuItem("UseAssistedUlt", "Use AssistedUlt").SetValue(true));
            Config.SubMenu("Ultimate").AddItem(new MenuItem("AssistedUltKey", "Assisted Ult Key").SetValue((new KeyBind("R".ToCharArray()[0], KeyBindType.Press))));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RDamage", "R Dmg onHPBar").SetValue(true));


            SkinManager.AddToMenu(ref Config);

            Config.AddToMainMenu();

            if (mustDebug)
                Game.PrintChat("InitializeMainMenu Finish");
        }
    }
}