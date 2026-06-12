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
                Cache.Remove(CacheKeyEnum.FraudeWordsCompiled);
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
                Cache.Remove(CacheKeyEnum.FraudeWordsCompiled);
            }
        }

        public async Task<bool> IsFraud(string message)
        {
            var compiled = Cache.Get<List<Func<string, string[], bool>>>(CacheKeyEnum.FraudeWordsCompiled)
                           ?? await LoadCompiled();

            var lower = message.ToLower();
            var tokens = lower.Split([' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':'], StringSplitOptions.RemoveEmptyEntries);
            return compiled.Any(match => match(lower, tokens));
        }

        private async Task<List<Func<string, string[], bool>>> LoadCompiled()
        {
            var compiled = (await LoadCached()).Select(Compile).ToList();
            Cache.Set(CacheKeyEnum.FraudeWordsCompiled, compiled);
            return compiled;
        }

        private static Func<string, string[], bool> Compile(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) { return (_, _) => false; }

            var sw = pattern.StartsWith("*");
            var ew = pattern.EndsWith("*");

            if (sw && ew)
            {
                var core = pattern[1..^1];
                return string.IsNullOrEmpty(core) ? (_, _) => false : (msg, _) => msg.Contains(core);
            }

            if (sw)
            {
                var suffix = pattern[1..];
                return string.IsNullOrEmpty(suffix) ? (_, _) => false : (_, toks) => toks.Any(t => t.EndsWith(suffix));
            }

            if (ew)
            {
                var prefix = pattern[..^1];
                return string.IsNullOrEmpty(prefix) ? (_, _) => false : (_, toks) => toks.Any(t => t.StartsWith(prefix));
            }

            return (_, toks) => toks.Any(t => t == pattern);
        }

    }
}
