using System;
using StardewModdingAPI;
using StardewValley;

namespace StardewQOL
{
    /// <summary>
    /// Acrescenta ocorrências climáticas sem substituir uma previsão natural já escolhida.
    /// Executa uma única vez por dia de jogo, depois que o jogo gera a previsão de amanhã. A velocidade do relógio e o ciclo de 24h não provocam novos sorteios: o clima continua vinculado ao calendário do jogo.
    /// </summary>
    internal sealed class ClimateSystem
    {
        private const string PrefixoDados = "Nuya.StardewQOL/Climate/";
        private const string ChavePeriodo = PrefixoDados + "ClimatePeriod";
        private const string ChaveChuva = PrefixoDados + "ExtraRainCount";
        private const string ChaveTempestade = PrefixoDados + "ExtraStormCount";
        private const string ChaveNeve = PrefixoDados + "ExtraSnowCount";

        private readonly IMonitor monitor;
        private readonly ClimateConfig config;

        public ClimateSystem(IMonitor monitor, ClimateConfig config)
        {
            this.monitor = monitor;
            this.config = config;
        }

        public void ProcessarPrevisaoDeAmanha()
        {
            if (!Context.IsWorldReady || !Context.IsMainPlayer)
                return;

            if (!this.config.ModuloAtivado)
            {
                this.monitor.Log("[CLIMA-DIAG] Módulo desativado; nenhuma tentativa foi executada.", LogLevel.Trace);
                return;
            }

            this.config.Validar();
            this.GarantirPeriodoAtual();

            (int diaAmanha, string estacaoAmanha, int anoAmanha) = ObterDataDeAmanha();
            string previsaoOriginal = Game1.weatherForTomorrow;
            int chuvaAntes = LerContador(ChaveChuva);
            int tempestadeAntes = LerContador(ChaveTempestade);
            int neveAntes = LerContador(ChaveNeve);
            bool primaveraAnoUm = Game1.currentSeason == "spring" && Game1.year == 1;
            bool dataProtegida = AmanhaTemClimaForcadoOuFestival();

            this.monitor.Log(
                $"[CLIMA-DIAG] INÍCIO | Hoje={Game1.currentSeason} {Game1.dayOfMonth}, Ano {Game1.year} | " +
                $"Amanhã={estacaoAmanha} {diaAmanha}, Ano {anoAmanha} | Previsão original={previsaoOriginal} | " +
                $"PrimaveraAno1={primaveraAnoUm} | DataProtegida={dataProtegida} | " +
                $"Contadores antes: chuva={chuvaAntes}/{this.config.ChuvaLimite}, " +
                $"tempestade={tempestadeAntes}/{this.config.TempestadeLimite}, " +
                $"neve={neveAntes}/{this.config.NeveLimite}.",
                LogLevel.Trace);

            // Só acrescentamos clima quando o jogo escolheu um dia ensolarado.
            // Chuva, tempestade, neve, vento, chuva verde, festivais e casamentos naturais
            // permanecem exatamente como o jogo os gerou.
            if (!string.Equals(previsaoOriginal, "Sun", StringComparison.OrdinalIgnoreCase))
            {
                this.monitor.Log(
                    $"[CLIMA-DIAG] SEM TENTATIVA | Motivo=previsão original diferente de Sun ({previsaoOriginal}); " +
                    "o clima natural foi preservado.", LogLevel.Trace);
                return;
            }

            if (dataProtegida)
            {
                this.monitor.Log(
                    "[CLIMA-DIAG] SEM TENTATIVA | Motivo=data protegida por clima forçado/festival.",
                    LogLevel.Trace);
                return;
            }

            string estacao = Game1.currentSeason;

            // Tempestade tem prioridade sobre chuva normal, pois são resultados distintos.
            if (this.config.TempestadeAtivada && primaveraAnoUm)
            {
                this.monitor.Log(
                    "[CLIMA-DIAG] TEMPESTADE BLOQUEADA | Motivo=Primavera do Ano 1.",
                    LogLevel.Trace);
            }
            else if (this.config.TempestadeAtivada && estacao != "winter")
            {
                if (this.TentarAdicionar("Storm", this.config.TempestadeChance,
                    this.config.TempestadeLimite, ChaveTempestade))
                {
                    this.LogResultadoFinal(previsaoOriginal, "Storm");
                    return;
                }
            }
            else if (!this.config.TempestadeAtivada)
            {
                this.monitor.Log("[CLIMA-DIAG] Tempestade ignorada | Motivo=opção desativada.", LogLevel.Trace);
            }
            else if (estacao == "winter")
            {
                this.monitor.Log("[CLIMA-DIAG] Tempestade bloqueada | Motivo=estação de inverno.", LogLevel.Trace);
            }

            if (this.config.ChuvaAtivada && estacao != "winter")
            {
                if (this.TentarAdicionar("Rain", this.config.ChuvaChance,
                    this.config.ChuvaLimite, ChaveChuva))
                {
                    this.LogResultadoFinal(previsaoOriginal, "Rain");
                    return;
                }
            }
            else if (!this.config.ChuvaAtivada)
            {
                this.monitor.Log("[CLIMA-DIAG] Chuva ignorada | Motivo=opção desativada.", LogLevel.Trace);
            }
            else if (estacao == "winter")
            {
                this.monitor.Log("[CLIMA-DIAG] Chuva bloqueada | Motivo=estação de inverno.", LogLevel.Trace);
            }

            if (this.config.NeveAtivada && estacao == "winter")
            {
                if (this.TentarAdicionar("Snow", this.config.NeveChance,
                    this.config.NeveLimite, ChaveNeve))
                {
                    this.LogResultadoFinal(previsaoOriginal, "Snow");
                    return;
                }
            }
            else if (!this.config.NeveAtivada)
            {
                this.monitor.Log("[CLIMA-DIAG] Neve ignorada | Motivo=opção desativada.", LogLevel.Trace);
            }
            else if (estacao != "winter")
            {
                this.monitor.Log("[CLIMA-DIAG] Neve bloqueada | Motivo=fora do inverno.", LogLevel.Trace);
            }

            this.LogResultadoFinal(previsaoOriginal, "nenhum clima adicional");
        }

