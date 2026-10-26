# Feelin’ Slimy — Game Design Document

Version: 0.1 — first playable test and Unity learning project

Source: מועדון גיימינג 4.10.html. This document describes a planned prototype, not an implemented game. Unresolved decisions are identified below; teaching arrangements and technical organisation are recommendations.

## 1. Concept

A 2D platformer inspired by Metroidvania games, built around the theme of body and mind. The player controls a Slime that absorbs small emotion creatures and gains physical abilities. Large enemies are dangerous; abilities help the player confront them and progress.

The first playable version contains one room, one type of small creature, one ability and one large enemy. Its purpose is to test the core interaction and provide a manageable introduction to Unity.

## 2. Design pillars

- Small emotion creatures are safe sources of power.
- Large enemies are dangerous and visually distinct.
- Absorbing an emotion produces an understandable physical change or ability.
- The acquired ability makes a meaningful difference to progress.
- Temporary shapes and effects are sufficient for the first test.

## 3. Core loop

1. Encounter a large enemy or challenge.
2. Find a small emotion creature.
3. Absorb it and acquire its ability.
4. Learn how to activate the ability.
5. Use it to deal with the large enemy or challenge.
6. Reach the room’s goal and complete the test.

## 4. Player

The Slime can move horizontally, jump and collide with the environment. It can absorb the selected emotion creature and activate the acquired ability. The game must communicate power acquisition and damage clearly.

**Chosen rules:**
- **Controls:** Move with `A`/`D` or `Left`/`Right` Arrow keys; Jump with `Space`, `W` or `Up` Arrow; Hold `E` near the emotion creature to pull & absorb; Press `S`, `Down` Arrow or `Left Shift` to perform the **Anger Stomp**; Press `R` to restart at any time.
- **Health & Failure:** The Slime has **3 HP** (displayed as 3 hearts in the UI). Touching the large enemy without stomping deals 1 damage, triggers brief invulnerability + flashing visual feedback, and knocks the Slime back. Losing all 3 HP or falling into a pit triggers the Failure state.

## 5. Small emotion creature and absorption

Include one creature type representing one chosen emotion. It does not damage the Slime. Its behaviour should make it easy to locate and distinguish from the large enemy.

Absorption grants the prototype’s single ability and produces visible and audible feedback. For this test, the absorbed creature should no longer remain available as an active pickup unless the group deliberately chooses a replenishing rule.

**Chosen rules:**
- **Emotion:** **Anger** ("Spark of Anger") — a small, vibrant crimson-orange sprite that hops/bobs gently on an upper-left platform and emits a warm glow. Safe to touch.
- **Absorption Method:** **Hold key to pull & absorb (`Hold E`)**. When the Slime is within absorption range, a prompt (`[Hold E to Absorb]`) and tether beam appear. Holding `E` pulls the creature toward the Slime over ~0.6 seconds with suction audio/visual feedback until it is absorbed.
- **Reset Rule:** Once absorbed, the creature disappears for the rest of the attempt and is restored when the room restarts (`R`).

## 6. Ability

Include one ability whose physical effect connects to the creature’s emotion. It must have a clear activation input, readable feedback and a useful effect against the enemy or obstacle.

**Chosen rules:**
- **Ability:** **Anger Stomp (Heavy Ground Pound)**.
- **Visual Transformation:** Upon absorbing the Anger creature, the Slime turns fiery red-orange with furrowed angry brows.
- **Activation & Effect:** Pressing `S`, `Down` Arrow or `Left Shift` activates the Stomp. If grounded, the Slime performs a quick wind-up hop before slamming downward; if already in mid-air, it briefly locks in place and slams straight down at high speed. Upon impact with the ground or enemy, it releases a crimson shockwave and camera shake.
- **Repeatability:** Permanent while held for the current run, with a short cooldown (0.5s) after landing.

## 7. Large enemy

Include one dangerous enemy that can hurt the Slime or block progress. Its behaviour and its response to the ability must be observable. Defeating the enemy is not yet a confirmed requirement: the chosen ability could enable another way of dealing with it.

**Chosen rules:**
- **Behaviour:** **Patrols & Defeated by Ability**. The large purple/dark "Gloom Brute" patrols back and forth in the lower arena guarding the path to the Exit Portal.
- **Damage to Player:** Walking or jumping into the enemy normally deals 1 HP damage and knocks the Slime away.
- **Response to Ability:** Hitting the enemy directly with the downward **Anger Stomp** or catching it within the landing shockwave radius crushes and defeats it (squash animation, flash, burst effect and defeat sound), clearing the path to the exit.

## 8. Test room

Build a single compact room with a start, ground/platforms, the large enemy, the small creature, space to try the ability and a clear destination. Arrange the room so that players encounter the problem and understand why the ability matters.

