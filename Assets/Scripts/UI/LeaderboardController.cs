using UnityEngine;
using UnityEngine.UIElements;
using GolfGame.Core;
using GolfGame.Multiplayer;

namespace GolfGame.UI
{
    /// <summary>
    /// Controls the leaderboard screen. Shows on AppState.Leaderboard.
    /// Displays top entries from LeaderboardManager with player highlight.
    /// </summary>
    public class LeaderboardController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement root;
        private VisualElement leaderboardRoot;
        private VisualElement leaderboardPanel;
        private Label loadingLabel;
        private Label emptyLabel;
        private Label playerRankLabel;
        private ScrollView entriesScroll;
        private Button backButton;

        private LeaderboardManager leaderboardManager;
        private string currentPlayerId;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }

        private void Start()
        {
            leaderboardManager = FindFirstObjectByType<LeaderboardManager>();

            var authService = ServiceLocator.Get<IAuthService>();
            currentPlayerId = authService?.PlayerId;

            root = uiDocument.rootVisualElement;

            leaderboardRoot = root.Q("leaderboard-root");
            leaderboardPanel = root.Q("leaderboard-panel");
            loadingLabel = root.Q<Label>("loading-label");
            emptyLabel = root.Q<Label>("empty-label");
            playerRankLabel = root.Q<Label>("player-rank-label");
            entriesScroll = root.Q<ScrollView>("entries-scroll");
            backButton = root.Q<Button>("back-button");

            backButton?.RegisterCallback<ClickEvent>(OnBackClicked);

            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged += HandleAppStateChanged;
                HandleAppStateChanged(AppManager.Instance.CurrentState);
            }

            if (leaderboardManager != null)
            {
                leaderboardManager.OnLeaderboardUpdated += HandleLeaderboardUpdated;
            }
        }

        private void OnDestroy()
        {
            backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);

            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged -= HandleAppStateChanged;
            }

            if (leaderboardManager != null)
            {
                leaderboardManager.OnLeaderboardUpdated -= HandleLeaderboardUpdated;
            }
        }

        private void HandleAppStateChanged(AppState state)
        {
            if (state == AppState.Leaderboard)
            {
                Show();
            }
            else
            {
                SetVisible(false);
            }
        }

        private void HandleLeaderboardUpdated(LeaderboardEntry[] entries, int playerRank)
        {
            if (root != null && root.style.display == DisplayStyle.Flex)
            {
                PopulateEntries(entries, playerRank);
            }
        }

        private void Show()
        {
            SetVisible(true);

            if (leaderboardManager != null)
            {
                PopulateEntries(leaderboardManager.CurrentEntries, leaderboardManager.PlayerRank);
            }
            else
            {
                PopulateEntries(System.Array.Empty<LeaderboardEntry>(), -1);
            }

            // Trigger fade-in on next frame so the transition plays
            if (leaderboardRoot != null)
            {
                leaderboardRoot.schedule.Execute(() =>
                {
                    if (leaderboardRoot != null)
                    {
                        leaderboardRoot.style.opacity = 1f;
                    }
                    if (leaderboardPanel != null)
                    {
                        leaderboardPanel.style.opacity = 1f;
                        leaderboardPanel.style.scale = new Scale(Vector2.one);
                    }
                });
            }
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!visible)
            {
                // Reset for next show
                if (leaderboardRoot != null)
                {
                    leaderboardRoot.style.opacity = 0f;
                }
                if (leaderboardPanel != null)
                {
                    leaderboardPanel.style.opacity = 0f;
                    leaderboardPanel.style.scale = new Scale(new Vector2(0.9f, 0.9f));
                }
            }
        }

        private void PopulateEntries(LeaderboardEntry[] entries, int playerRank)
        {
            if (entriesScroll == null)
                return;

            entriesScroll.Clear();

            bool hasEntries = entries != null && entries.Length > 0;

            if (loadingLabel != null)
            {
                loadingLabel.style.display = DisplayStyle.None;
            }

            if (emptyLabel != null)
            {
                emptyLabel.style.display = hasEntries ? DisplayStyle.None : DisplayStyle.Flex;
            }

            entriesScroll.style.display = hasEntries ? DisplayStyle.Flex : DisplayStyle.None;

            if (hasEntries)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    bool isCurrentPlayer = !string.IsNullOrEmpty(currentPlayerId)
                        && entries[i].PlayerId == currentPlayerId;
                    bool isLast = i == entries.Length - 1;
                    var row = CreateEntryRow(entries[i], isCurrentPlayer, isLast);
                    entriesScroll.Add(row);
                }
            }

            if (playerRankLabel != null)
            {
                if (playerRank >= 1)
                {
                    playerRankLabel.text = $"Your rank: #{playerRank}";
                }
                else
                {
                    playerRankLabel.text = "Not yet ranked";
                }
            }
        }

        private VisualElement CreateEntryRow(LeaderboardEntry entry, bool isCurrentPlayer, bool isLast)
        {
            var row = new VisualElement();
            row.AddToClassList("leaderboard-entry");

            if (isLast)
            {
                row.AddToClassList("leaderboard-entry-last");
            }

            if (isCurrentPlayer)
            {
                row.AddToClassList("leaderboard-entry-player");
            }

            var rankLabel = new Label(FormatRank(entry.Rank));
            rankLabel.AddToClassList("leaderboard-rank");
            if (entry.Rank == 1)
            {
                rankLabel.AddToClassList("text-gold");
            }

            var nameLabel = new Label(entry.DisplayName);
            nameLabel.AddToClassList("leaderboard-name");

            var distanceLabel = new Label(FormatDistance(entry.Distance));
            distanceLabel.AddToClassList("leaderboard-distance");

            if (isCurrentPlayer)
            {
                nameLabel.AddToClassList("text-gold");
                distanceLabel.AddToClassList("text-gold");
            }

            row.Add(rankLabel);
            row.Add(nameLabel);
            row.Add(distanceLabel);

            return row;
        }

        private void OnBackClicked(ClickEvent evt)
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.ReturnToTitle();
            }
        }

        /// <summary>
        /// Format a rank number for display.
        /// Top 3 get ordinal suffixes (1st, 2nd, 3rd), rest are plain numbers.
        /// </summary>
        public static string FormatRank(int rank)
        {
            switch (rank)
            {
                case 1:
                    return "1st";
                case 2:
                    return "2nd";
                case 3:
                    return "3rd";
                default:
                    return rank.ToString();
            }
        }

        /// <summary>
        /// Format a distance value for leaderboard display.
        /// </summary>
        public static string FormatDistance(float distance)
        {
            return $"{distance:F1} yds";
        }
    }
}
