using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;

namespace StardewQOL
{
    /// <summary>
    /// Sistema de Saúde: recuperação de HP por atividade e cooldown compartilhado
    /// com Energia após a detecção de dano real.
    /// </summary>
    public partial class ModEntry
    {
        // Estado da Saúde e cooldown único disparado ao receber dano.
        private DateTime? ultimaAtualizacaoSaudeUtc;
        private DateTime? fimCooldownDanoUtc;
        private DateTime? inicioPausaDanoUtc;
        private int ultimaSaude;
        private int ultimaSaudeDetectada;
        private bool estadoSaudeInicializado;
        private bool estadoDeteccaoDanoInicializado;
        private Vector2 ultimaPosicaoSaude;
        private readonly Dictionary<string, double> acumuladoresRecuperacaoSaude = new();
        // A vida é inteira no jogo; estes resíduos preservam quantidades fracionárias entre ativações.
        private readonly Dictionary<string, double> residuosQuantidadeSaude = new();

        /// <summary>
        /// Detecta perda real de vida e inicia um único cooldown compartilhado entre Saúde e Energia.
        /// Menus, diálogos e eventos congelam o tempo restante desse cooldown.
        /// </summary>
        private void ProcessarCooldownDano()
        {
            Farmer jogador = Game1.player;
            DateTime agora = DateTime.UtcNow;

            if (!this.Config.SaudeModuloAtivado || !this.Config.CooldownDanoAtivado)
            {
                this.fimCooldownDanoUtc = null;
                this.inicioPausaDanoUtc = null;
                this.ultimaSaudeDetectada = jogador.health;
                
                this.estadoDeteccaoDanoInicializado = true;
                return;
            }

            if (!this.estadoDeteccaoDanoInicializado)
            {
                this.ultimaSaudeDetectada = jogador.health;
                
                this.estadoDeteccaoDanoInicializado = true;
            }

            bool interfaceAberta = Game1.activeClickableMenu is not null || Game1.dialogueUp || Game1.eventUp;
            if (interfaceAberta)
            {
                this.inicioPausaDanoUtc ??= agora;
                this.ultimaSaudeDetectada = jogador.health;
                
                return;
            }

            if (this.inicioPausaDanoUtc.HasValue)
            {
                TimeSpan pausa = agora - this.inicioPausaDanoUtc.Value;
                if (this.fimCooldownDanoUtc.HasValue)
                    this.fimCooldownDanoUtc = this.fimCooldownDanoUtc.Value.Add(pausa);
                this.inicioPausaDanoUtc = null;
                this.ultimaSaudeDetectada = jogador.health;
                
                return;
            }

            if (jogador.health < this.ultimaSaudeDetectada)
                this.fimCooldownDanoUtc = agora.AddSeconds(this.Config.CooldownDanoSegundos);

            this.ultimaSaudeDetectada = jogador.health;
            
        }

