# gnosia-customizer (WIP)

A mod based on BepinEx/Harmony that allows substituting custom sprites in places of the current character sprites, as well as other customizations.

## Prototype features as of 5/28/25:

The following assets can be modified: 

Characters
 * Sprites
 * Name
 * Attributes (internal stats used to determine certain actions)
 * Ability points
 * Known skills
 
Other
 * Backgrounds
 * Audio

Not included:
 * Mini character portraits
 * Dialogue

## Quick start guide

1. Install BepInEx.
2. Run the game once to create the plugins folder.
3. Place `GnosiaCustomizer.dll` into the plugins folder.
3. Unzip the `example_assets.zip` into the plugins folder. There should be three directories: sound, text, and textures.
4. Open the game.

## Customizations

### Character sprites

Place custom character sprites into the plugins/textures directory. Files must have the naming format `pXX_hYY.png`. pXX corresponds to the character ID. Here are the corresponding IDs for the Gnosia characters:

| ID | Name  |
|----|-------|
| p01  | Gina       |
| p02  | SQ         |
| p03  | Raqio      |
| p04  | Stella     |
| p05  | Shigemichi |
| p06  | Chipie     |
| p07  | Remnan     |
| p08  | Comet      |
| p09  | Yuriko     |
| p10 | Jonas      |
| p11 | Setsu      |
| p12 | Otome      |
| p13 | Sha-ming   |
| p14 | Kukrushka  |

The hYY corresponds to the "emotion ID". These should correspond with the following emotions:

| ID | Name  |
|----|-------|
| h00  | Neutral/Default |
| h01  | Happy/Relieved |
| h02  | Annoyed/Irritated |
| h03  | Wounded/Cold Sleep |
| h04  | Surprised |
| h05  | Thinking |
| h06  | Smug/Joking |
| h07  | Gnosia |

To customize a character's sprites, you MUST include all 8 emotions. If one is missing, the mod will not load the custom sprites.

For instance, to replace Chipie with custom sprites, these files would need to be present in plugins/textures: p06_h00.png, p06_h01.png, p06_h02.png, p06_h03.png, p06_h04.png, p06_h05.png, p06_h06.png, p06_h07.png.

### Character names and other config

In plugins/text, the mod will read any files with the format charaXX.yaml, where XX is the character ID that will be replaced by this config (see table above for character ID mapping). Here is an example of the contents of a chara config file:


```yaml
# The display name shown in‐game.
name: "Sayaka"
# 0 = Male, 1 = Female, 2 = Non-binary.
sex: 1
# The character's age.
age: 16
# The character's place of origin.
place: "Ultimate Pop Sensation"
 
# Affects the character's likelihood to perform certain actions. Must be between 0 and 1.
attributes:
  playful: 0.42
  social: 0.4
  logic: 0.2
  neat: 0.24
  desire: 0.62
  courage: 0.72

# The character's starting ability for each type. Must be between 0 and 1.
ability_start:
  charisma: 0.15
  intuition: 0.05
  charm: 0.15
  logic: 0.1
  perform: 0.3
  stealth: 0.2

# The character's max ability for each type. Must be between 0 and 1.
ability_max:
  charisma: 0.8
  intuition: 0.6
  charm: 0.7
  logic: 0.3
  perform: 0.9
  stealth: 0.6

# Skills the character is capable of using, provided they also have a high enough ability score.
known_skills:
  charisma_step_forward: true
  charisma_seek_agreement: true
  charisma_block_argument: true
  intuition_say_human: true
  intuition_dont_be_fooled: true
  charm_regret: true
  charm_collaborate: true
  logic_vote: true
  logic_dont_vote: true
  logic_definite_human: true
  logic_definite_enemy: true
  logic_freeze_all: true
  perform_retaliate: true
  perform_seek_help: true
  perform_exaggerate: true
  stealth_obfuscate: true
  stealth_small_talk: true
  stealth_grovel: true
```

### Audio

Custom audio can be placed in the plugins/sound folder in .wav format. If it matches the name of an in-game sound file, it will be substituted. You can unpack the game assets to see more, but here's a table with a quick guide for some common tracks:

| Name | Description  |
|----|-------|
| G_bgm_01_strm.wav  | Vote screen |
| G_bgm_02_strm.wav  | Discussion |
| G_bgm_10_strm.wav  | Title |
| G_bgm_15_strm.wav  | Configure loop |
| G_bgm_22_strm.wav  | Gnosia |
| G_bgm_23_strm.wav  | Happy end |
| G_bgm_24_strm.wav  | Bug victory |
| G_bgm_26_strm.wav  | Small talk |
| G_jin_02.wav  | No one was attacked last night |
|G_jin_03.wav| Last night, ___ disappeared |
| G_jin_06.wav |You were killed by Gnosia |
|G_se_pusyu.wav|____ was put into cold sleep|
| G_uta_02_strm.wav | Night phase music |

Ex. To replace the night phase music, save the track as G_uta_02_strm.wav in plugins/sound.