**Test Room Layout Sketch:**
```text
+-----------------------------------------------------------------------+
|  [Anger Creature]                                                     |
|   ========= (Upper Platform)                                          |
|                 ==== (Stepping Platform)       ==== (High Vantage)    |
|                                                                       |
|  [Slime]            [Wall]          <-- [Large Enemy] -->    [EXIT]   |
|===================+======+============================================|
|   Spawn Zone      |Bumper|            Enemy Patrol Arena     Goal     |
+-----------------------------------------------------------------------+
```
1. The Slime starts on the left and sees the patrolling Large Enemy ahead behind a low divider wall.
2. Stepping platforms lead up to the safe **Anger Creature** on the upper-left platform.
3. Holding `E` pulls and absorbs the creature, transforming the Slime and unlocking **Anger Stomp**.
4. A high vantage platform above the enemy arena invites the player to jump over the Large Enemy and press `S` / `Down` / `Shift` to **Stomp** down and crush it, then walk into the **Exit Portal** on the right.

## 9. Success, failure and restart

Success: the player reaches the agreed goal after resolving the room’s challenge. Show clear completion feedback.

Failure: implement a condition appropriate to the chosen enemy and damage rules. Show failure feedback and offer another attempt.

Restart: restore the player, enemy, creature and ability state to their initial conditions. Do not retain an acquired power accidentally between attempts.

## 10. Presentation

Visuals: distinct Slime, friendly small creature, dangerous large enemy, ground, obstacles and destination. Add a clear sign that the Slime has gained power, plus absorption, ability and damage feedback.

UI: movement/absorption/ability instructions, current ability indicator, success/failure feedback and restart. Health, time and energy indicators are conditional on the selected rules.

Audio: absorption/power acquisition, ability activation and effect, damage, success and failure. Music and ambient sound are optional for this first test.

Story: establish what the Slime wants, the creature’s emotion, how that emotion becomes a physical ability, and what the large enemy represents. A full story is outside this prototype’s scope.

## 11. Suggested Unity organisation

Use one gameplay scene and reusable prefabs for the Slime, emotion creature, enemy and exit. Keep movement, absorption, ability behaviour, enemy behaviour and game-state logic understandable and separate enough to demonstrate individually. Expose useful tuning values in the Inspector.

Suggested folders: Scenes, Scripts, Prefabs, Art, Audio and UI. Exact Unity version, renderer and input approach should be chosen when setting up the project.

## 12. Learning approach — recommendation

Prepare a working instructor version and a starter copy. Give beginners small changes with immediate visible results: position platforms, change movement values, assign a sprite, configure a trigger, connect an ability reference and add a sound. Introduce GameObjects, transforms, components, colliders, prefabs, Inspector references and Play mode through these tasks.

Keep advanced architecture out of the initial lesson. Session length and participant experience have not yet been specified, so this is not a timed lesson plan.

## 13. Build order and acceptance checklist

### A. Choose the test rules
- [x] Choose the emotion and its connection to the physical ability (Anger -> Heavy Stomp / Ground Pound).
- [x] Choose absorption controls and conditions (Hold `E` within range to pull & absorb).
- [x] Define ability effect, activation and limits (`S` / `Down` / `Left Shift` slams down & emits shockwave; permanent with short cooldown).
- [x] Define enemy behaviour and ability response (Patrols arena guarding exit; deals 1 HP contact damage; crushed & defeated by Anger Stomp).
- [x] Define goal, damage and failure conditions (3 HP + pit fall = Failure; reaching Exit Portal = Success; `R` restarts all state).
- [x] Sketch the test room.

### B. Build the playable foundation
- [x] Create the Unity project and gameplay scene.
- [x] Add temporary room geometry and colliders.
- [x] Add a camera that clearly shows the playable area.
- [x] Implement Slime movement, jumping and ground detection.
- [x] Confirm the player cannot pass through the ground or obstacles.

### C. Build the core mechanic
- [x] Create the small-creature prefab and its simple behaviour.
- [x] Confirm the small creature does not hurt the player.
- [x] Implement absorption and power acquisition.
- [x] Implement one ability and its input.
- [x] Create the large-enemy prefab and its behaviour.
- [x] Implement damage and the enemy’s response to the ability.
- [x] Confirm the ability enables the intended progress.

### D. Complete the test loop
- [x] Add the destination and success detection.
- [x] Add the chosen failure condition.
- [x] Add restart and restore all initial gameplay state.
- [x] Add controls, ability indicator and result feedback.
- [x] Add temporary visual feedback and event sounds.

### E. Prepare the teaching project
- [x] Expose useful movement, creature, enemy and ability settings.
- [x] Name objects, components and assets clearly.
- [x] Prepare the working instructor version and beginner starter copy.
- [x] Choose small participant tasks suited to the available time.
- [x] Confirm the project opens and runs on participant computers.

### F. Playtest
- [ ] Can players distinguish safe small creatures from dangerous enemies?
- [ ] Can they understand how to absorb and acquire power?
- [ ] Is the ability enjoyable to use?
- [ ] Does it meaningfully help with the large enemy?
- [ ] Are completion, failure and restart understandable?
- [ ] Select one improvement based on observation.

## 14. Outside the first playable scope

A full Metroidvania map, multiple rooms, additional emotions and abilities, multiple enemy types, final art, a complete story and music production. Revisit these after the single-room interaction has been tested.
