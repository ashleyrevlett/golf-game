---
issue: 11
title: Audio system + sound design
type: feature
tags: [audio, sfx, ambient, webgl, audio-manager]
---

# Audio System

## What
Centralized audio with pooled AudioSources, ball SFX, ambient audio, and UI sounds.

## Components
- **AudioManager**: Singleton, pool of 8 AudioSources, WebGL autoplay handling
- **AudioConfig**: ScriptableObject for clips and volume settings
- **BallAudioController**: Hit (pitch-scaled), bounce, roll loop, stop
- **AmbientAudioController**: Wind (volume matches speed), crowd on close shots
- **UIAudioController**: Static methods for click and score reveal

## WebGL
- AudioContext requires user gesture to initialize
- AudioManager.OnFirstUserGesture() called on first click

## Files
- `Assets/Scripts/Audio/{AudioConfig,AudioManager}.cs`
- `Assets/Scripts/Audio/{BallAudioController,AmbientAudioController,UIAudioController}.cs`