        private void LogResultadoFinal(string previsaoOriginal, string resultado)
        {
            this.monitor.Log(
                $"[CLIMA-DIAG] FIM | Previsão original={previsaoOriginal} | Resultado={resultado} | " +
                $"Previsão atualizada={Game1.weatherForTomorrow} | " +
                $"Contadores finais: chuva={LerContador(ChaveChuva)}, " +
                $"tempestade={LerContador(ChaveTempestade)}, neve={LerContador(ChaveNeve)}.",
                LogLevel.Trace);
        }

        private bool TentarAdicionar(string clima, float chancePercentual, int limite, string chaveContador)
        {
            int contador = LerContador(chaveContador);
            if (limite <= 0 || chancePercentual <= 0.0f)
            {
                this.monitor.Log(
                    $"[CLIMA-DIAG] TENTATIVA {clima} | Resultado=ignorada | " +
                    $"Motivo=chance ou limite zerado | Chance={chancePercentual:0.##}% | Contador={contador}/{limite}.",
                    LogLevel.Trace);
                return false;
            }

            if (contador >= limite)
            {
                this.monitor.Log(
                    $"[CLIMA-DIAG] TENTATIVA {clima} | Resultado=bloqueada | " +
                    $"Motivo=limite atingido | Chance={chancePercentual:0.##}% | Contador={contador}/{limite}.",
                    LogLevel.Trace);
                return false;
            }

            // RNG próprio atual, mantido intacto nesta versão de diagnóstico.
            // A instrumentação apenas registra a tentativa; não altera a semente nem o resultado.
            int seed = HashCode.Combine(
                Game1.uniqueIDForThisGame,
                Game1.year,
                Game1.currentSeason,
                Game1.dayOfMonth,
                clima,
                "StardewQOL");
            var random = new Random(seed);
            double sorteio = random.NextDouble();
            double limiteSorteio = chancePercentual / 100.0;
            bool sucesso = sorteio < limiteSorteio;

            this.monitor.Log(
                $"[CLIMA-DIAG] TENTATIVA {clima} | Chance={chancePercentual:0.##}% | " +
                $"Sorteio={sorteio:0.000000} | LimiteSorteio={limiteSorteio:0.000000} | " +
                $"Contador={contador}/{limite} | Resultado={(sucesso ? "APROVADA" : "NÃO APROVADA" )}.",
                LogLevel.Trace);

            if (!sucesso)
                return false;

            Game1.weatherForTomorrow = clima;
            Game1.player.modData[chaveContador] = (contador + 1).ToString(
                System.Globalization.CultureInfo.InvariantCulture);

            this.monitor.Log(
                $"Clima adicional programado para amanhã: {clima}. Contador desta metade do mês: {contador + 1}/{limite}.",
                LogLevel.Trace);

            return true;
        }

