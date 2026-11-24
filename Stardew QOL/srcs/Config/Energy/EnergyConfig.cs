namespace StardewQOL
{
    /// <summary>Configurações do módulo Energia e seu cooldown próprio.</summary>
    public partial class ModConfig
    {
        public bool EnergiaModuloAtivado { get; set; } = true;
        public bool RecuperacaoGeralAtivada { get; set; } = false;
        public float EnergiaGeralQuantidade { get; set; } = 1.5f;
        public float EnergiaGeralIntervalo { get; set; } = 5.0f;
        public bool RecuperacaoAndandoAtivada { get; set; } = true;
        public float EnergiaAndandoQuantidade { get; set; } = 1.0f;
        public float EnergiaAndandoIntervalo { get; set; } = 8.0f;
        public bool RecuperacaoParadoAtivada { get; set; } = true;
        public float EnergiaParadoQuantidade { get; set; } = 1.5f;
        public float EnergiaParadoIntervalo { get; set; } = 4.0f;
        public bool RecuperacaoSentadoAtivada { get; set; } = true;
        public float EnergiaSentadoQuantidade { get; set; } = 2.0f;
        public float EnergiaSentadoIntervalo { get; set; } = 3.0f;
        public bool RecuperacaoPescaAtivada { get; set; } = true;
        public float EnergiaPescaQuantidade { get; set; } = 2.0f;
        public float EnergiaPescaIntervalo { get; set; } = 3.5f;
        public bool CooldownEnergiaAtivado { get; set; } = true;
        public float CooldownEnergiaSegundos { get; set; } = 3.0f;

        private void ValidarEnergia()
        {
            this.EnergiaGeralQuantidade = ValidarQuantidade(this.EnergiaGeralQuantidade, 1.5f, 1.5f);
            this.EnergiaGeralIntervalo = ValidarIntervalo(this.EnergiaGeralIntervalo, 5.0f, 5.0f, minimo: 5.0f * 0.1f);
            this.EnergiaAndandoQuantidade = ValidarQuantidade(this.EnergiaAndandoQuantidade, 1.0f, 1.0f);
            this.EnergiaAndandoIntervalo = ValidarIntervalo(this.EnergiaAndandoIntervalo, 8.0f, 8.0f, minimo: 8.0f * 0.1f);
            this.EnergiaParadoQuantidade = ValidarQuantidade(this.EnergiaParadoQuantidade, 1.5f, 1.5f);
            this.EnergiaParadoIntervalo = ValidarIntervalo(this.EnergiaParadoIntervalo, 4.0f, 4.0f, minimo: 4.0f * 0.1f);
            this.EnergiaSentadoQuantidade = ValidarQuantidade(this.EnergiaSentadoQuantidade, 2.0f, 2.0f);
            this.EnergiaSentadoIntervalo = ValidarIntervalo(this.EnergiaSentadoIntervalo, 3.0f, 3.0f, minimo: 3.0f * 0.1f);
            this.EnergiaPescaQuantidade = ValidarQuantidade(this.EnergiaPescaQuantidade, 2.0f, 2.0f);
            this.EnergiaPescaIntervalo = ValidarIntervalo(this.EnergiaPescaIntervalo, 3.5f, 12.0f, minimo: 12.0f * 0.1f);
            this.CooldownEnergiaSegundos = ValidarIntervalo(this.CooldownEnergiaSegundos, 3.0f, 5.0f, minimo: 3.0f);
        }
    }
}
