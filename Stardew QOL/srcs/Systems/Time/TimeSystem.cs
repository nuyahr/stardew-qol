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
        private bool usandoVelocidadeSentado;
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
            return (float)Math.Clamp(segundosPorIntervalo, 0.1, 86400.0);
        }

        /// <summary>Converte o formato de horário do jogo (HHMM) em minutos.</summary>
        private static int ConverterHorarioEmMinutos(int horario)
        {
            int horas = horario / 100;
            int minutos = horario % 100;
            return (horas * 60) + minutos;
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
    }
}
