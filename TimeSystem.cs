using System;
using StardewValley;
using StardewValley.Objects;

namespace StardewQOL
{
    /// <summary>Implementação do relógio personalizado e do balanceamento de 24 horas.</summary>
    public partial class ModEntry
    {
        private const float SegundosPorDezMinutosCiclo24h = 720.0f;
        private const double DuracaoCiclo24hSegundos = 24.0 * 60.0 * 60.0;
        private const int MinutosDeJogoPorDia = 20 * 60;
        // Evita que a compensação torne o relógio excessivamente lento: no máximo 2x
        // o intervalo-base do ciclo (24h passa a ser uma meta com limite de segurança).
        private const double LimiteMaximoSegundosPorDezMinutos24h = 1440.0;
        private bool usandoVelocidadeSentado;
        private bool aceleracaoAssentosExternosBloqueadaAteProximoDia;
        private DateTime? inicioCiclo24hUtc;

        /// <summary>
        /// Reconstrói o início estimado do ciclo de 24h a partir do horário atual do jogo.
        /// Isso permite ativar o ciclo no meio do dia sem reiniciá-lo artificialmente às 6h.
        /// </summary>
        private void IniciarCicloPeloHorarioAtual()
        {
            int minutosDesdeInicio = Math.Clamp(ConverterHorarioEmMinutos(Game1.timeOfDay) - (6 * 60), 0, MinutosDeJogoPorDia);
            double proporcaoDoDia = minutosDesdeInicio / (double)MinutosDeJogoPorDia;
            double segundosDecorridosEstimados = proporcaoDoDia * DuracaoCiclo24hSegundos;
            this.inicioCiclo24hUtc = DateTime.UtcNow.AddSeconds(-segundosDecorridosEstimados);
        }

        /// <summary>
        /// Divide o tempo real restante até completar 24 horas pelos blocos de 10 minutos
        /// de jogo que ainda faltam no dia, compensando acelerações anteriores.
        /// </summary>
        private float CalcularVelocidadeBalanceada24h()
        {
            if (this.inicioCiclo24hUtc is null)
                this.IniciarCicloPeloHorarioAtual();

            int minutosDeJogoDecorridos = Math.Clamp(ConverterHorarioEmMinutos(Game1.timeOfDay) - (6 * 60), 0, MinutosDeJogoPorDia);
            int minutosDeJogoRestantes = MinutosDeJogoPorDia - minutosDeJogoDecorridos;
            int intervalosRestantes = (int)Math.Ceiling(minutosDeJogoRestantes / 10.0);
            if (intervalosRestantes <= 0)
                return SegundosPorDezMinutosCiclo24h;

            double segundosReaisDecorridos = (DateTime.UtcNow - this.inicioCiclo24hUtc!.Value).TotalSeconds;
            double segundosReaisRestantes = DuracaoCiclo24hSegundos - segundosReaisDecorridos;
            if (segundosReaisRestantes <= 0)
                return SegundosPorDezMinutosCiclo24h;

            double segundosPorIntervalo = segundosReaisRestantes / intervalosRestantes;
            return (float)Math.Clamp(segundosPorIntervalo, 0.1, LimiteMaximoSegundosPorDezMinutos24h);
        }

        /// <summary>Converte o formato de horário do jogo (HHMM) em minutos.</summary>
        private static int ConverterHorarioEmMinutos(int horario)
        {
            int horas = horario / 100;
            int minutos = horario % 100;
            return (horas * 60) + minutos;
        }


        /// <summary>
        /// Detecta exclusivamente o banco específico da praia confirmado pelo diagnóstico do SMAPI.
        /// O retângulo corresponde ao SeatBounds (41,34,3,1) no mapa Beach.
        /// </summary>
        private bool JogadorSentadoNoBancoPermitidoDaPraia()
        {
            Farmer jogador = Game1.player;
            var local = jogador.currentLocation;
            if (local is null || local.NameOrUniqueName != "Beach" || !jogador.IsSitting())
                return false;

            var assento = jogador.sittingFurniture;
            if (assento is null)
                return false;

            Microsoft.Xna.Framework.Rectangle limitesAssento = assento.GetSeatBounds();
            return limitesAssento.X == 41
                && limitesAssento.Y == 34
                && limitesAssento.Width == 3
                && limitesAssento.Height == 1;
        }

