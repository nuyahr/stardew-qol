using System;
using StardewModdingAPI;

namespace StardewQOL
{
    /// <summary>
    /// Declara somente os métodos do Generic Mod Config Menu usados pelo Stardew QOL.
    /// É uma interface local para acessar a API opcional sem exigir referência direta
    /// ao assembly do GMCM durante a compilação.
    /// </summary>
    public interface IGenericModConfigMenuApi
    {
        /// <summary>Registra o mod no GMCM e informa como restaurar/salvar as opções.</summary>
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        /// <summary>Cria uma página de configuração.</summary>
        void AddPage(IManifest mod, string pageId, Func<string>? pageTitle = null);
        /// <summary>Adiciona um link para outra página do menu.</summary>
        void AddPageLink(IManifest mod, string pageId, Func<string> text, Func<string>? tooltip = null);
        /// <summary>Adiciona um texto explicativo sem controle interativo.</summary>
        void AddParagraph(IManifest mod, Func<string> text);
        /// <summary>Adiciona uma opção liga/desliga vinculada à configuração.</summary>
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
        /// <summary>Adiciona um controle numérico com limites, passo e formatação opcionais.</summary>
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);
    }
}
