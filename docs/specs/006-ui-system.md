---
issue: 6
title: UI system (UI Toolkit)
type: feature
tags: [ui, ui-toolkit, uxml, uss, hud, menu]
---

# UI System

## What
All game UI via UI Toolkit: main menu, settings, gameplay HUD, game over.

## Screens
- **Main Menu**: Play + Settings buttons, centered panel
- **Settings**: Volume slider, quality toggle, back button (PlayerPrefs)
- **Gameplay HUD**: Shot counter, best distance, wind, ready message, shot stats
- **Game Over**: Final best distance, play again, menu buttons

## Architecture
- One UIDocument per screen with layered sort orders
- Controllers subscribe to AppManager/GameManager/ScoringManager events
- Visibility via DisplayStyle.Flex/None driven by state machine
- Named elements for queryability (e.g., "play-button", "shot-counter")

## Design Tokens
- Dark semi-transparent panels (rgba(0,0,0,0.7))
- Green buttons (#4CAF50), gold best distance (#FFD700)
- 44px minimum touch targets
- 20px body text, 48px titles

## Files
- `Assets/UI/Styles/Common.uss`
- `Assets/UI/Screens/{MainMenu,Settings,GameplayHUD,GameOver}.uxml`
- `Assets/Scripts/UI/{MainMenu,Settings,GameplayHUD,GameOver}Controller.cs`