        /// <summary>
        /// Detecta somente os assentos originais permitidos na parte inferior da Biblioteca
        /// do Museu. Móveis colocados pelo jogador e a área da sala de aula são excluídos.
        /// Essa detecção é usada pela aceleração do relógio e pelo bônus de energia da biblioteca.
        /// </summary>
        private bool JogadorSentadoPertoDeMesa()
        {
            Farmer jogador = Game1.player;
            var local = jogador.currentLocation;
            if (local is null || local.NameOrUniqueName != "ArchaeologyHouse" || !jogador.IsSitting())
                return false;

            var assento = jogador.sittingFurniture;
            if (assento is null || assento is Furniture)
                return false;

            Microsoft.Xna.Framework.Rectangle limitesAssento = assento.GetSeatBounds();
            const int esquerda = 8, topo = 13, direita = 21, baseMapa = 20;
            return limitesAssento.X >= esquerda
                && limitesAssento.Right <= direita
                && limitesAssento.Y >= topo
                && limitesAssento.Bottom <= baseMapa;
        }

        /// <summary>
        /// Detecta exclusivamente os quatro assentos marcados pelo usuário no Saloon.
        /// As posições vêm do diagnóstico do SMAPI feito no save do usuário.
        /// </summary>
        private bool JogadorSentadoEmAssentoPermitidoDoSaloon()
        {
            Farmer jogador = Game1.player;
            var local = jogador.currentLocation;
            if (local is null || local.NameOrUniqueName != "Saloon" || !jogador.IsSitting())
                return false;

            var assento = jogador.sittingFurniture;
            if (assento is null)
                return false;

            // Coordenadas MapSeat confirmadas pelo diagnóstico do SMAPI:
            // (13,20,1,1), (20,18,1,1), sofá azul (42,17,1,1) e sofá vermelho (50,18,1,2).
            Microsoft.Xna.Framework.Rectangle limitesAssento = assento.GetSeatBounds();
            return (limitesAssento.X == 13 && limitesAssento.Y == 20 && limitesAssento.Width == 1 && limitesAssento.Height == 1)
                || (limitesAssento.X == 20 && limitesAssento.Y == 18 && limitesAssento.Width == 1 && limitesAssento.Height == 1)
                || (limitesAssento.X == 42 && limitesAssento.Y == 17 && limitesAssento.Width == 1 && limitesAssento.Height == 1)
                || (limitesAssento.X == 43 && limitesAssento.Y == 18 && limitesAssento.Width == 1 && limitesAssento.Height == 2)
                || (limitesAssento.X == 50 && limitesAssento.Y == 18 && limitesAssento.Width == 1 && limitesAssento.Height == 2);
        }

        /// <summary>
        /// Detecta exclusivamente os três bancos marcados pelo usuário ao redor do chafariz,
        /// na área gramada ao lado do Centro Comunitário em Pelican Town.
        /// </summary>
        private bool JogadorSentadoEmBancoPermitidoDaCidade()
        {
            Farmer jogador = Game1.player;
            var local = jogador.currentLocation;
            if (local is null || local.NameOrUniqueName != "Town" || !jogador.IsSitting())
                return false;

            var assento = jogador.sittingFurniture;
            if (assento is null)
                return false;

            // MapSeat.GetSeatBounds() já retorna coordenadas em tiles, não pixels.
            // Posições registradas no SMAPI ao sentar nos três bancos escolhidos:
            // esquerdo: (20,26,1,2); horizontal: (25,21), (26,21), (27,21);
            // direito: (33,26,1,2).
            Microsoft.Xna.Framework.Rectangle limitesAssento = assento.GetSeatBounds();

            bool bancoEsquerdo = limitesAssento.X == 20
                && limitesAssento.Y == 26
                && limitesAssento.Width == 1
                && limitesAssento.Height == 2;

            bool bancoHorizontal = limitesAssento.Y == 21
                && limitesAssento.X >= 25
                && limitesAssento.X <= 27
                && limitesAssento.Width == 1
                && limitesAssento.Height == 1;

            bool bancoDireito = limitesAssento.X == 33
                && limitesAssento.Y == 26
                && limitesAssento.Width == 1
                && limitesAssento.Height == 2;

            return bancoEsquerdo || bancoHorizontal || bancoDireito;
        }
    }
}
