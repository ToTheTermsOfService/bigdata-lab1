using System.Collections.Concurrent;
using System.Text.Json;
using MovieRevenue.Core;

namespace MovieRevenue.Api;

/// <summary>
/// Кешує опис моделей (*.json) замість читання з диска на кожен запит.
/// Файл перечитується лише коли змінився (після повторного тренування). Якщо Trainer саме зараз
/// його переписує і прочитати не вдалося, повертається попередня версія, а не помилка 500.
/// </summary>
public sealed class ModelInfoCache(string modelsDir)
{
    private readonly ConcurrentDictionary<string, (DateTime WriteTimeUtc, ModelInfo Info)> _cache = new();

    public ModelInfo Get(string file)
    {
        var path = Path.Combine(modelsDir, file);
        var writeTime = File.GetLastWriteTimeUtc(path);

        if (_cache.TryGetValue(file, out var cached) && cached.WriteTimeUtc == writeTime)
            return cached.Info;

        try
        {
            var info = JsonSerializer.Deserialize<ModelInfo>(File.ReadAllText(path))
                ?? throw new JsonException($"Порожній файл {file}");
            _cache[file] = (writeTime, info);
            return info;
        }
        catch (Exception ex) when (ex is IOException or JsonException && cached.Info is not null)
        {
            return cached.Info;
        }
    }
}
