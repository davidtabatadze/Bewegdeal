using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace Bewegdeal.Services
{
    public class FraudWordService(IFraudWordRepository FraudWordRepository, IMemoryCache Cache)
    {
        public async Task<List<string>> Load()
            => [.. (await FraudWordRepository.Load<FraudWordEntity>()).Select(i => i.Word)];

        public async Task<List<string>> LoadCached()
            => Cache.Get<List<string>>(CacheKeyEnum.FraudeWords) ?? await Load();

        public async Task Create(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                await FraudWordRepository.Create(new FraudWordEntity { Word = word.Trim() });
                Cache.Remove(CacheKeyEnum.FraudeWords);
            }
        }

        public async Task Delete(string word)
        {
            var existing = await FraudWordRepository.Load<FraudWordEntity>();
            var entity = existing.FirstOrDefault(e => e.Word == word.Trim());
            if (entity is not null)
            {
                await FraudWordRepository.Delete<FraudWordEntity>(entity.Id);
                Cache.Remove(CacheKeyEnum.FraudeWords);
            }
        }

    }
}
