﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace OneKeyToWin_AIO_Sebby
{
    class Kalista
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        public Spell Q, Q2, W, E, R;
        public float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;

        private int count = 0 , countE = 0 , wCount = 0;
        private float grabTime = Game.Time, lastecast = 0f;

        private static Obj_AI_Hero AllyR;

        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1130);
            Q2 = new Spell(SpellSlot.Q, 1130);
            W = new Spell(SpellSlot.W, 5200);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 1400f);

            Q.SetSkillshot(0.25f, 30f, 1700f, true, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(0.25f, 30f, 1700f, false, SkillshotType.SkillshotLine);

            LoadMenuOKTW();

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
           // Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private void LoadMenuOKTW()
        {
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells", true).SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras Q").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("jungleE", "Jungle ks E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("countE", "Auto E if stacks", true).SetValue(new Slider(10, 30, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("farmEcount", "Auto E if minions", true).SetValue(new Slider(2, 10, 1)));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("minionE", "Auto E big minion", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("Edmg", "E % dmg adjust", true).SetValue(new Slider(100, 150, 50)));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("Edead", "Cast E before Kalista dead", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("autoW", "Auto W", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("Wdragon", "Auto W Dragon, Baron, Blue, Red", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoR", "Auto R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Balista Config").AddItem(new MenuItem("balista", "Balista R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Balista Config").AddItem(new MenuItem("rangeBalista", "Balista min range", true).SetValue(new Slider(300, 1400, 0)));
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "KalistaExpungeWrapper")
                {
                    lastecast = Game.Time;
                    Orbwalking.ResetAutoAttackTimer();
                }
                if (args.SData.Name == "kalistaw")
                {
                    wCount++;
                }
            }

            if (R.IsReady() && sender.IsAlly && args.SData.Name == "RocketGrab" && Player.Distance(sender.Position) < R.Range && Player.Distance(sender.Position) > Config.Item("rangeBalista", true).GetValue<Slider>().Value)
            {
                grabTime = Game.Time;
            }

            if ( Config.Item("Edead", true).GetValue<bool>() && E.IsReady()  && sender.IsEnemy && sender.IsValidTarget(1500))
            {

                double dmg = 0;

                if (args.Target != null && args.Target.IsMe)
                {
                    dmg = dmg + sender.GetSpellDamage(Player, args.SData.Name);
                }
                else 
                {
                    var castArea = Player.Distance(args.End) * (args.End - Player.ServerPosition).Normalized() + Player.ServerPosition;
                    if (castArea.Distance(Player.ServerPosition) < Player.BoundingRadius / 2)
                    {
                        dmg = dmg + sender.GetSpellDamage(Player, args.SData.Name);
                    }
                }

                if (Player.Health - dmg < (Player.CountEnemiesInRange(600) * Player.Level * 10 ) + (Player.Level * 10))
                {
                     CastE();
                }
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if ( Player.Mana > QMANA + EMANA)
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Q.IsReady() && Target.IsValidTarget(Q.Range) )
                    Q.Cast(Target, true);
            }
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.HasBuff("Recall"))
                return;
            if (E.IsReady())
            {
                farm();
                LogicE();
                JungleE();
            }

            if (R.IsReady() && Config.Item("balista", true).GetValue<bool>() && AllyR != null && AllyR.IsVisible && AllyR.Distance(Player.Position) < R.Range && AllyR.ChampionName == "Blitzcrank" && Player.Distance(AllyR.Position) > Config.Item("rangeBalista", true).GetValue<Slider>().Value)
            {
                foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget() && !enemy.IsDead && enemy.HasBuff("rocketgrab2")))
                {
                    Program.debug("Activ");
                    R.Cast();
                }
                if (Game.Time - grabTime < 1)
                    return;
            }

            if (Program.LagFree(0))
            {
                SetMana();
                countE = Config.Item("countE", true).GetValue<Slider>().Value;
            }

            if (Program.LagFree(1) && Q.IsReady() && !Player.IsWindingUp && !Player.IsDashing())
                LogicQ();

            if (Program.LagFree(3) && R.IsReady() && Config.Item("autoR", true).GetValue<bool>())
                LogicR();

            if (Program.LagFree(4) && W.IsReady())
                LogicW();
        }

        private void LogicW()
        {
            if (Config.Item("Wdragon", true).GetValue<bool>() &&  !Orbwalker.GetTarget().IsValidTarget() && !Program.Combo && Player.CountEnemiesInRange(800)==0)
            {
                if (wCount > 0)
                {
                    Vector3 baronPos;
                    baronPos.X = 5232;
                    baronPos.Y = 10788;
                    baronPos.Z = 0;
                    if (Player.Distance(baronPos) < 5000)
                        W.Cast(baronPos);
                }
                if (wCount == 0)
                {
                    Vector3 dragonPos;
                    dragonPos.X = 9919f;
                    dragonPos.Y = 4475f;
                    dragonPos.Z = 0f;
                    if (Player.Distance(dragonPos) < 5000)
                        W.Cast(dragonPos);
                    else
                        wCount ++;
                    return;
                }

                if (wCount == 1)
                {
                    Vector3 redPos;
                    redPos.X = 8022;
                    redPos.Y = 4156;
                    redPos.Z = 0;
                    if (Player.Distance(redPos) < 5000)
                        W.Cast(redPos);
                    else
                        wCount++;
                    return;
                }
                if (wCount == 2)
                {
                    Vector3 bluePos;
                    bluePos.X = 11396;
                    bluePos.Y = 7076;
                    bluePos.Z = 0;
                    if (Player.Distance(bluePos) < 5000)
                        W.Cast(bluePos);
                    else
                        wCount++;
                    return;
                }
                if (wCount > 2)
                {
                    wCount = 0;
                }
            }
        }
        private void JungleE()
        {

            if (!Config.Item("jungleE", true).GetValue<bool>())
                return;

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                var dmg = GetEdmg(mob);


                if (mob.Name.Contains("Baron") && Player.HasBuff("barontarget"))
                {
                    dmg = dmg * 0.5f;
                }
                if (mob.Name.Contains("Dragon") && Player.HasBuff("s5test_dragonslayerbuff"))
                {
                    dmg = dmg * (1 - (0.07f * ObjectManager.Player.GetBuffCount("s5test_dragonslayerbuff")));
                }

                if (mob.Health < dmg)
                    CastE();
            }
        }
        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget())
            {
                var poutput = Q.GetPrediction(t);
                var col = poutput.CollisionObjects;
                bool cast = true;
                foreach (var colobj in col)
                {
                    if (Q.GetDamage(colobj) < colobj.Health)
                        cast = false;
                }

                var qDmg = Q.GetDamage(t) + Player.GetAutoAttackDamage(t);
                var eDmg = GetEdmg(t);

                if (qDmg > t.Health && eDmg < t.Health && Player.Mana > QMANA + EMANA)
                    castQ(cast, t);
                else if ((qDmg * 1.1) + eDmg > t.Health && eDmg < t.Health && Player.Mana > QMANA + EMANA && Orbwalking.InAutoAttackRange(t))
                    castQ(cast, t);
                else if (Program.Combo && Player.Mana > RMANA + QMANA + EMANA + WMANA && (!Orbwalking.InAutoAttackRange(t) || Player.CountEnemiesInRange(400) > 0))
                    castQ(cast, t);
                else if (Program.Farm && Config.Item("haras" + t.ChampionName).GetValue<bool>() && !Player.UnderTurret(true) && Player.ManaPercent > 50 && !Orbwalking.InAutoAttackRange(t))
                    castQ(cast, t);
                else if ((Program.Combo || Program.Farm) && Player.Mana > RMANA + QMANA + EMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                        Q.Cast(enemy, true);
                }   
            }
        }


        private float GetEdmg( Obj_AI_Base t)
        {
            var defuffer = 1f;
            if (Player.HasBuff("summonerexhaust"))
                defuffer *= .4f;
            return (defuffer * E.GetDamage(t) * 0.01f * (float)Config.Item("Edmg", true).GetValue<Slider>().Value) - t.HPRegenRate;
        }

        private void CastE()
        {
            if (Game.Time - lastecast < 0.300)
            {
                return;
            }
            else
            { 
                lastecast = Game.Time;
                E.Cast();
            }
        }

        void castQ(bool cast, Obj_AI_Base t)
        {
            if (cast)
                Program.CastSpell(Q2, t);
            else
                Program.CastSpell(Q, t);
        }

        private void LogicE()
        {
            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(E.Range) && OktwCommon.ValidUlt(target)))
            {
                var Edmg = GetEdmg(target);
                if (target.Health  < Edmg)
                {
                    CastE();
                    return;
                }
                if (0 < Edmg && count > 0)
                {
                    CastE();
                    return;
                }
                
                if (GetRStacks(target) >= countE && (GetPassiveTime(target) < 0.5 || Player.ServerPosition.Distance(target.ServerPosition) > E.Range - 150 
                    || Player.Health < Player.MaxHealth * 0.3)
                    && Player.Mana > RMANA + QMANA + EMANA + WMANA
                    && Player.CountEnemiesInRange(800) == 0)
                {
                    CastE();
                    return;
                }
            }
            if (Program.LaneClear && (count >= Config.Item("farmEcount", true).GetValue<Slider>().Value  || ((Player.UnderTurret(false) && !Player.UnderTurret(true)) && count > 0 && Player.Mana > RMANA + QMANA + EMANA)))
            {
                CastE();
                return;
            }
        }

        private float GetPassiveTime(Obj_AI_Base target)
        {
            return
                target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Name == "kalistaexpungemarker")
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault() - Game.Time;
        }
        private int GetRStacks(Obj_AI_Base target)
        {
            foreach (var buff in target.Buffs)
            {
                if (buff.Name == "kalistaexpungemarker")
                    return buff.Count;
            }
            return 0;
        }

        private int farm()
        {
            count = 0;
            int outRange = 0;
            foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValidTarget(E.Range) && minion.IsEnemy))
            {
                var minionDmg = E.GetDamage(minion); 
                if (minion.Health < minionDmg)
                {

                    if (GetPassiveTime(minion) > 0.5 && HealthPrediction.LaneClearHealthPrediction(minion, 1000) > minion.GetAutoAttackDamage(minion) * 3 + Player.GetAutoAttackDamage(minion))
                    {
                        count++;
                        if (!Orbwalking.InAutoAttackRange(minion))
                        {
                            outRange++;
                        }
                    }
                    if ((minion.SkinName.ToLower().Contains("siege") || minion.SkinName.ToLower().Contains("super")) && Config.Item("minionE", true).GetValue<bool>())
                    {
                        outRange++; 
                    }
                }
            }
            if (Program.Farm && outRange > 0)
            {
                CastE();
                return 0;
            }
            return count;
        }

        private void LogicR()
        {
            if (Player.IsRecalling() || Player.InFountain())
                return;

            if (AllyR == null)
            {
                foreach (var ally in Program.Allies.Where(ally => !ally.IsDead && !ally.IsMe && ally.HasBuff("kalistacoopstrikeally")))
                {
                    AllyR = ally;
                    break;
                }
            }
            else if (AllyR.IsVisible && AllyR.Distance(Player.Position) < R.Range)
            {
                if (AllyR.Health < AllyR.CountEnemiesInRange(600) * AllyR.Level * 30)
                {
                    R.Cast();
                }
            }
        }
        private void SetMana()
        {
            if ((Config.Item("manaDisable", true).GetValue<bool>() && Program.Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;

            if (!R.IsReady())
                RMANA = QMANA - Player.PARRegenRate * Q.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost;
        }

        public static void drawText(string msg, Obj_AI_Base Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1] - 200, color, msg);
        }

        private void Drawing_OnDraw(EventArgs args)
        {

            foreach (var enemy in Program.Enemies.Where(target => target.IsValidTarget(E.Range + 500) && target.IsEnemy))
            {
                float hp = enemy.Health - E.GetDamage(enemy);
                int stack = GetRStacks(enemy);
                float dmg = (float)Player.GetAutoAttackDamage(enemy) * 2f;
                if (stack > 0)
                    dmg = (float)Player.GetAutoAttackDamage(enemy) + (E.GetDamage(enemy) / (float)stack);

                if (hp > 0)
                    drawText((int)((E.GetDamage(enemy) / enemy.Health) * 100) + " %", enemy, System.Drawing.Color.GreenYellow);
                else
                    drawText("KILL E", enemy, System.Drawing.Color.Red);
            }

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                drawText((int)((E.GetDamage(mob) / mob.Health) * 100) + " %", mob, System.Drawing.Color.GreenYellow);

            }

            if (Config.Item("qRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (Q.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
            }

            if (Config.Item("eRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (E.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
            }
            if (Config.Item("rRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (R.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
            }
        }

    }
}
