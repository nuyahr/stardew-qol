using System;

namespace StardewQOL
{
    /// <summary>Configurações do módulo Clima e dos três tipos de clima adicionais.</summary>
    public class ClimateConfig
    {
        public bool ModuloAtivado { get; set; } = true;

        public bool ChuvaAtivada { get; set; } = true;
        public float ChuvaChance { get; set; } = 10.0f;
        public int ChuvaLimite { get; set; } = 3;

        public bool TempestadeAtivada { get; set; } = true;
        public float TempestadeChance { get; set; } = 5.0f;
        public int TempestadeLimite { get; set; } = 2;

        public bool NeveAtivada { get; set; } = true;
        public float NeveChance { get; set; } = 10.0f;
        public int NeveLimite { get; set; } = 3;

        public void Validar()
        {
            this.ChuvaChance = ValidarChance(this.ChuvaChance, 10.0f, 75.0f);
            this.TempestadeChance = ValidarChance(this.TempestadeChance, 5.0f, 35.0f);
            this.NeveChance = ValidarChance(this.NeveChance, 10.0f, 75.0f);
            this.ChuvaLimite = Math.Clamp(this.ChuvaLimite, 1, 5);
            this.TempestadeLimite = Math.Clamp(this.TempestadeLimite, 1, 2);
            this.NeveLimite = Math.Clamp(this.NeveLimite, 1, 5);
        }

        private static float ValidarChance(float valor, float padrao, float maximo = 50.0f)
        {
            if (float.IsNaN(valor) || float.IsInfinity(valor))
                valor = padrao;
            return Math.Clamp(valor, 1.0f, maximo);
        }
    }
}