        /// <summary>
        /// Recupera vida usando os mesmos modos de atividade da Energia, com contadores independentes.
        /// A recuperação não é bloqueada pelo uso de espada; somente menus, eventos e cooldown de dano pausam.
        /// </summary>
        private void ProcessarRecuperacaoSaude()
        {
            Farmer jogador = Game1.player;
            DateTime agora = DateTime.UtcNow;

            if (!this.Config.SaudeModuloAtivado)
            {
                this.ultimaSaude = jogador.health;
                this.ultimaPosicaoSaude = jogador.Position;
                this.ultimaAtualizacaoSaudeUtc = agora;
                this.estadoSaudeInicializado = true;
                this.acumuladoresRecuperacaoSaude.Clear();
                this.residuosQuantidadeSaude.Clear();
                return;
            }

            if (!this.estadoSaudeInicializado)
            {
                this.ultimaSaude = jogador.health;
                this.ultimaPosicaoSaude = jogador.Position;
                this.ultimaAtualizacaoSaudeUtc = agora;
                this.estadoSaudeInicializado = true;
            }

            bool interfaceAberta = Game1.activeClickableMenu is not null || Game1.dialogueUp || Game1.eventUp;
            if (interfaceAberta)
            {
                this.ultimaSaude = jogador.health;
                this.ultimaPosicaoSaude = jogador.Position;
                this.ultimaAtualizacaoSaudeUtc = agora;
                return;
            }

            double decorrido = this.ultimaAtualizacaoSaudeUtc is null
                ? 0.0
                : Math.Clamp((agora - this.ultimaAtualizacaoSaudeUtc.Value).TotalSeconds, 0.0, 1.0);
            this.ultimaAtualizacaoSaudeUtc = agora;

            bool emCooldownDano = this.Config.CooldownDanoAtivado
                && this.fimCooldownDanoUtc.HasValue
                && agora < this.fimCooldownDanoUtc.Value;
            bool emMinigamePesca = Game1.activeClickableMenu is BobberBar;
            float dx = jogador.Position.X - this.ultimaPosicaoSaude.X;
            float dy = jogador.Position.Y - this.ultimaPosicaoSaude.Y;
            bool andando = (dx * dx) + (dy * dy) > 0.01f;
            this.ultimaPosicaoSaude = jogador.Position;

            if (jogador.health >= jogador.maxHealth)
            {
                this.residuosQuantidadeSaude.Clear();
                this.ultimaSaude = jogador.health;
                return;
            }

            string? modo = null;
            float quantidade = 0.0f;
            float intervalo = 1.0f;

            if (!emCooldownDano && !emMinigamePesca)
            {
                if (this.Config.RecuperacaoSaudeSentadoAtivada && this.JogadorSentadoPertoDeMesa())
                {
                    modo = "biblioteca";
                    quantidade = this.Config.SaudeSentadoQuantidade; intervalo = this.Config.SaudeSentadoIntervalo;
                }
                else if (this.Config.RecuperacaoSaudeSentadoAtivada && jogador.IsSitting())
                {
                    modo = "sentado"; quantidade = this.Config.SaudeSentadoQuantidade; intervalo = this.Config.SaudeSentadoIntervalo;
                }
                else if (!andando && this.Config.RecuperacaoSaudeParadoAtivada)
                {
                    modo = "parado"; quantidade = this.Config.SaudeParadoQuantidade; intervalo = this.Config.SaudeParadoIntervalo;
                }
                else if (andando && this.Config.RecuperacaoSaudeAndandoAtivada)
                {
                    modo = "andando"; quantidade = this.Config.SaudeAndandoQuantidade; intervalo = this.Config.SaudeAndandoIntervalo;
                }
                else if (this.Config.RecuperacaoSaudeGeralAtivada)
                {
                    modo = "geral"; quantidade = this.Config.SaudeGeralQuantidade; intervalo = this.Config.SaudeGeralIntervalo;
                }
            }

            if (modo is not null)
            {
                this.acumuladoresRecuperacaoSaude.TryGetValue(modo, out double acumulado);
                acumulado += decorrido;
                // O intervalo é contado em segundos reais, sem depender da velocidade do relógio do jogo.
                double intervaloEfetivo = Math.Max(0.01, intervalo);
                int recuperacoes = (int)(acumulado / intervaloEfetivo);
                if (recuperacoes > 0)
                {
                    acumulado -= recuperacoes * intervaloEfetivo;
                    this.residuosQuantidadeSaude.TryGetValue(modo, out double residuo);
                    double curaAcumulada = residuo + (quantidade * recuperacoes);
                    int curaInteira = (int)Math.Floor(curaAcumulada);
                    this.residuosQuantidadeSaude[modo] = curaAcumulada - curaInteira;
                    if (curaInteira > 0)
                        jogador.health = Math.Min(jogador.maxHealth, jogador.health + curaInteira);
                }
                this.acumuladoresRecuperacaoSaude[modo] = acumulado;
            }

            this.ultimaSaude = jogador.health;
        }

        /// <summary>Limpa os estados temporários do módulo Saúde e do cooldown compartilhado por dano.</summary>
        private void ResetarEstadoSaude()
        {
            this.ultimaAtualizacaoSaudeUtc = null;
            this.fimCooldownDanoUtc = null;
            this.inicioPausaDanoUtc = null;
            this.estadoSaudeInicializado = false;
            this.estadoDeteccaoDanoInicializado = false;
            this.ultimaSaude = 0;
            this.ultimaSaudeDetectada = 0;
            this.ultimaPosicaoSaude = Vector2.Zero;
            this.acumuladoresRecuperacaoSaude.Clear();
            this.residuosQuantidadeSaude.Clear();
        }

    }
}
