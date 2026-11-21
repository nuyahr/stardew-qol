namespace StardewQOL
{
    /// <summary>Configurações persistentes exclusivas do módulo Saúde/HP.</summary>
    public partial class ModConfig
    {
        // MÓDULO SAÚDE: recuperação de vida por atividade, independente da recuperação de energia.
        public bool SaudeModuloAtivado { get; set; } = true;

        public bool RecuperacaoSaudeGeralAtivada { get; set; } = false;
        public float SaudeGeralQuantidade { get; set; } = 1.5f;
        public float SaudeGeralIntervalo { get; set; } = 5.0f;

        public bool RecuperacaoSaudeAndandoAtivada { get; set; } = true;
        public float SaudeAndandoQuantidade { get; set; } = 1.0f;
        public float SaudeAndandoIntervalo { get; set; } = 8.0f;

        public bool RecuperacaoSaudeParadoAtivada { get; set; } = true;
        public float SaudeParadoQuantidade { get; set; } = 1.5f;
        public float SaudeParadoIntervalo { get; set; } = 4.0f;

        public bool RecuperacaoSaudeSentadoAtivada { get; set; } = true;
        public float SaudeSentadoQuantidade { get; set; } = 2.0f;
        public float SaudeSentadoIntervalo { get; set; } = 3.0f;


        // O cooldown de dano é compartilhado: pausa simultaneamente Saúde e Energia.
        public bool CooldownDanoAtivado { get; set; } = true;
        public float CooldownDanoSegundos { get; set; } = 2.0f;
        /// <summary>Valida os valores do módulo Saúde sem alterar as opções de Energia ou Tempo.</summary>
        private void ValidarSaude()
        {
            this.SaudeGeralQuantidade = ValidarQuantidade(this.SaudeGeralQuantidade, 1.5f, 2.0f);
            this.SaudeGeralIntervalo = ValidarIntervalo(this.SaudeGeralIntervalo, 5.0f, 5.0f);
            this.SaudeAndandoQuantidade = ValidarQuantidade(this.SaudeAndandoQuantidade, 1.0f, 1.5f);
            this.SaudeAndandoIntervalo = ValidarIntervalo(this.SaudeAndandoIntervalo, 8.0f, 8.0f);
            this.SaudeParadoQuantidade = ValidarQuantidade(this.SaudeParadoQuantidade, 1.5f, 2.0f);
            this.SaudeParadoIntervalo = ValidarIntervalo(this.SaudeParadoIntervalo, 4.0f, 4.0f);
            this.SaudeSentadoQuantidade = ValidarQuantidade(this.SaudeSentadoQuantidade, 2.0f, 3.0f);
            this.SaudeSentadoIntervalo = ValidarIntervalo(this.SaudeSentadoIntervalo, 3.0f, 3.0f);
            this.CooldownDanoSegundos = ValidarIntervalo(this.CooldownDanoSegundos, 2.0f, 3.0f, minimo: 1.0f);
        }
    }
}
