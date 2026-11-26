using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using Microsoft.Xna.Framework;

namespace StardewQOL
{
    /// <summary>
    /// Ponto de entrada do Stardew QOL. Coordena os módulos de tempo e energia,
    /// registra as opções do GMCM e acompanha os eventos principais do SMAPI.
    /// </summary>
    public partial class ModEntry : Mod
    {

        // Configuração persistida em config.json e compartilhada pelos dois módulos.
        private ModConfig Config = new();
        private ClimateSystem? climateSystem;



        /// <summary>
        /// Carrega e valida as configurações, salva os valores corrigidos e registra
        /// os eventos do SMAPI utilizados durante a execução do mod.
        /// </summary>
        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();
            this.Config.Validar();
            this.climateSystem = new ClimateSystem(this.Monitor, this.Config.Clima);
            helper.WriteConfig(this.Config);

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        /// <summary>
        /// Registra as páginas e opções do Generic Mod Config Menu.
        /// Se o GMCM não estiver instalado, o mod continua funcionando via config.json.
        /// </summary>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            IGenericModConfigMenuApi? configMenu =
                this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

            if (configMenu is null)
            {
                this.Monitor.Log(
                    "Generic Mod Config Menu não está instalado. A configuração continua disponível pelo arquivo config.json.",
                    LogLevel.Trace
                );
                return;
            }

            // Páginas principais e subpáginas. Os textos vêm dos arquivos i18n.
            configMenu.Register(this.ModManifest, this.ResetConfig, this.SaveConfig, titleScreenOnly: false);

            // Interruptor geral: mantém o GMCM acessível mesmo quando o mod está desligado.
            configMenu.AddPageLink(this.ModManifest, "mod",
                () => "> " + this.Helper.Translation.Get("config.page.mod.name"),
                () => this.Helper.Translation.Get("config.page.mod.tooltip"));

            configMenu.AddPageLink(this.ModManifest, "time",
                () => "> " + this.Helper.Translation.Get("config.page.time.name"),
                () => this.Helper.Translation.Get("config.page.time.tooltip"));
            configMenu.AddPageLink(this.ModManifest, "energy",
                () => "> " + this.Helper.Translation.Get("config.page.energy.name"),
                () => this.Helper.Translation.Get("config.page.energy.tooltip"));
            configMenu.AddPageLink(this.ModManifest, "health",
                () => "> " + this.Helper.Translation.Get("config.page.health.name"),
                () => this.Helper.Translation.Get("config.page.health.tooltip"));
            configMenu.AddPageLink(this.ModManifest, "climate",
                () => "> " + this.Helper.Translation.Get("config.page.climate.name"),
                () => this.Helper.Translation.Get("config.page.climate.tooltip"));
            configMenu.AddPageLink(this.ModManifest, "notifications",
                () => "> " + this.Helper.Translation.Get("config.page.notifications.name"),
                () => this.Helper.Translation.Get("config.page.notifications.tooltip"));

