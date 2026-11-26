using System;
using System.Collections.Generic;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace StardewQOL
{
    /// <summary>Implementação e estado do módulo de recuperação de Energia.</summary>
    public partial class ModEntry
    {
        private DateTime? ultimaAtualizacaoEnergiaUtc;
        private DateTime? fimCooldownEnergiaUtc;
        private float ultimaStamina;
        private Vector2 ultimaPosicaoJogador;
        private bool estadoEnergiaInicializado;
        private DateTime? inicioPausaEnergiaUtc;
        private DateTime? fimAtrasoRegeneracaoCorridaUtc;
        private double acumuladoEnergiaCorrida;
        private float energiaGastaPelaCorridaDesdeUltimaAtualizacao;
        private bool estavaCorrendo;
        private readonly Dictionary<string, double> acumuladoresRecuperacaoEnergia = new();

        /// <summary>
        /// Inicia o cooldown único de energia quando o jogo confirma o início de um ataque
        /// corpo a corpo ou o disparo efetivo de um estilingue.
        /// </summary>
        private void RegistrarCooldownUsoDeArma(Farmer jogador)
        {
            if (!Context.IsWorldReady
                || !this.Config.ModAtivado
                || !this.Config.EnergiaModuloAtivado
                || !this.Config.CooldownEnergiaAtivado
                || jogador != Game1.player
                || Game1.activeClickableMenu is not null
                || Game1.dialogueUp
                || Game1.eventUp)
            {
                return;
            }

            this.fimCooldownEnergiaUtc = DateTime.UtcNow.AddSeconds(this.Config.CooldownEnergiaSegundos);
        }

        /// <summary>
        /// Prefixo Harmony chamado quando o jogo inicia uma animação real de ataque corpo a corpo.
        /// Inclui espadas, adagas, clavas e foices.
        /// </summary>
        private static void AoIniciarAtaqueCorpoACorpo(MeleeWeapon __instance, Farmer who)
        {
            ModEntry? mod = instanciaAtiva;
            if (mod is not null && who.CurrentTool == __instance)
                mod.RegistrarCooldownUsoDeArma(who);
        }

        /// <summary>
        /// Prefixo Harmony guarda a quantidade de projéteis antes da tentativa de disparo.
        /// </summary>
        private static void AntesDeDispararEstilingue(GameLocation location, out int __state)
        {
            __state = location.projectiles.Count;
        }

        /// <summary>
        /// Só inicia o cooldown se o disparo realmente adicionou um projétil ao mundo.
        /// Puxar, mirar ou soltar sem munição não ativa o cooldown.
        /// </summary>
        private static void DepoisDeDispararEstilingue(GameLocation location, Farmer who, int __state)
        {
            if (location.projectiles.Count > __state)
                instanciaAtiva?.RegistrarCooldownUsoDeArma(who);
        }

        /// <summary>
        /// Escolhe o modo de recuperação aplicável e atualiza a energia do jogador.
        /// Menus, diálogos e eventos pausam a contagem; cooldown e minigame de pesca
        /// também suspendem a recuperação sem descartar o progresso acumulado.
        /// Cada atividade mantém seu próprio contador para que a troca de modo não zere o tempo.
        /// </summary>
        private void ProcessarRecuperacaoEnergia()
        {
            Farmer jogador = Game1.player;
            DateTime agora = DateTime.UtcNow;

            if (!this.Config.EnergiaModuloAtivado)
            {
                this.ultimaStamina = jogador.Stamina;
                this.ultimaPosicaoJogador = jogador.Position;
                this.ultimaAtualizacaoEnergiaUtc = agora;
                this.estadoEnergiaInicializado = true;
                this.acumuladoresRecuperacaoEnergia.Clear();
                this.fimCooldownEnergiaUtc = null;
                this.inicioPausaEnergiaUtc = null;
                this.fimAtrasoRegeneracaoCorridaUtc = null;
                this.acumuladoEnergiaCorrida = 0.0;
                this.energiaGastaPelaCorridaDesdeUltimaAtualizacao = 0.0f;
                this.estavaCorrendo = false;
                return;
            }

            if (!this.estadoEnergiaInicializado)
            {
                this.ultimaStamina = jogador.Stamina;
                this.ultimaPosicaoJogador = jogador.Position;
                this.ultimaAtualizacaoEnergiaUtc = agora;
                this.estadoEnergiaInicializado = true;
            }

            // Menus, diálogos, eventos e a piscina do Spa pausam a recuperação personalizada.
            // No Spa, a recuperação original do jogo continua ativa; o mod apenas evita somar
            // sua própria recuperação de energia. O cooldown e os acumuladores ficam pausados.
            bool interfaceAberta = Game1.activeClickableMenu is not null
                || Game1.dialogueUp
                || Game1.eventUp;
            bool jogadorNasAguasTermais = jogador.currentLocation?.NameOrUniqueName == "BathHouse_Pool";

            if (interfaceAberta || jogadorNasAguasTermais)
            {
                this.inicioPausaEnergiaUtc ??= agora;
                this.ultimaStamina = jogador.Stamina;
                this.ultimaPosicaoJogador = jogador.Position;
                this.ultimaAtualizacaoEnergiaUtc = agora;
                return;
            }

            // Ao sair da interface ou das águas termais, desloca o fim do cooldown
            // pelo tempo pausado, preservando exatamente o tempo restante anterior.
            if (this.inicioPausaEnergiaUtc.HasValue)
            {
                TimeSpan duracaoPausa = agora - this.inicioPausaEnergiaUtc.Value;
                if (this.fimCooldownEnergiaUtc.HasValue)
                    this.fimCooldownEnergiaUtc = this.fimCooldownEnergiaUtc.Value.Add(duracaoPausa);
                if (this.fimAtrasoRegeneracaoCorridaUtc.HasValue)
                    this.fimAtrasoRegeneracaoCorridaUtc = this.fimAtrasoRegeneracaoCorridaUtc.Value.Add(duracaoPausa);

                this.inicioPausaEnergiaUtc = null;
                this.ultimaStamina = jogador.Stamina;
                this.ultimaPosicaoJogador = jogador.Position;
                this.ultimaAtualizacaoEnergiaUtc = agora;
                return;
            }

            double decorrido = this.ultimaAtualizacaoEnergiaUtc is null
                ? 0.0
                : Math.Clamp((agora - this.ultimaAtualizacaoEnergiaUtc.Value).TotalSeconds, 0.0, 1.0);
            this.ultimaAtualizacaoEnergiaUtc = agora;

            if (!this.Config.CooldownEnergiaAtivado)
                this.fimCooldownEnergiaUtc = null;

            bool segurandoVara = jogador.CurrentTool is FishingRod;
            float energiaGasta = this.ultimaStamina - jogador.Stamina - this.energiaGastaPelaCorridaDesdeUltimaAtualizacao;
            this.energiaGastaPelaCorridaDesdeUltimaAtualizacao = 0.0f;
            if (energiaGasta > 0.01f && this.Config.CooldownEnergiaAtivado)
                this.fimCooldownEnergiaUtc = agora.AddSeconds(this.Config.CooldownEnergiaSegundos);

            float dx = jogador.Position.X - this.ultimaPosicaoJogador.X;
            float dy = jogador.Position.Y - this.ultimaPosicaoJogador.Y;
            bool andando = (dx * dx) + (dy * dy) > 0.01f;
            this.ultimaPosicaoJogador = jogador.Position;

            // O próprio jogo diferencia corrida de caminhada pelo estado running.
            // Exige movimento real e exclui montaria para não cobrar energia a cavalo.
            bool correndo = this.Config.CustoEnergiaCorridaAtivado
                && jogador.running
                && jogador.movedDuringLastTick()
                && !jogador.UsingTool
                && !jogador.isRidingHorse();

            if (correndo)
            {
                this.acumuladoEnergiaCorrida += decorrido;
                double intervaloCorrida = Math.Max(0.1, this.Config.EnergiaCorridaIntervalo);
                int cobrancas = (int)(this.acumuladoEnergiaCorrida / intervaloCorrida);
                if (cobrancas > 0)
                {
                    this.acumuladoEnergiaCorrida -= cobrancas * intervaloCorrida;
                    float energiaMinima = this.Config.EnergiaCorridaMinima;
                    if (jogador.Stamina > energiaMinima)
                    {
                        float staminaAntesDaCorrida = jogador.Stamina;
                        jogador.Stamina = Math.Max(energiaMinima,
                            jogador.Stamina - (this.Config.EnergiaCorridaQuantidade * cobrancas));
                        this.energiaGastaPelaCorridaDesdeUltimaAtualizacao += staminaAntesDaCorrida - jogador.Stamina;
                    }
                }
                this.fimAtrasoRegeneracaoCorridaUtc = null;
            }
            else
            {
                this.acumuladoEnergiaCorrida = 0.0;
                if (this.estavaCorrendo)
                    this.fimAtrasoRegeneracaoCorridaUtc = agora.AddSeconds(this.Config.EnergiaCorridaAtrasoRegeneracao);
            }
            this.estavaCorrendo = correndo;

            bool emAtrasoAposCorrida = this.fimAtrasoRegeneracaoCorridaUtc.HasValue
                && agora < this.fimAtrasoRegeneracaoCorridaUtc.Value;
            if (this.fimAtrasoRegeneracaoCorridaUtc.HasValue && !emAtrasoAposCorrida)
                this.fimAtrasoRegeneracaoCorridaUtc = null;

            bool emCooldownEnergia = this.Config.CooldownEnergiaAtivado
                && this.fimCooldownEnergiaUtc.HasValue
                && agora < this.fimCooldownEnergiaUtc.Value;
            bool emCooldownDano = this.Config.SaudeModuloAtivado
                && this.Config.CooldownDanoAtivado
                && this.fimCooldownDanoUtc.HasValue
                && agora < this.fimCooldownDanoUtc.Value;
            bool emCooldown = emCooldownEnergia || emCooldownDano;
            bool emMinigamePesca = Game1.activeClickableMenu is BobberBar;

            // A ordem abaixo define a prioridade: biblioteca, outro assento, pesca,
            // parado, andando e, por último, recuperação geral como alternativa.
            string? modo = null;
            float quantidade = 0.0f;
            float intervalo = 1.0f;

            if (!emCooldown && !emMinigamePesca && !correndo && !emAtrasoAposCorrida)
            {
                if (this.Config.RecuperacaoSentadoAtivada && this.JogadorSentadoPertoDeMesa())
                {
                    // A Biblioteca é exclusiva e tem contador próprio, separado de outras cadeiras.
                    modo = "biblioteca";
                    quantidade = this.Config.EnergiaSentadoQuantidade;
                    intervalo = this.Config.EnergiaSentadoIntervalo;
                }
                else if (this.Config.RecuperacaoSentadoAtivada && (jogador.IsSitting() || jogador.isRidingHorse()))
                {
                    // Cadeiras, bancos e montaria usam a mesma configuração de sentado.
                    modo = "sentado";
                    quantidade = this.Config.EnergiaSentadoQuantidade;
                    intervalo = this.Config.EnergiaSentadoIntervalo;
                }
                else if (this.Config.RecuperacaoPescaAtivada && segurandoVara)
                {
                    modo = "pesca";
                    quantidade = this.Config.EnergiaPescaQuantidade;
                    intervalo = this.Config.EnergiaPescaIntervalo;
                }
                else if (!andando && this.Config.RecuperacaoParadoAtivada)
                {
                    modo = "parado";
                    quantidade = this.Config.EnergiaParadoQuantidade;
                    intervalo = this.Config.EnergiaParadoIntervalo;
                }
                else if (andando && this.Config.RecuperacaoAndandoAtivada)
                {
                    modo = "andando";
                    quantidade = this.Config.EnergiaAndandoQuantidade;
                    intervalo = this.Config.EnergiaAndandoIntervalo;
                }
                else if (this.Config.RecuperacaoGeralAtivada)
                {
                    modo = "geral";
                    quantidade = this.Config.EnergiaGeralQuantidade;
                    intervalo = this.Config.EnergiaGeralIntervalo;
                }
            }

            // A troca de atividade pausa o contador anterior; ao voltar, ele retoma do ponto salvo.
            // Cooldown e minigame também pausam todos os contadores, sem apagar o progresso.
            if (modo is not null)
            {
                // Bônus de 20% somente nos assentos que realmente aceleram o relógio.
                if (this.usandoVelocidadeSentado && (modo == "sentado" || modo == "biblioteca"))
                    quantidade *= 1.2f;

                this.acumuladoresRecuperacaoEnergia.TryGetValue(modo, out double acumulado);
                acumulado += decorrido;

                // O intervalo é contado em segundos reais, sem depender da velocidade do relógio do jogo.
                double intervaloEfetivo = Math.Max(0.01, intervalo);
                int recuperacoes = (int)(acumulado / intervaloEfetivo);
                if (recuperacoes > 0)
                {
                    acumulado -= recuperacoes * intervaloEfetivo;
                    jogador.Stamina = Math.Min(jogador.MaxStamina, jogador.Stamina + (quantidade * recuperacoes));
                }

                this.acumuladoresRecuperacaoEnergia[modo] = acumulado;
            }

            this.ultimaStamina = jogador.Stamina;
        }

        /// <summary>
        /// Limpa acumuladores, cooldown e referências temporais da energia.
        /// Usado ao trocar de dia, sair do mundo ou reinicializar o estado do jogador.
        /// </summary>
        private void ResetarEstadoEnergia()
        {
            this.ultimaAtualizacaoEnergiaUtc = null;
            this.fimCooldownEnergiaUtc = null;
            this.estadoEnergiaInicializado = false;
            this.inicioPausaEnergiaUtc = null;
            this.fimAtrasoRegeneracaoCorridaUtc = null;
            this.acumuladoEnergiaCorrida = 0.0;
            this.energiaGastaPelaCorridaDesdeUltimaAtualizacao = 0.0f;
            this.estavaCorrendo = false;
            this.acumuladoresRecuperacaoEnergia.Clear();
            this.ultimaStamina = 0;
            this.ultimaPosicaoJogador = Vector2.Zero;
        }
    }
}
