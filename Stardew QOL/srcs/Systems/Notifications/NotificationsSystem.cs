using StardewValley;
using StardewValley.BellsAndWhistles;

namespace StardewQOL
{
    /// <summary>Centraliza a exibição de notificações opcionais do Stardew QOL.</summary>
    public partial class ModEntry
    {
        private void ExibirNotificacao(string chaveTraducao)
        {
            if (!this.Config.NotificacoesAtivadas)
                return;

            Game1.addHUDMessage(new HUDMessage(
                this.Helper.Translation.Get(chaveTraducao),
                HUDMessage.newQuest_type));
        }
    }
}