            configMenu.AddPage(this.ModManifest, "mod",
                () => this.Helper.Translation.Get("config.page.mod.name"));
            configMenu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get("config.page.mod.description"));
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.mod.enabled.name"),
                tooltip: () => this.Helper.Translation.Get("config.mod.enabled.tooltip"),
                getValue: () => this.Config.ModAtivado,
                setValue: value =>
                {
                    this.Config.ModAtivado = value;
                    if (!value)
                    {
                        // Desliga imediatamente os efeitos ativos e devolve o relógio ao padrão.
                        this.usandoVelocidadeSentado = false;
                        this.inicioCiclo24hUtc = null;
                        this.ResetarEstadoEnergia();
                        this.ResetarEstadoSaude();
                        const int velocidadePadrao = 7000;
                        if (Game1.realMilliSecondsPerGameTenMinutes != velocidadePadrao)
                            Game1.realMilliSecondsPerGameTenMinutes = velocidadePadrao;
                    }
                    else if (Context.IsWorldReady && this.Config.TempoModuloAtivado
                        && this.Config.Ciclo24hEfetivamenteAtivado)
                    {
                        this.IniciarCicloPeloHorarioAtual();
                    }
                },
                fieldId: "mod-ativado");

            configMenu.AddPage(this.ModManifest, "time",
                () => this.Helper.Translation.Get("config.page.time.name"));
            configMenu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get("config.page.time.description"));
            configMenu.AddPageLink(this.ModManifest, "time-module",
                () => "> " + this.Helper.Translation.Get("config.time.module.page.name"),
                () => this.Helper.Translation.Get("config.time.module.page.tooltip"));
            configMenu.AddPageLink(this.ModManifest, "time-custom",
                () => "> " + this.Helper.Translation.Get("config.page.custom-time.name"),
                () => this.Helper.Translation.Get("config.page.custom-time.tooltip"));
            configMenu.AddPageLink(this.ModManifest, "time-seated",
                () => "> " + this.Helper.Translation.Get("config.page.seated-time.name"),
                () => this.Helper.Translation.Get("config.page.seated-time.tooltip"));
            configMenu.AddPageLink(this.ModManifest, "time-24h",
                () => "> " + this.Helper.Translation.Get("config.page.24h.name"),
                () => this.Helper.Translation.Get("config.page.24h.tooltip"));

            configMenu.AddPage(this.ModManifest, "time-module",
                () => this.Helper.Translation.Get("config.time.module.page.name"));
            configMenu.AddPageLink(this.ModManifest, "time",
                () => "@ " + this.Helper.Translation.Get("config.page.back-to-time.name"),
                () => this.Helper.Translation.Get("config.page.back-to-time.tooltip"));
            configMenu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get("config.time.module.page.description"));
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.time.module.enabled.name"),
                tooltip: () => this.Helper.Translation.Get("config.time.module.enabled.tooltip"),
                getValue: () => this.Config.TempoModuloAtivado,
                setValue: value =>
                {
                    this.Config.TempoModuloAtivado = value;
                    if (!value)
                    {
                        this.usandoVelocidadeSentado = false;
                        this.inicioCiclo24hUtc = null;
                    }
                    else if (this.Config.Ciclo24hEfetivamenteAtivado && Context.IsWorldReady)
                    {
                        this.IniciarCicloPeloHorarioAtual();
                    }
                },
                fieldId: "tempo-modulo-ativado");

            configMenu.AddPage(this.ModManifest, "time-custom",
                () => this.Helper.Translation.Get("config.page.custom-time.name"));
            configMenu.AddPageLink(this.ModManifest, "time",
                () => "@ " + this.Helper.Translation.Get("config.page.back-to-time.name"),
                () => this.Helper.Translation.Get("config.page.back-to-time.tooltip"));
            configMenu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get("config.page.custom-time.description"));
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.custom-time.enabled.name"),
                tooltip: () => this.Helper.Translation.Get("config.custom-time.enabled.tooltip"),
                getValue: () => this.Config.TempoPersonalizadoAtivado,
                setValue: value => this.Config.TempoPersonalizadoAtivado = value,
                fieldId: "tempo-personalizado-ativado");
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.seconds-per-ten-minutes.name"),
                tooltip: () => this.Helper.Translation.Get("config.seconds-per-ten-minutes.tooltip"),
                getValue: () => this.Config.SegundosPorDezMinutos,
                setValue: value => { this.Config.SegundosPorDezMinutos = value; this.Config.Validar(); },
                min: 7.0f, max: 720.0f, interval: 0.5f,
                formatValue: value => $"{value:0.0} s",
                fieldId: "segundos-por-dez-minutos");

            configMenu.AddPage(this.ModManifest, "time-seated",
                () => this.Helper.Translation.Get("config.page.seated-time.name"));
            configMenu.AddPageLink(this.ModManifest, "time",
                () => "@ " + this.Helper.Translation.Get("config.page.back-to-time.name"),
                () => this.Helper.Translation.Get("config.page.back-to-time.tooltip"));
            configMenu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get("config.page.seated-time.description"));
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.seated-time.enabled.name"),
                tooltip: () => this.Helper.Translation.Get("config.seated-time.enabled.tooltip"),
                getValue: () => this.Config.AceleracaoMesaAtivada,
                setValue: value => this.Config.AceleracaoMesaAtivada = value,
                fieldId: "aceleracao-mesa-ativada");
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.seated-time.fountain.name"),
                tooltip: () => this.Helper.Translation.Get("config.seated-time.fountain.tooltip"),
                getValue: () => this.Config.AceleracaoChafarizAtivada,
                setValue: value => this.Config.AceleracaoChafarizAtivada = value,
                fieldId: "aceleracao-chafariz-ativada");
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.seated-time.saloon.name"),
                tooltip: () => this.Helper.Translation.Get("config.seated-time.saloon.tooltip"),
                getValue: () => this.Config.AceleracaoSaloonAtivada,
                setValue: value => this.Config.AceleracaoSaloonAtivada = value,
                fieldId: "aceleracao-saloon-ativada");
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.seated-time.library.name"),
                tooltip: () => this.Helper.Translation.Get("config.seated-time.library.tooltip"),
                getValue: () => this.Config.AceleracaoBibliotecaAtivada,
                setValue: value => this.Config.AceleracaoBibliotecaAtivada = value,
                fieldId: "aceleracao-biblioteca-ativada");
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.seated-time.beach.name"),
                tooltip: () => this.Helper.Translation.Get("config.seated-time.beach.tooltip"),
                getValue: () => this.Config.AceleracaoPraiaAtivada,
                setValue: value => this.Config.AceleracaoPraiaAtivada = value,
                fieldId: "aceleracao-praia-ativada");
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.seated-time.speed.name"),
                tooltip: () => this.Helper.Translation.Get("config.seated-time.speed.tooltip"),
                getValue: () => this.Config.SegundosPorDezMinutosSentado,
                setValue: value => { this.Config.SegundosPorDezMinutosSentado = value; this.Config.Validar(); },
                min: 1.0f, max: 3.5f, interval: 0.5f,
                formatValue: value => $"{value:0.0} s",
                fieldId: "segundos-por-dez-minutos-sentado");

            configMenu.AddPage(this.ModManifest, "time-24h",
                () => this.Helper.Translation.Get("config.page.24h.name"));
            configMenu.AddPageLink(this.ModManifest, "time",
                () => "@ " + this.Helper.Translation.Get("config.page.back-to-time.name"),
                () => this.Helper.Translation.Get("config.page.back-to-time.tooltip"));
            configMenu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get("config.page.24h.description"));
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.24h.cycle.name"),
                tooltip: () => this.Helper.Translation.Get("config.24h.cycle.tooltip"),
                getValue: () => this.Config.Ciclo24HorasAtivado,
                setValue: value =>
                {
                    this.Config.Ciclo24HorasAtivado = value;
                    if (this.Config.TempoModuloAtivado && this.Config.Ciclo24hEfetivamenteAtivado && this.inicioCiclo24hUtc is null && Context.IsWorldReady)
                        this.IniciarCicloPeloHorarioAtual();
                    if (!this.Config.Ciclo24hEfetivamenteAtivado)
                        this.inicioCiclo24hUtc = null;
                },
                fieldId: "ciclo-24h-ativado");
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.24h.balance.name"),
                tooltip: () => this.Helper.Translation.Get("config.24h.balance.tooltip"),
                getValue: () => this.Config.Balanceamento24HorasAtivado,
                setValue: value => this.Config.Balanceamento24HorasAtivado = value,
                fieldId: "balanceamento-24h-ativado");

            configMenu.AddPage(this.ModManifest, "energy",
                () => this.Helper.Translation.Get("config.page.energy.name"));
            configMenu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get("config.page.energy.description"));
            configMenu.AddPageLink(this.ModManifest, "energy-module",
                () => "> " + this.Helper.Translation.Get("config.energy.module.page.name"),
                () => this.Helper.Translation.Get("config.energy.module.page.tooltip"));
            configMenu.AddPageLink(this.ModManifest, "energy-general", () => "> " + this.Helper.Translation.Get("config.energy.general.name"));
            configMenu.AddPageLink(this.ModManifest, "energy-walking", () => "> " + this.Helper.Translation.Get("config.energy.walking.name"));
            configMenu.AddPageLink(this.ModManifest, "energy-idle", () => "> " + this.Helper.Translation.Get("config.energy.idle.name"));
            configMenu.AddPageLink(this.ModManifest, "energy-seated", () => "> " + this.Helper.Translation.Get("config.energy.seated.name"));
            configMenu.AddPageLink(this.ModManifest, "energy-fishing", () => "> " + this.Helper.Translation.Get("config.energy.fishing.name"));
            configMenu.AddPageLink(this.ModManifest, "energy-running", () => "> " + this.Helper.Translation.Get("config.energy.running.name"));
            configMenu.AddPageLink(this.ModManifest, "energy-cooldown", () => "> " + this.Helper.Translation.Get("config.energy.cooldown.name"));

            configMenu.AddPage(this.ModManifest, "energy-module",
                () => this.Helper.Translation.Get("config.energy.module.page.name"));
            configMenu.AddPageLink(this.ModManifest, "energy",
                () => "@ " + this.Helper.Translation.Get("config.page.back-to-energy.name"));
            configMenu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get("config.energy.module.page.description"));
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.energy.module.enabled.name"),
                tooltip: () => this.Helper.Translation.Get("config.energy.module.enabled.tooltip"),
                getValue: () => this.Config.EnergiaModuloAtivado,
                setValue: value => this.Config.EnergiaModuloAtivado = value,
                fieldId: "energia-modulo-ativado");

            this.RegistrarPaginaEnergia(configMenu, "energy-general", "config.energy.general", () => this.Config.RecuperacaoGeralAtivada, v => this.Config.RecuperacaoGeralAtivada = v, () => this.Config.EnergiaGeralQuantidade, v => this.Config.EnergiaGeralQuantidade = v, () => this.Config.EnergiaGeralIntervalo, v => this.Config.EnergiaGeralIntervalo = v, 5.0f, 1.5f, "energia-geral");
            this.RegistrarPaginaEnergia(configMenu, "energy-walking", "config.energy.walking", () => this.Config.RecuperacaoAndandoAtivada, v => this.Config.RecuperacaoAndandoAtivada = v, () => this.Config.EnergiaAndandoQuantidade, v => this.Config.EnergiaAndandoQuantidade = v, () => this.Config.EnergiaAndandoIntervalo, v => this.Config.EnergiaAndandoIntervalo = v, 8.0f, 1.0f, "energia-andando");
            this.RegistrarPaginaEnergia(configMenu, "energy-idle", "config.energy.idle", () => this.Config.RecuperacaoParadoAtivada, v => this.Config.RecuperacaoParadoAtivada = v, () => this.Config.EnergiaParadoQuantidade, v => this.Config.EnergiaParadoQuantidade = v, () => this.Config.EnergiaParadoIntervalo, v => this.Config.EnergiaParadoIntervalo = v, 4.0f, 1.5f, "energia-parado");
            this.RegistrarPaginaEnergia(configMenu, "energy-seated", "config.energy.seated", () => this.Config.RecuperacaoSentadoAtivada, v => this.Config.RecuperacaoSentadoAtivada = v, () => this.Config.EnergiaSentadoQuantidade, v => this.Config.EnergiaSentadoQuantidade = v, () => this.Config.EnergiaSentadoIntervalo, v => this.Config.EnergiaSentadoIntervalo = v, 3.0f, 2.0f, "energia-sentado");
            this.RegistrarPaginaEnergia(configMenu, "energy-fishing", "config.energy.fishing", () => this.Config.RecuperacaoPescaAtivada, v => this.Config.RecuperacaoPescaAtivada = v, () => this.Config.EnergiaPescaQuantidade, v => this.Config.EnergiaPescaQuantidade = v, () => this.Config.EnergiaPescaIntervalo, v => this.Config.EnergiaPescaIntervalo = v, 12.0f, 2.0f, "energia-pesca");

            configMenu.AddPage(this.ModManifest, "energy-cooldown", () => this.Helper.Translation.Get("config.energy.cooldown.name"));
            configMenu.AddPageLink(this.ModManifest, "energy", () => "@ " + this.Helper.Translation.Get("config.page.back-to-energy.name"));
            configMenu.AddParagraph(this.ModManifest, () => this.Helper.Translation.Get("config.energy.cooldown.description"));
            configMenu.AddBoolOption(this.ModManifest, () => this.Config.CooldownEnergiaAtivado, v => this.Config.CooldownEnergiaAtivado = v, () => this.Helper.Translation.Get("config.energy.cooldown.enabled.name"), () => this.Helper.Translation.Get("config.energy.cooldown.enabled.tooltip"), "energia-cooldown-ativado");
            configMenu.AddNumberOption(this.ModManifest, () => this.Config.CooldownEnergiaSegundos, v => { this.Config.CooldownEnergiaSegundos = v; this.Config.Validar(); }, () => this.Helper.Translation.Get("config.energy.cooldown.seconds.name"), () => this.Helper.Translation.Get("config.energy.cooldown.seconds.tooltip"), 3.0f, 5.0f, 1.0f, v => $"{v:0.0} s", "energia-cooldown-segundos");

            configMenu.AddPage(this.ModManifest, "energy-running", () => this.Helper.Translation.Get("config.energy.running.name"));
            configMenu.AddPageLink(this.ModManifest, "energy", () => "@ " + this.Helper.Translation.Get("config.page.back-to-energy.name"));
            configMenu.AddParagraph(this.ModManifest, () => this.Helper.Translation.Get("config.energy.running.description"));
            configMenu.AddBoolOption(this.ModManifest, () => this.Config.CustoEnergiaCorridaAtivado, v => this.Config.CustoEnergiaCorridaAtivado = v, () => this.Helper.Translation.Get("config.energy.running.enabled.name"), () => this.Helper.Translation.Get("config.energy.running.enabled.tooltip"), "energia-corrida-ativado");
            configMenu.AddNumberOption(this.ModManifest, () => this.Config.EnergiaCorridaQuantidade, v => { this.Config.EnergiaCorridaQuantidade = v; this.Config.Validar(); }, () => this.Helper.Translation.Get("config.energy.running.amount.name"), () => this.Helper.Translation.Get("config.energy.running.amount.tooltip"), 1.0f, 2.0f, 1.0f, v => $"{v:0.0}", "energia-corrida-quantidade");
            configMenu.AddNumberOption(this.ModManifest, () => this.Config.EnergiaCorridaIntervalo, v => { this.Config.EnergiaCorridaIntervalo = v; this.Config.Validar(); }, () => this.Helper.Translation.Get("config.energy.running.interval.name"), () => this.Helper.Translation.Get("config.energy.running.interval.tooltip"), 2.0f, 4.0f, 1.0f, v => $"{v:0.0} s", "energia-corrida-intervalo");

            // Módulo Saúde: estrutura paralela à Energia, com cooldown de dano compartilhado.
            configMenu.AddPage(this.ModManifest, "health",
                () => this.Helper.Translation.Get("config.page.health.name"));
            configMenu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get("config.page.health.description"));
            configMenu.AddPageLink(this.ModManifest, "health-module", () => "> " + this.Helper.Translation.Get("config.health.module.page.name"), () => this.Helper.Translation.Get("config.health.module.page.tooltip"));
            configMenu.AddPageLink(this.ModManifest, "health-general", () => "> " + this.Helper.Translation.Get("config.health.general.name"));
            configMenu.AddPageLink(this.ModManifest, "health-walking", () => "> " + this.Helper.Translation.Get("config.health.walking.name"));
            configMenu.AddPageLink(this.ModManifest, "health-idle", () => "> " + this.Helper.Translation.Get("config.health.idle.name"));
            configMenu.AddPageLink(this.ModManifest, "health-seated", () => "> " + this.Helper.Translation.Get("config.health.seated.name"));
            configMenu.AddPageLink(this.ModManifest, "health-damage-cooldown", () => "> " + this.Helper.Translation.Get("config.health.damage-cooldown.name"));

            configMenu.AddPage(this.ModManifest, "health-module", () => this.Helper.Translation.Get("config.health.module.page.name"));
            configMenu.AddPageLink(this.ModManifest, "health", () => "@ " + this.Helper.Translation.Get("config.page.back-to-health.name"));
            configMenu.AddParagraph(this.ModManifest, () => this.Helper.Translation.Get("config.health.module.page.description"));
            configMenu.AddBoolOption(this.ModManifest,
                () => this.Config.SaudeModuloAtivado,
                v => this.Config.SaudeModuloAtivado = v,
                () => this.Helper.Translation.Get("config.health.module.enabled.name"),
                () => this.Helper.Translation.Get("config.health.module.enabled.tooltip"),
                "saude-modulo-ativado");

            this.RegistrarPaginaSaude(configMenu, "health-general", "config.health.general", () => this.Config.RecuperacaoSaudeGeralAtivada, v => this.Config.RecuperacaoSaudeGeralAtivada = v, () => this.Config.SaudeGeralQuantidade, v => this.Config.SaudeGeralQuantidade = v, () => this.Config.SaudeGeralIntervalo, v => this.Config.SaudeGeralIntervalo = v, 5.0f, 1.5f, "saude-geral");
            this.RegistrarPaginaSaude(configMenu, "health-walking", "config.health.walking", () => this.Config.RecuperacaoSaudeAndandoAtivada, v => this.Config.RecuperacaoSaudeAndandoAtivada = v, () => this.Config.SaudeAndandoQuantidade, v => this.Config.SaudeAndandoQuantidade = v, () => this.Config.SaudeAndandoIntervalo, v => this.Config.SaudeAndandoIntervalo = v, 8.0f, 1.0f, "saude-andando");
            this.RegistrarPaginaSaude(configMenu, "health-idle", "config.health.idle", () => this.Config.RecuperacaoSaudeParadoAtivada, v => this.Config.RecuperacaoSaudeParadoAtivada = v, () => this.Config.SaudeParadoQuantidade, v => this.Config.SaudeParadoQuantidade = v, () => this.Config.SaudeParadoIntervalo, v => this.Config.SaudeParadoIntervalo = v, 4.0f, 1.5f, "saude-parado");
            this.RegistrarPaginaSaude(configMenu, "health-seated", "config.health.seated", () => this.Config.RecuperacaoSaudeSentadoAtivada, v => this.Config.RecuperacaoSaudeSentadoAtivada = v, () => this.Config.SaudeSentadoQuantidade, v => this.Config.SaudeSentadoQuantidade = v, () => this.Config.SaudeSentadoIntervalo, v => this.Config.SaudeSentadoIntervalo = v, 3.0f, 2.0f, "saude-sentado");

            configMenu.AddPage(this.ModManifest, "health-damage-cooldown", () => this.Helper.Translation.Get("config.health.damage-cooldown.name"));
            configMenu.AddPageLink(this.ModManifest, "health", () => "@ " + this.Helper.Translation.Get("config.page.back-to-health.name"));
            configMenu.AddParagraph(this.ModManifest, () => this.Helper.Translation.Get("config.health.damage-cooldown.description"));
            configMenu.AddBoolOption(this.ModManifest, () => this.Config.CooldownDanoAtivado, v => this.Config.CooldownDanoAtivado = v, () => this.Helper.Translation.Get("config.health.damage-cooldown.enabled.name"), () => this.Helper.Translation.Get("config.health.damage-cooldown.enabled.tooltip"), "cooldown-dano-ativado");
            configMenu.AddNumberOption(this.ModManifest, () => this.Config.CooldownDanoSegundos, v => { this.Config.CooldownDanoSegundos = v; this.Config.Validar(); }, () => this.Helper.Translation.Get("config.health.damage-cooldown.seconds.name"), () => this.Helper.Translation.Get("config.health.damage-cooldown.seconds.tooltip"), 2.0f, 4.0f, 1.0f, v => $"{v:0.0} s", "cooldown-dano-segundos");

            // Módulo Clima: página geral e configurações independentes por tipo.
            configMenu.AddPage(this.ModManifest, "climate",
                () => this.Helper.Translation.Get("config.page.climate.name"));
            configMenu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get("config.page.climate.description"));
            configMenu.AddPageLink(this.ModManifest, "climate-module",
                () => "> " + this.Helper.Translation.Get("config.climate.module.name"));
            configMenu.AddPageLink(this.ModManifest, "climate-rain",
                () => "> " + this.Helper.Translation.Get("config.climate.rain.name"));
            configMenu.AddPageLink(this.ModManifest, "climate-storm",
                () => "> " + this.Helper.Translation.Get("config.climate.storm.name"));
            configMenu.AddPageLink(this.ModManifest, "climate-snow",
                () => "> " + this.Helper.Translation.Get("config.climate.snow.name"));

            configMenu.AddPage(this.ModManifest, "climate-module",
                () => this.Helper.Translation.Get("config.climate.module.name"));
            configMenu.AddPageLink(this.ModManifest, "climate",
                () => "@ " + this.Helper.Translation.Get("config.page.back-to-climate.name"));
            configMenu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get("config.climate.module.description"));
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.climate.module.enabled.name"),
                tooltip: () => this.Helper.Translation.Get("config.climate.module.enabled.tooltip"),
                getValue: () => this.Config.Clima.ModuloAtivado,
                setValue: value => this.Config.Clima.ModuloAtivado = value,
                fieldId: "clima-modulo-ativado");

            this.RegistrarPaginaClima(configMenu, "climate-rain", "config.climate.rain",
                () => this.Config.Clima.ChuvaAtivada, v => this.Config.Clima.ChuvaAtivada = v,
                () => this.Config.Clima.ChuvaChance, v => this.Config.Clima.ChuvaChance = v,
                () => this.Config.Clima.ChuvaLimite, v => this.Config.Clima.ChuvaLimite = v,
                5, 75.0f, "clima-chuva");
            this.RegistrarPaginaClima(configMenu, "climate-storm", "config.climate.storm",
                () => this.Config.Clima.TempestadeAtivada, v => this.Config.Clima.TempestadeAtivada = v,
                () => this.Config.Clima.TempestadeChance, v => this.Config.Clima.TempestadeChance = v,
                () => this.Config.Clima.TempestadeLimite, v => this.Config.Clima.TempestadeLimite = v,
                2, 35.0f, "clima-tempestade");
            this.RegistrarPaginaClima(configMenu, "climate-snow", "config.climate.snow",
                () => this.Config.Clima.NeveAtivada, v => this.Config.Clima.NeveAtivada = v,
                () => this.Config.Clima.NeveChance, v => this.Config.Clima.NeveChance = v,
                () => this.Config.Clima.NeveLimite, v => this.Config.Clima.NeveLimite = v,
                5, 75.0f, "clima-neve");

            configMenu.AddPage(this.ModManifest, "notifications",
                () => this.Helper.Translation.Get("config.page.notifications.name"));
            configMenu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get("config.page.notifications.description"));
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.notifications.enabled.name"),
                tooltip: () => this.Helper.Translation.Get("config.notifications.enabled.tooltip"),
                getValue: () => this.Config.NotificacoesAtivadas,
                setValue: value => this.Config.NotificacoesAtivadas = value,
                fieldId: "notificacoes-ativadas");
        }

        /// <summary>
        /// Cria uma página de clima com ativação, chance adicional e limite por metade do mês.
        /// </summary>
        private void RegistrarPaginaClima(
            IGenericModConfigMenuApi menu,
            string pageId,
            string translationPrefix,
            Func<bool> getEnabled,
            Action<bool> setEnabled,
            Func<float> getChance,
            Action<float> setChance,
            Func<int> getLimit,
            Action<int> setLimit,
            int maxLimit,
            float maxChance,
            string fieldPrefix)
        {
            menu.AddPage(this.ModManifest, pageId,
                () => this.Helper.Translation.Get(translationPrefix + ".name"));
            menu.AddPageLink(this.ModManifest, "climate",
                () => "@ " + this.Helper.Translation.Get("config.page.back-to-climate.name"));
            menu.AddParagraph(this.ModManifest,
                () => this.Helper.Translation.Get(translationPrefix + ".description"));
            menu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.climate.type.enabled.name"),
                tooltip: () => this.Helper.Translation.Get("config.climate.type.enabled.tooltip"),
                getValue: getEnabled,
                setValue: setEnabled,
                fieldId: fieldPrefix + "-ativado");
            menu.AddNumberOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.climate.chance.name"),
                tooltip: () => this.Helper.Translation.Get("config.climate.chance.tooltip"),
                getValue: getChance,
                setValue: value => { setChance(value); this.Config.Validar(); },
                min: 1.0f, max: maxChance, interval: 1.0f,
                formatValue: value => $"{value:0}%",
                fieldId: fieldPrefix + "-chance");
            menu.AddNumberOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.climate.limit.name"),
                tooltip: () => this.Helper.Translation.Get("config.climate.limit.tooltip"),
                getValue: () => (float)getLimit(),
                setValue: value => { setLimit((int)Math.Round(value)); this.Config.Validar(); },
                min: 1.0f, max: maxLimit, interval: 1.0f,
                formatValue: value => $"{value:0}",
                fieldId: fieldPrefix + "-limite");
        }

        /// <summary>Cria uma página padronizada para um modo de recuperação de energia.</summary>
        private void RegistrarPaginaEnergia(
            IGenericModConfigMenuApi menu, string pageId, string translationPrefix,
            Func<bool> getEnabled, Action<bool> setEnabled,
            Func<float> getAmount, Action<float> setAmount,
            Func<float> getInterval, Action<float> setInterval,
            float maxInterval, float maxAmount, string fieldPrefix)
        {
            menu.AddPage(this.ModManifest, pageId, () => this.Helper.Translation.Get(translationPrefix + ".name"));
            menu.AddPageLink(this.ModManifest, "energy", () => "@ " + this.Helper.Translation.Get("config.page.back-to-energy.name"));
            menu.AddParagraph(this.ModManifest, () => this.Helper.Translation.Get(translationPrefix + ".description"));
            menu.AddBoolOption(this.ModManifest, getEnabled, setEnabled,
                () => this.Helper.Translation.Get("config.energy.mode.enabled.name"),
                () => this.Helper.Translation.Get("config.energy.mode.enabled.tooltip"), fieldPrefix + "-ativado");
            menu.AddNumberOption(this.ModManifest, getAmount, v => { setAmount(v); this.Config.Validar(); },
                () => this.Helper.Translation.Get("config.energy.amount.name"),
                () => this.Helper.Translation.Get("config.energy.amount.tooltip"),
                0.1f, maxAmount, 0.1f, v => $"{v:0.0}", fieldPrefix + "-quantidade");
            menu.AddNumberOption(this.ModManifest, getInterval, v => { setInterval(v); this.Config.Validar(); },
                () => this.Helper.Translation.Get("config.energy.interval.name"),
                () => this.Helper.Translation.Get("config.energy.interval.tooltip"),
                maxInterval * 0.1f, maxInterval, 0.1f, v => $"{v:0.0} s", fieldPrefix + "-intervalo");
        }

        /// <summary>Cria uma página padronizada para um modo de recuperação de saúde.</summary>
        private void RegistrarPaginaSaude(
            IGenericModConfigMenuApi menu, string pageId, string translationPrefix,
            Func<bool> getEnabled, Action<bool> setEnabled,
            Func<float> getAmount, Action<float> setAmount,
            Func<float> getInterval, Action<float> setInterval,
            float maxInterval, float maxAmount, string fieldPrefix)
        {
            menu.AddPage(this.ModManifest, pageId, () => this.Helper.Translation.Get(translationPrefix + ".name"));
            menu.AddPageLink(this.ModManifest, "health", () => "@ " + this.Helper.Translation.Get("config.page.back-to-health.name"));
            menu.AddParagraph(this.ModManifest, () => this.Helper.Translation.Get(translationPrefix + ".description"));
            menu.AddBoolOption(this.ModManifest, getEnabled, setEnabled,
                () => this.Helper.Translation.Get("config.health.mode.enabled.name"),
                () => this.Helper.Translation.Get("config.health.mode.enabled.tooltip"), fieldPrefix + "-ativado");
            menu.AddNumberOption(this.ModManifest, getAmount, v => { setAmount(v); this.Config.Validar(); },
                () => this.Helper.Translation.Get("config.health.amount.name"),
                () => this.Helper.Translation.Get("config.health.amount.tooltip"),
                0.1f, maxAmount, 0.1f, v => $"{v:0.0}", fieldPrefix + "-quantidade");
            menu.AddNumberOption(this.ModManifest, getInterval, v => { setInterval(v); this.Config.Validar(); },
                () => this.Helper.Translation.Get("config.health.interval.name"),
                () => this.Helper.Translation.Get("config.health.interval.tooltip"),
                maxInterval * 0.1f, maxInterval, 0.1f, v => $"{v:0.0} s", fieldPrefix + "-intervalo");
        }

        /// <summary>Restaura todos os valores padrão e reinicializa os estados temporários.</summary>
        private void ResetConfig()
        {
            this.Config = new ModConfig();
            this.Config.Validar();
            this.climateSystem = new ClimateSystem(this.Monitor, this.Config.Clima);
            this.usandoVelocidadeSentado = false;
            this.inicioCiclo24hUtc = null;
            this.ResetarEstadoEnergia();
            this.ResetarEstadoSaude();
        }

        /// <summary>Valida e grava no config.json as opções atuais do usuário.</summary>
        private void SaveConfig()
        {
            this.Config.Validar();
            this.Helper.WriteConfig(this.Config);
        }

        /// <summary>
        /// No começo de cada dia, reinicia o marco do ciclo 24h e os estados temporários
        /// de energia para que nenhum progresso/cooldown do dia anterior seja carregado.
        /// </summary>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            this.inicioCiclo24hUtc = this.Config.TempoModuloAtivado && this.Config.Ciclo24hEfetivamenteAtivado ? DateTime.UtcNow : null;
            this.usandoVelocidadeSentado = false;
            this.aceleracaoAssentosExternosBloqueadaAteProximoDia = false;
            this.ResetarEstadoEnergia();
            this.ResetarEstadoSaude();
            if (this.Config.ModAtivado)
                this.climateSystem?.ProcessarPrevisaoDeAmanha();
        }

        /// <summary>
        /// Atualização central do mod: aplica a velocidade do relógio conforme as opções
        /// e processa a recuperação de energia em cada tick válido do mundo.
        /// </summary>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            this.Config.Validar();

            // O interruptor geral suspende todos os sistemas sem apagar suas configurações.
            if (!this.Config.ModAtivado)
            {
                this.usandoVelocidadeSentado = false;
                this.inicioCiclo24hUtc = null;
                const int velocidadePadrao = 7000;
                if (Game1.realMilliSecondsPerGameTenMinutes != velocidadePadrao)
                    Game1.realMilliSecondsPerGameTenMinutes = velocidadePadrao;
                return;
            }

            if (!Context.IsWorldReady)
            {
                this.usandoVelocidadeSentado = false;
                this.ResetarEstadoEnergia();
                this.ResetarEstadoSaude();
                return;
            }

            if (!this.Config.TempoModuloAtivado)
            {
                this.usandoVelocidadeSentado = false;
                this.inicioCiclo24hUtc = null;
                const int velocidadePadrao = 7000;
                if (Game1.realMilliSecondsPerGameTenMinutes != velocidadePadrao)
                    Game1.realMilliSecondsPerGameTenMinutes = velocidadePadrao;

                this.ProcessarCooldownDano();
                this.ProcessarRecuperacaoEnergia();
                this.ProcessarRecuperacaoSaude();
                return;
            }

            // O ciclo de 24h tem prioridade sobre o tempo personalizado.
            // O balanceamento recalcula a velocidade restante para fechar o dia em 24h reais.
            bool ciclo24hAtivado = this.Config.Ciclo24hEfetivamenteAtivado;
            bool balanceamento24hAtivado = this.Config.Balanceamento24hEfetivamenteAtivado;
            if (!ciclo24hAtivado)
                this.inicioCiclo24hUtc = null;
            else if (this.inicioCiclo24hUtc is null)
                this.IniciarCicloPeloHorarioAtual();

            bool sentadoNaBiblioteca = this.JogadorSentadoPertoDeMesa();
            bool sentadoEmBancoDaCidade = this.JogadorSentadoEmBancoPermitidoDaCidade();
            bool sentadoNoSaloon = this.JogadorSentadoEmAssentoPermitidoDoSaloon();
            bool sentadoNoBancoDaPraia = this.JogadorSentadoNoBancoPermitidoDaPraia();

            // Ao alcançar 00:00, bloqueia a aceleração dos assentos externos até o próximo dia.
            // Assim, levantar e sentar novamente depois da meia-noite não reativa a aceleração.
            if (Game1.timeOfDay >= 2400)
                this.aceleracaoAssentosExternosBloqueadaAteProximoDia = true;

            // A Biblioteca encerra às 18h; chafariz, Saloon e o banco da praia encerram às 00:00.
            bool bibliotecaElegivel = sentadoNaBiblioteca && this.Config.AceleracaoBibliotecaAtivada;
            bool chafarizElegivel = sentadoEmBancoDaCidade && this.Config.AceleracaoChafarizAtivada;
            bool saloonElegivel = sentadoNoSaloon && this.Config.AceleracaoSaloonAtivada;
            bool praiaElegivel = sentadoNoBancoDaPraia && this.Config.AceleracaoPraiaAtivada;

            bool dentroDoHorarioPermitido = bibliotecaElegivel
                ? Game1.timeOfDay < 1800
                : (chafarizElegivel || saloonElegivel || praiaElegivel)
                    && Game1.timeOfDay < 2400
                    && !this.aceleracaoAssentosExternosBloqueadaAteProximoDia;

            bool deveAcelerar = this.Config.AceleracaoMesaAtivada
                && dentroDoHorarioPermitido;

            if (deveAcelerar != this.usandoVelocidadeSentado)
            {
                this.usandoVelocidadeSentado = deveAcelerar;
                this.ExibirNotificacao(deveAcelerar
                    ? "notification.seated-time.started"
                    : "notification.seated-time.ended");
            }

            float segundos;
            if (ciclo24hAtivado)
            {
                if (this.usandoVelocidadeSentado)
                    segundos = this.Config.SegundosPorDezMinutosSentado;
                else if (balanceamento24hAtivado)
                    segundos = this.CalcularVelocidadeBalanceada24h();
                else
                    segundos = SegundosPorDezMinutosCiclo24h;
            }
            else
            {
                segundos = this.usandoVelocidadeSentado
                    ? this.Config.SegundosPorDezMinutosSentado
                    : this.Config.TempoPersonalizadoAtivado ? this.Config.SegundosPorDezMinutos : 7.0f;
            }

            int milliseconds = Math.Max(1, (int)Math.Round(segundos * 1000.0f));
            if (Game1.realMilliSecondsPerGameTenMinutes != milliseconds)
                Game1.realMilliSecondsPerGameTenMinutes = milliseconds;

            this.ProcessarCooldownDano();
            this.ProcessarRecuperacaoEnergia();
            this.ProcessarRecuperacaoSaude();
        }


        /// <summary>
        /// Ao voltar ao título, limpa os estados temporários e restaura a velocidade apropriada
        /// para não deixar o relógio do jogo preso à configuração da sessão anterior.
        /// </summary>
        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            this.usandoVelocidadeSentado = false;
            this.aceleracaoAssentosExternosBloqueadaAteProximoDia = false;
            this.inicioCiclo24hUtc = null;
            this.ResetarEstadoEnergia();
            this.ResetarEstadoSaude();
            int milliseconds = (int)Math.Round((this.Config.ModAtivado && this.Config.TempoModuloAtivado && this.Config.TempoPersonalizadoAtivado ? this.Config.SegundosPorDezMinutos : 7.0f) * 1000.0f);
            if (Game1.realMilliSecondsPerGameTenMinutes != milliseconds)
                Game1.realMilliSecondsPerGameTenMinutes = milliseconds;
        }
    }
}
