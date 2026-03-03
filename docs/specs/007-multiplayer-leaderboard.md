---
issue: 7
title: Multiplayer + leaderboard integration
type: feature
tags: [multiplayer, leaderboard, auth, mock, service-locator]
---

# Multiplayer + Leaderboard

## What
Interface-based auth and leaderboard services with mock implementations.

## Interfaces
- `IAuthService`: GetPlayerToken(), GetPlayerInfo()
- `ILeaderboardService`: PostScore(), GetLeaderboard(), GetPlayerRank()

## Mock Implementations
- MockAuthService: hardcoded local player
- MockLeaderboardService: in-memory, pre-populated with 10 simulated players

## Infrastructure
- ServiceLocator: static Register<T>/Get<T> for DI
- LeaderboardManager: polls every 5s, posts on best distance update

## Data Flow
- ScoringManager.OnBestDistanceUpdated -> LeaderboardManager.PostScore
- LeaderboardManager.OnLeaderboardUpdated -> UI mini-leaderboard

## Files
- `Assets/Scripts/Multiplayer/{IAuthService,ILeaderboardService}.cs`
- `Assets/Scripts/Multiplayer/{MockAuthService,MockLeaderboardService}.cs`
- `Assets/Scripts/Multiplayer/{ServiceLocator,LeaderboardManager}.cs`
- `Assets/Scripts/Multiplayer/{PlayerInfo,LeaderboardEntry}.cs`