        private void GarantirPeriodoAtual()
        {
            (int dia, string estacao, int ano) = ObterDataDeAmanha();
            int metade = dia <= 14 ? 1 : 2;
            string periodo = $"{Game1.uniqueIDForThisGame}/{ano}/{estacao}/{metade}";

            if (Game1.player.modData.TryGetValue(ChavePeriodo, out string? salvo)
                && string.Equals(salvo, periodo, StringComparison.Ordinal))
                return;

            Game1.player.modData[ChavePeriodo] = periodo;
            Game1.player.modData[ChaveChuva] = "0";
            Game1.player.modData[ChaveTempestade] = "0";
            Game1.player.modData[ChaveNeve] = "0";
        }

        private static (int dia, string estacao, int ano) ObterDataDeAmanha()
        {
            int dia = Game1.dayOfMonth + 1;
            string estacao = Game1.currentSeason;
            int ano = Game1.year;

            if (dia > 28)
            {
                dia = 1;
                estacao = estacao switch
                {
                    "spring" => "summer",
                    "summer" => "fall",
                    "fall" => "winter",
                    _ => "spring"
                };

                if (estacao == "spring")
                    ano++;
            }

            return (dia, estacao, ano);
        }

        private static int LerContador(string chave)
        {
            if (Game1.player.modData.TryGetValue(chave, out string? valor)
                && int.TryParse(valor, out int contador))
                return Math.Max(0, contador);

            return 0;
        }

        private static bool AmanhaTemClimaForcadoOuFestival()
        {
            int dia = Game1.dayOfMonth + 1;
            string estacao = Game1.currentSeason;
            int ano = Game1.year;

            if (dia > 28)
            {
                dia = 1;
                estacao = estacao switch
                {
                    "spring" => "summer",
                    "summer" => "fall",
                    "fall" => "winter",
                    _ => "spring"
                };
                if (estacao == "spring")
                    ano++;
            }

            // Dias com clima definido pelo jogo, incluindo o início de cada estação.
            if ((estacao == "spring" && (dia == 1 || (ano == 1 && dia == 2) || (ano == 1 && dia == 3) || (ano == 1 && dia == 4)))
                || (estacao == "summer" && (dia == 1 || dia == 13 || dia == 26))
                || (estacao == "fall" && dia == 1)
                || (estacao == "winter" && dia == 1))
                return true;

            // Festivais tradicionais e festivais passivos adicionados/expandidos na versão 1.6.
            return estacao switch
            {
                "spring" => dia is 13 or 15 or 16 or 17 or 24,
                "summer" => dia is 11 or 20 or 21 or 28,
                "fall" => dia is 16 or 27,
                "winter" => dia is 8 or 12 or 13 or 15 or 16 or 17 or 25,
                _ => false
            };
        }
    }
}
