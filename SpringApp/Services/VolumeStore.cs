using System.Collections.Generic;

namespace SpringApp.Services;

public static class VolumeStore
{
    // Dicionário global compartilhado para armazenar temporariamente 
    // os códigos das etiquetas bipadas na Conferência para a Expedição ler
    public static Dictionary<int, List<string>> CodigosDosVolumes = new();
}