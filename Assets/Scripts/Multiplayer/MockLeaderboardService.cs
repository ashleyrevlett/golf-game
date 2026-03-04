using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfGame.Multiplayer
{
    /// <summary>
    /// In-memory mock leaderboard pre-populated with simulated players.
    /// Maintains sorted order by distance (ascending -- closest wins).
    /// </summary>
    public class MockLeaderboardService : ILeaderboardService
    {
        private readonly List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        private readonly Random random;

        public MockLeaderboardService() : this(42) { }

        public MockLeaderboardService(int seed)
        {
            random = new Random(seed);
            PopulateSimulatedPlayers();
        }

        private void PopulateSimulatedPlayers()
        {
            string[] names = { "Eagle_Pro", "Birdie_King", "Par_Master", "Ace_Shot",
                              "Iron_Will", "Putter_Pro", "Wedge_Wiz", "Driver_Dan",
                              "Chip_Queen", "Bogey_Bob" };

            for (int i = 0; i < names.Length; i++)
            {
                entries.Add(new LeaderboardEntry
                {
                    PlayerId = $"sim_{i}",
                    DisplayName = names[i],
                    Distance = 0.5f + (float)(random.NextDouble() * 14.5f)
                });
            }

            SortAndRank();
        }

        public Task PostScoreAsync(string playerId, float distance)
        {
            // Find existing entry
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].PlayerId == playerId)
                {
                    // Only update if this is a better (lower) distance
                    if (distance < entries[i].Distance)
                    {
                        var entry = entries[i];
                        entry.Distance = distance;
                        entries[i] = entry;
                    }
                    SortAndRank();
                    return Task.CompletedTask;
                }
            }

            // New entry
            entries.Add(new LeaderboardEntry
            {
                PlayerId = playerId,
                DisplayName = playerId,
                Distance = distance
            });

            SortAndRank();
            return Task.CompletedTask;
        }

        public Task<LeaderboardEntry[]> GetLeaderboardAsync(int count)
        {
            int take = Math.Min(count, entries.Count);
            var result = new LeaderboardEntry[take];
            for (int i = 0; i < take; i++)
            {
                result[i] = entries[i];
            }
            return Task.FromResult(result);
        }

        public Task<int> GetPlayerRankAsync(string playerId)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].PlayerId == playerId)
                {
                    return Task.FromResult(entries[i].Rank);
                }
            }
            return Task.FromResult(-1);
        }

        /// <summary>
        /// Total number of entries. Useful for testing.
        /// </summary>
        public int EntryCount => entries.Count;

        private void SortAndRank()
        {
            entries.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                entry.Rank = i + 1;
                entries[i] = entry;
            }
        }
    }
}
