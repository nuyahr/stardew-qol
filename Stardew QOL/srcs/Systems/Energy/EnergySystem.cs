using System;
using System.Collections.Generic;
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
        private readonly Dictionary<string, double> acumuladoresRecuperacaoEnergia = new();

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
                return;
            }

            if (!this.estadoEnergiaInicializado)
            {
                this.ultimaStamina = jogador.Stamina;
                this.ultimaPosicaoJogador = jogador.Position;
                this.ultimaAtualizacaoEnergiaUtc = agora;
                this.estadoEnergiaInicializado = true;
            }

            // Menus, diálogos e eventos pausam a recuperação e congelam o cooldown.
            // Não contar tempo real enquanto uma interface ou evento bloqueia a jogabilidade.
            bool interfaceAberta = Game1.activeClickableMenu is not null
                || Game1.dialogueUp
                || Game1.eventUp;

            if (interfaceAberta)
            {
                this.inicioPausaEnergiaUtc ??= agora;
                this.ultimaStamina = jogador.Stamina;
                this.ultimaPosicaoJogador = jogador.Position;
                this.ultimaAtualizacaoEnergiaUtc = agora;
                return;
            }

            // Ao fechar a interface, desloca o fim do cooldown pelo tempo pausado,
            // preservando exatamente o tempo restante que havia antes do menu.
            if (this.inicioPausaEnergiaUtc.HasValue)
            {
                TimeSpan duracaoPausa = agora - this.inicioPausaEnergiaUtc.Value;
                if (this.fimCooldownEnergiaUtc.HasValue)
                    this.fimCooldownEnergiaUtc = this.fimCooldownEnergiaUtc.Value.Add(duracaoPausa);

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
            float energiaGasta = this.ultimaStamina - jogador.Stamina;
            if (energiaGasta > 0.01f && this.Config.CooldownEnergiaAtivado)
                this.fimCooldownEnergiaUtc = agora.AddSeconds(this.Config.CooldownEnergiaSegundos);

            float dx = jogador.Position.X - this.ultimaPosicaoJogador.X;
            float dy = jogador.Position.Y - this.ultimaPosicaoJogador.Y;
            bool andando = (dx * dx) + (dy * dy) > 0.01f;
            this.ultimaPosicaoJogador = jogador.Position;

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

            if (!emCooldown && !emMinigamePesca)
            {
                if (this.Config.RecuperacaoSentadoAtivada && this.JogadorSentadoPertoDeMesa())
                {
                    // A Biblioteca é exclusiva e tem contador próprio, separado de outras cadeiras.
                    modo = "biblioteca";
                    quantidade = this.Config.EnergiaSentadoQuantidade;
                    intervalo = this.Config.EnergiaSentadoIntervalo;
                }
                else if (this.Config.RecuperacaoSentadoAtivada && jogador.IsSitting())
                {
                    // Qualquer cadeira, banco ou assento usa a configuração de sentado.
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
            this.acumuladoresRecuperacaoEnergia.Clear();
            this.ultimaStamina = 0;
            this.ultimaPosicaoJogador = Vector2.Zero;
        }
    }
}
