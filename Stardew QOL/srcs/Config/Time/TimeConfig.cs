namespace StardewQOL
{
    /// <summary>Configurações do módulo Tempo e do ciclo de 24 horas.</summary>
    public partial class ModConfig
    {
        public bool TempoModuloAtivado { get; set; } = true;
        public bool TempoPersonalizadoAtivado { get; set; } = false;
        public float SegundosPorDezMinutos { get; set; } = 7.0f;
        public bool AceleracaoMesaAtivada { get; set; } = true;
        public float SegundosPorDezMinutosSentado { get; set; } = 2.0f;

        public bool AtivarTodos24h { get; set; } = false;
        public bool Ciclo24HorasAtivado { get; set; } = false;
        public bool Balanceamento24HorasAtivado { get; set; } = false;

        public bool Ciclo24hEfetivamenteAtivado => this.AtivarTodos24h || this.Ciclo24HorasAtivado;
        public bool Balanceamento24hEfetivamenteAtivado => this.AtivarTodos24h || this.Balanceamento24HorasAtivado;

        private void ValidarTempo()
        {
            this.SegundosPorDezMinutos = ValidarVelocidade(this.SegundosPorDezMinutos, 7.0f, 7.0f, 720.0f);
            this.SegundosPorDezMinutosSentado = ValidarVelocidade(this.SegundosPorDezMinutosSentado, 2.0f, 1.0f, 6.0f);
        }
    }
}
