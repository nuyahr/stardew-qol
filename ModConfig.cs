using System;

namespace StardewQOL
{
    /// <summary>Configurações gerais persistidas pelo SMAPI.</summary>
    public partial class ModConfig
    {
        /// <summary>Ativa ou desativa todos os sistemas do Stardew QOL.</summary>
        public bool ModAtivado { get; set; } = true;

        public ClimateConfig Clima { get; set; } = new();

        /// <summary>Valida todos os módulos antes de usar ou salvar as configurações.</summary>
        public void Validar()
        {
            this.ValidarTempo();
            this.ValidarEnergia();
            this.Clima ??= new ClimateConfig();
            this.Clima.Validar();
            this.ValidarSaude();
        }

        private static float ValidarQuantidade(float valor, float padrao, float maximo = 10.0f)
        {
            if (float.IsNaN(valor) || float.IsInfinity(valor))
                valor = padrao;
            return Math.Clamp(valor, 0.1f, maximo);
        }

        private static float ValidarIntervalo(float valor, float padrao, float maximo, bool permitirZero = false, float minimo = 0.1f)
        {
            if (float.IsNaN(valor) || float.IsInfinity(valor))
                valor = padrao;
            return Math.Clamp(valor, permitirZero ? 0.0f : minimo, maximo);
        }

        private static float ValidarVelocidade(float valor, float padrao, float minimo, float maximo)
        {
            if (float.IsNaN(valor) || float.IsInfinity(valor))
                valor = padrao;
            valor = Math.Clamp(valor, minimo, maximo);
            return MathF.Round(valor * 2.0f, MidpointRounding.AwayFromZero) / 2.0f;
        }
    }
}
