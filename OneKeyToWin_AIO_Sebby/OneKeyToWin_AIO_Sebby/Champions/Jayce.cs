﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
namespace OneKeyToWin_AIO_Sebby.Champions
{
    class Jayce
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Spell Q, Q2, Qext, W, W2, E, E2, R;
        private float QMANA = 0, WMANA = 0, EMANA = 0, QMANA2 = 0, WMANA2 = 0, EMANA2 = 0, RMANA = 0;
        private float Qcd, Wcd, Ecd, Q2cd, W2cd, E2cd;
        private float Qcdt, Wcdt, Ecdt, Q2cdt, W2cdt, E2cdt;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1050);
            Qext = new Spell(SpellSlot.Q, 1650);
            Q2 = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W);
            W2 = new Spell(SpellSlot.W, 350);
            E = new Spell(SpellSlot.E, 650);
            E2 = new Spell(SpellSlot.E, 240);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 80, 1200, true, SkillshotType.SkillshotLine);
            Q.SetSkillshot(0.25f, 100, 1600, true, SkillshotType.SkillshotLine);
            Q2.SetTargetted(0.25f, float.MaxValue);
            E.SetSkillshot(0.1f, 120, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E2.SetTargetted(0.25f, float.MaxValue);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification", true).SetValue(false));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += OnUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
           
            if (args.Slot == SpellSlot.Q )
            {
                E.Cast(Player.ServerPosition .Extend(args.EndPosition, 100));
            }

        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if(sender.IsMe && E.IsReady() && Range && args.SData.Name == "jayceshockblast")
            {
                E.Cast(args.Start.Extend(args.End, 200));
                Program.debug(args.SData.Name);
            }
        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (W.IsReady() && Range && args.Target is Obj_AI_Hero)
            {
                W.Cast();
            }
        }

        private void OnUpdate(EventArgs args)
        {
            SetValue();

            if(Range)
            {

                if (Program.LagFree(2) && Q.IsReady() )
                    LogicQ();

                if (Program.LagFree(3) && W.IsReady() )
                    LogicW();
            }
            else
            {
                if (Program.LagFree(1) && E.IsReady())
                    LogicE2();

                if (Program.LagFree(2) && Q.IsReady())
                    LogicQ2();

                if (Program.LagFree(3) && W.IsReady())
                    LogicW2();
            }

            if (Program.LagFree(4) && R.IsReady())
                LogicR();
        }

        private void LogicQ()
        {

            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (Player.CountEnemiesInRange(900) > 0)
                t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);


            if (t.IsValidTarget())
            {
                var qDmg = Q.GetDamage(t);
                if (qDmg > t.Health)
                    Program.CastSpell(Q, t);
                else if (Program.Combo && Player.Mana > RMANA + QMANA)
                    Program.CastSpell(Q, t);
                else if ( Program.Farm && Player.Mana > RMANA + EMANA + QMANA + WMANA && !Player.UnderTurret(true) && OktwCommon.CanHarras())
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && Config.Item("haras" + enemy.ChampionName).GetValue<bool>()))
                    {
                        Program.CastSpell(Q, enemy);
                    }
                }

                else if ((Program.Combo || Program.Farm) && Player.Mana > RMANA + QMANA + EMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                        Q.Cast(enemy, true);
                }
            }
        }

        private void LogicW()
        {

        }

        private void LogicE()
        {

        }

        private void LogicW2()
        {
            if (Player.CountEnemiesInRange(500) > 0)
                W.Cast();
        }

        private void LogicE2()
        {
           
        }

        private void LogicQ2()
        {
            
        }

        private void LogicR()
        {

        }

        private void Drawing_OnDraw(EventArgs args)
        {

        }

        private float SetPlus(float valus)
        {
            if (valus < 0)
                return 0;
            else
                return valus;
        }

        private void SetValue()
        {
            if (Range)
            {
                Qcdt = Q.Instance.CooldownExpires;
                Wcdt = W.Instance.CooldownExpires;
                Ecd = E.Instance.CooldownExpires;

                QMANA = Q.Instance.ManaCost;
                WMANA = W.Instance.ManaCost;
                EMANA = E.Instance.ManaCost;
            }
            else
            {
                Q2cdt = Q.Instance.CooldownExpires;
                W2cdt = W.Instance.CooldownExpires;
                E2cdt = E.Instance.CooldownExpires;

                QMANA2 = Q.Instance.ManaCost;
                WMANA2 = W.Instance.ManaCost;
                EMANA2 = E.Instance.ManaCost;
            }

            Qcd = SetPlus(Qcdt - Game.Time);
            Wcd = SetPlus(Wcdt - Game.Time);
            Ecd = SetPlus(Ecdt - Game.Time);
            Q2cd = SetPlus(Q2cdt - Game.Time);
            W2cd = SetPlus(W2cdt - Game.Time);
            E2cd = SetPlus(E2cdt - Game.Time);
        }

        private bool Range { get { return Q.Instance.Name.Contains("jayceshockblast"); } }

    }
}