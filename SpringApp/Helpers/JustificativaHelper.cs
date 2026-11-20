namespace SpringApp.Helpers;

public static class JustificativaHelper
{
    private static readonly string[] MotivosPadrao =
    {
        "Almoço",
        "Priorização de pedido",
        "Fator pessoal",
        "Outro"
    };

    public static async Task<string?> SolicitarJustificativaAsync(Page pagina, string acaoTitulo)
    {
        string? escolhido = await pagina.DisplayActionSheet(
            $"Justificativa — {acaoTitulo}",
            "Cancelar",
            null,
            MotivosPadrao);

        if (string.IsNullOrWhiteSpace(escolhido) || escolhido == "Cancelar")
            return null;

        if (escolhido == "Outro")
        {
            while (true)
            {
                string? textoLivre = await pagina.DisplayPromptAsync(
                    "Detalhar Justificativa",
                    "Informe o motivo (obrigatório):",
                    "Confirmar",
                    "Voltar",
                    placeholder: "Ex.: Falta de etiqueta ou divergência no item");

                if (textoLivre == null)
                    return null;

                if (!string.IsNullOrWhiteSpace(textoLivre))
                    return $"Outro: {textoLivre.Trim()}";

                await pagina.DisplayAlert("Atenção", "Digite um motivo válido ou toque em Voltar.", "OK");
            }
        }

        return escolhido;
    }
}