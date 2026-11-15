#!/bin/sh
# Generated 2026-11-15. Moves UNREFERENCED art out of Assets, keeping the same folder shape.
# Read Docs/Unused-Art-Report.md first. Files + .meta move together, so GUIDs survive a move back.
set -e
cd "$(dirname "$0")/.."
DEST="ArtSource_Unused"

mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/preview.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/preview.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/preview.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/preview.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/preview.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/preview.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Wizard"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Necromancer"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_D"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_D/Demon_D.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_D/Demon_D.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_D/Demon_D.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_D/Demon_D.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_D/Demon_D.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_D/Demon_D.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Black Knight_C"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_C/Black Knight_C.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_C/Black Knight_C.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Black Knight_C/Black Knight_C.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_C/Black Knight_C.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_C/Black Knight_C.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Black Knight_C/Black Knight_C.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Warlock"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Warlock/Warlock.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Warlock/Warlock.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Warlock/Warlock.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Warlock/Warlock.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Warlock/Warlock.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Warlock/Warlock.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Demoness_A"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demoness_A/Demoness_A.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demoness_A/Demoness_A.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demoness_A/Demoness_A.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demoness_A/Demoness_A.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demoness_A/Demoness_A.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demoness_A/Demoness_A.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Black Knight_A"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_A/Black Knight_A.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_A/Black Knight_A.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Black Knight_A/Black Knight_A.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_A/Black Knight_A.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_A/Black Knight_A.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Black Knight_A/Black Knight_A.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art"
if [ -f "Assets/IdleRPG/Art/Greenwake title.png" ]; then mv "Assets/IdleRPG/Art/Greenwake title.png" "$DEST/IdleRPG/Art/Greenwake title.png"; fi
if [ -f "Assets/IdleRPG/Art/Greenwake title.png.meta" ]; then mv "Assets/IdleRPG/Art/Greenwake title.png.meta" "$DEST/IdleRPG/Art/Greenwake title.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Swordsman"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Swordsman/Swordsman.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Swordsman/Swordsman.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Swordsman/Swordsman.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Swordsman/Swordsman.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Swordsman/Swordsman.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Swordsman/Swordsman.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Knight Templar"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Knight Templar/Knight Templar.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Knight Templar/Knight Templar.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Knight Templar/Knight Templar.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Knight Templar/Knight Templar.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Knight Templar/Knight Templar.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Knight Templar/Knight Templar.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Greatsword Skeleton"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Greatsword Skeleton/Greatsword Skeleton.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Greatsword Skeleton/Greatsword Skeleton.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Greatsword Skeleton/Greatsword Skeleton.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Greatsword Skeleton/Greatsword Skeleton.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Greatsword Skeleton/Greatsword Skeleton.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Greatsword Skeleton/Greatsword Skeleton.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Werebear"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Werebear/Werebear.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Werebear/Werebear.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Werebear/Werebear.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Werebear/Werebear.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Werebear/Werebear.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Werebear/Werebear.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_E"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_E/Demon_E.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_E/Demon_E.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_E/Demon_E.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_E/Demon_E.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_E/Demon_E.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_E/Demon_E.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Priest"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Priest/Priest.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Priest/Priest.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Orc rider"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Orc rider/Orc rider.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Orc rider/Orc rider.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Orc rider/Orc rider.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Orc rider/Orc rider.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Orc rider/Orc rider.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Orc rider/Orc rider.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Knight"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Knight/Knight.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Knight/Knight.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Knight/Knight.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Knight/Knight.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Knight/Knight.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Knight/Knight.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Flame Golem"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Flame Golem/Flame Golem.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Flame Golem/Flame Golem.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Flame Golem/Flame Golem.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Flame Golem/Flame Golem.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Flame Golem/Flame Golem.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Flame Golem/Flame Golem.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Armored Axeman"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Armored Axeman/Armored Axeman.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Armored Axeman/Armored Axeman.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Armored Axeman/Armored Axeman.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Armored Axeman/Armored Axeman.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Armored Axeman/Armored Axeman.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Armored Axeman/Armored Axeman.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Werewolf"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Werewolf/Werewolf.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Werewolf/Werewolf.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Werewolf/Werewolf.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Werewolf/Werewolf.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Werewolf/Werewolf.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Werewolf/Werewolf.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Slime"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Slime/Slime.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Slime/Slime.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Slime/Slime.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Slime/Slime.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Slime/Slime.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Slime/Slime.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Lava Slime"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Lava Slime/Lava Slime.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Lava Slime/Lava Slime.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Lava Slime/Lava Slime.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Lava Slime/Lava Slime.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Lava Slime/Lava Slime.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Lava Slime/Lava Slime.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Lancer"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Lancer/Lancer.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Lancer/Lancer.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Lancer/Lancer.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Lancer/Lancer.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Lancer/Lancer.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Lancer/Lancer.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster/Eyeball Monster.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster/Eyeball Monster.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster/Eyeball Monster.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster/Eyeball Monster.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster/Eyeball Monster.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster/Eyeball Monster.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Black Knight_B"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_B/Black Knight_B.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_B/Black Knight_B.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Black Knight_B/Black Knight_B.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_B/Black Knight_B.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_B/Black Knight_B.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Black Knight_B/Black Knight_B.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Armored Orc"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Armored Orc/Armored Orc.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Armored Orc/Armored Orc.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Armored Orc/Armored Orc.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Armored Orc/Armored Orc.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Armored Orc/Armored Orc.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Armored Orc/Armored Orc.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Archer"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Archer/Archer.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Archer/Archer.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Archer/Archer.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Archer/Archer.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Archer/Archer.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Archer/Archer.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Skeleton"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Skeleton/Skeleton.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Skeleton/Skeleton.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Skeleton/Skeleton.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Skeleton/Skeleton.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Skeleton/Skeleton.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Skeleton/Skeleton.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Soldier"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Soldier/Soldier.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Soldier/Soldier.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Soldier/Soldier.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Soldier/Soldier.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Soldier/Soldier.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Soldier/Soldier.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Armored Skeleton"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Armored Skeleton/Armored Skeleton.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Armored Skeleton/Armored Skeleton.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Armored Skeleton/Armored Skeleton.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Armored Skeleton/Armored Skeleton.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Armored Skeleton/Armored Skeleton.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Armored Skeleton/Armored Skeleton.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/preview.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/preview.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/preview.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/preview.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/preview.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/preview.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Minotaur"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Minotaur/Minotaur.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Minotaur/Minotaur.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Minotaur/Minotaur.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Minotaur/Minotaur.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Minotaur/Minotaur.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Minotaur/Minotaur.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Skeleton Archer"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Skeleton Archer/Skeleton Archer.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Skeleton Archer/Skeleton Archer.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Skeleton Archer/Skeleton Archer.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Skeleton Archer/Skeleton Archer.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Skeleton Archer/Skeleton Archer.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Skeleton Archer/Skeleton Archer.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_B"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_B/Demon_B.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_B/Demon_B.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_B/Demon_B.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_B/Demon_B.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_B/Demon_B.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_B/Demon_B.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Ghostfire"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Ghostfire/Ghostfire.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Ghostfire/Ghostfire.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Ghostfire/Ghostfire.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Ghostfire/Ghostfire.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Ghostfire/Ghostfire.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Ghostfire/Ghostfire.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Orc"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Orc/Orc.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Orc/Orc.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Orc/Orc.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Orc/Orc.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Orc/Orc.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Orc/Orc.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Hellhound"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Hellhound/Hellhound.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Hellhound/Hellhound.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Hellhound/Hellhound.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Hellhound/Hellhound.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Hellhound/Hellhound.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Hellhound/Hellhound.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Demoness_B"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demoness_B/Demoness_B.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demoness_B/Demoness_B.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demoness_B/Demoness_B.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demoness_B/Demoness_B.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demoness_B/Demoness_B.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demoness_B/Demoness_B.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_A"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_A/Demon_A.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_A/Demon_A.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_A/Demon_A.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_A/Demon_A.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_A/Demon_A.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_A/Demon_A.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Blood Monster_A"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Blood Monster_A/Blood Monster_A.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Blood Monster_A/Blood Monster_A.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Blood Monster_A/Blood Monster_A.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Blood Monster_A/Blood Monster_A.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Blood Monster_A/Blood Monster_A.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Blood Monster_A/Blood Monster_A.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Blood Monster_B"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Blood Monster_B/Blood Monster_B.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Blood Monster_B/Blood Monster_B.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Blood Monster_B/Blood Monster_B.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Blood Monster_B/Blood Monster_B.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Blood Monster_B/Blood Monster_B.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Blood Monster_B/Blood Monster_B.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_C"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_C/Demon_C.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_C/Demon_C.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_C/Demon_C.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_C/Demon_C.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Demon_C/Demon_C.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Demon_C/Demon_C.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Hellbat"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Hellbat/Hellbat.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Hellbat/Hellbat.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Hellbat/Hellbat.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Hellbat/Hellbat.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Hellbat/Hellbat.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Hellbat/Hellbat.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Bat"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Bat/Bat.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Bat/Bat.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Bat/Bat.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Bat/Bat.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Bat/Bat.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Bat/Bat.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/wand_crystal.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/wand_crystal.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/wand_crystal.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/wand_crystal.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/wand_crystal.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/wand_crystal.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/torch.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/torch.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/torch.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/torch.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/torch.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/torch.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/sword_long.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/sword_long.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/sword_long.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/sword_long.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/sword_long.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/sword_long.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/staff_wizard.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/staff_wizard.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/staff_wizard.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/staff_wizard.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/staff_wizard.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/staff_wizard.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/spellbook_red.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/spellbook_red.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/spellbook_red.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/spellbook_red.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/spellbook_red.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/spellbook_red.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/spell_heal.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/spell_heal.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/spell_heal.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/spell_heal.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/spell_heal.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/spell_heal.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/spell_fireball.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/spell_fireball.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/spell_fireball.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/spell_fireball.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/spell_fireball.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/spell_fireball.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/skill_slash.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/skill_slash.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/skill_slash.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/skill_slash.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/skill_slash.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/skill_slash.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/shield_round.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/shield_round.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/shield_round.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/shield_round.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/shield_round.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/shield_round.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/scroll_sealed.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/scroll_sealed.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/scroll_sealed.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/scroll_sealed.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/scroll_sealed.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/scroll_sealed.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_poison.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_poison.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_poison.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_poison.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_poison.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_poison.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_mana.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_mana.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_mana.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_mana.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_mana.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_mana.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_health.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_health.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_health.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_health.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_health.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/potion_health.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/meat_leg.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/meat_leg.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/meat_leg.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/meat_leg.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/meat_leg.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/meat_leg.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/key_brass.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/key_brass.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/key_brass.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/key_brass.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/key_brass.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/key_brass.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/helmet_iron.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/helmet_iron.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/helmet_iron.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/helmet_iron.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/helmet_iron.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/helmet_iron.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/gem_ruby.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/gem_ruby.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/gem_ruby.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/gem_ruby.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/gem_ruby.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/gem_ruby.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/dagger.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/dagger.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/dagger.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/dagger.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/dagger.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/dagger.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/coin_gold.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/coin_gold.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/coin_gold.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/coin_gold.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/coin_gold.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/coin_gold.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/chest_treasure.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/chest_treasure.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/chest_treasure.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/chest_treasure.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/chest_treasure.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/chest_treasure.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/bow_wood.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/bow_wood.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/bow_wood.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/bow_wood.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/bow_wood.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/bow_wood.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/axe_battle.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/axe_battle.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/axe_battle.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/axe_battle.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/axe_battle.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/axe_battle.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/armor_leather.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/armor_leather.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/armor_leather.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/armor_leather.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/armor_leather.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/armor_leather.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/ale_mug.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/ale_mug.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/ale_mug.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/ale_mug.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/512/ale_mug.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/512/ale_mug.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Wizard"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack02(With magic effects).png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack02(With magic effects).png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack02(With magic effects).png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack02(With magic effects).png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack02(With magic effects).png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack02(With magic effects).png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Necromancer"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Summon(With magic effects).png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Summon(With magic effects).png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Summon(With magic effects).png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Summon(With magic effects).png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Summon(With magic effects).png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Summon(With magic effects).png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Black Knight_C"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_C/Black Knight_C_Attack03(No beam effects).png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_C/Black Knight_C_Attack03(No beam effects).png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Black Knight_C/Black Knight_C_Attack03(No beam effects).png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_C/Black Knight_C_Attack03(No beam effects).png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Black Knight_C/Black Knight_C_Attack03(No beam effects).png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Black Knight_C/Black Knight_C_Attack03(No beam effects).png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack02.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack02.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack02.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack02.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack02.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack02.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Resources/Vfx"
if [ -f "Assets/IdleRPG/Resources/Vfx/ice_shatter.png" ]; then mv "Assets/IdleRPG/Resources/Vfx/ice_shatter.png" "$DEST/IdleRPG/Resources/Vfx/ice_shatter.png"; fi
if [ -f "Assets/IdleRPG/Resources/Vfx/ice_shatter.png.meta" ]; then mv "Assets/IdleRPG/Resources/Vfx/ice_shatter.png.meta" "$DEST/IdleRPG/Resources/Vfx/ice_shatter.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Wizard"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack01_Effect.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack01_Effect.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack01_Effect.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack01_Effect.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack01_Effect.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack01_Effect.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Warlock"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Warlock/Warlock_Attack02_Effect.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Warlock/Warlock_Attack02_Effect.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Warlock/Warlock_Attack02_Effect.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Warlock/Warlock_Attack02_Effect.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Warlock/Warlock_Attack02_Effect.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Warlock/Warlock_Attack02_Effect.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Lava Slime"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Lava Slime/Lava Slime_Attack02(No spike effects).png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Lava Slime/Lava Slime_Attack02(No spike effects).png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Lava Slime/Lava Slime_Attack02(No spike effects).png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Lava Slime/Lava Slime_Attack02(No spike effects).png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Lava Slime/Lava Slime_Attack02(No spike effects).png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Lava Slime/Lava Slime_Attack02(No spike effects).png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack03.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack03.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack03.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack03.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack03.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack03.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Lancer"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Lancer/Lancer_Walk02.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Lancer/Lancer_Walk02.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Lancer/Lancer_Walk02.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Lancer/Lancer_Walk02.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Lancer/Lancer_Walk02.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Lancer/Lancer_Walk02.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Knight Templar"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Knight Templar/Knight Templar_Walk02.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Knight Templar/Knight Templar_Walk02.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Knight Templar/Knight Templar_Walk02.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Knight Templar/Knight Templar_Walk02.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Knight Templar/Knight Templar_Walk02.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Knight Templar/Knight Templar_Walk02.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Walk.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Walk.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Walk.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Walk.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Walk.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Walk.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Wizard"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack02_Effect.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack02_Effect.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack02_Effect.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack02_Effect.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack02_Effect.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Wizard/Wizard_Attack02_Effect.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Necromancer"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Sumon_Effect.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Sumon_Effect.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Sumon_Effect.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Sumon_Effect.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Sumon_Effect.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Sumon_Effect.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Ghostfire"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Ghostfire/Ghostfire_Attack02(No beam effects).png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Ghostfire/Ghostfire_Attack02(No beam effects).png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Ghostfire/Ghostfire_Attack02(No beam effects).png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Ghostfire/Ghostfire_Attack02(No beam effects).png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Ghostfire/Ghostfire_Attack02(No beam effects).png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Ghostfire/Ghostfire_Attack02(No beam effects).png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack01.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack01.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack01.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack01.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack01.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Attack01.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/combat_bg.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/combat_bg.png" "$DEST/IdleRPG/Art/Placeholder/combat_bg.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/combat_bg.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/combat_bg.png.meta" "$DEST/IdleRPG/Art/Placeholder/combat_bg.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/wand_crystal.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/wand_crystal.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/wand_crystal.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/wand_crystal.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/wand_crystal.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/wand_crystal.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/torch.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/torch.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/torch.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/torch.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/torch.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/torch.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/sword_long.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/sword_long.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/sword_long.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/sword_long.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/sword_long.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/sword_long.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/staff_wizard.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/staff_wizard.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/staff_wizard.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/staff_wizard.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/staff_wizard.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/staff_wizard.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/spellbook_red.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/spellbook_red.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/spellbook_red.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/spellbook_red.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/spellbook_red.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/spellbook_red.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/spell_heal.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/spell_heal.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/spell_heal.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/spell_heal.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/spell_heal.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/spell_heal.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/spell_fireball.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/spell_fireball.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/spell_fireball.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/spell_fireball.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/spell_fireball.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/spell_fireball.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/skill_slash.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/skill_slash.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/skill_slash.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/skill_slash.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/skill_slash.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/skill_slash.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/shield_round.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/shield_round.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/shield_round.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/shield_round.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/shield_round.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/shield_round.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/scroll_sealed.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/scroll_sealed.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/scroll_sealed.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/scroll_sealed.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/scroll_sealed.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/scroll_sealed.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_poison.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_poison.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_poison.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_poison.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_poison.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_poison.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_mana.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_mana.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_mana.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_mana.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_mana.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_mana.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_health.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_health.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_health.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_health.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_health.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/potion_health.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/meat_leg.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/meat_leg.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/meat_leg.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/meat_leg.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/meat_leg.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/meat_leg.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/key_brass.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/key_brass.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/key_brass.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/key_brass.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/key_brass.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/key_brass.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/helmet_iron.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/helmet_iron.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/helmet_iron.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/helmet_iron.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/helmet_iron.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/helmet_iron.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/gem_ruby.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/gem_ruby.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/gem_ruby.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/gem_ruby.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/gem_ruby.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/gem_ruby.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/dagger.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/dagger.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/dagger.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/dagger.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/dagger.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/dagger.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/coin_gold.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/coin_gold.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/coin_gold.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/coin_gold.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/coin_gold.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/coin_gold.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/chest_treasure.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/chest_treasure.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/chest_treasure.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/chest_treasure.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/chest_treasure.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/chest_treasure.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/bow_wood.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/bow_wood.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/bow_wood.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/bow_wood.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/bow_wood.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/bow_wood.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/axe_battle.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/axe_battle.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/axe_battle.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/axe_battle.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/axe_battle.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/axe_battle.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/armor_leather.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/armor_leather.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/armor_leather.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/armor_leather.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/armor_leather.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/armor_leather.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/ale_mug.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/ale_mug.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/ale_mug.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/ale_mug.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/256/ale_mug.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/256/ale_mug.png.meta"; fi
mkdir -p "$DEST/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Soldier/Soldier"
if [ -f "Assets/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Soldier/Soldier/Soldier_Attack01.png" ]; then mv "Assets/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Soldier/Soldier/Soldier_Attack01.png" "$DEST/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Soldier/Soldier/Soldier_Attack01.png"; fi
if [ -f "Assets/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Soldier/Soldier/Soldier_Attack01.png.meta" ]; then mv "Assets/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Soldier/Soldier/Soldier_Attack01.png.meta" "$DEST/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Soldier/Soldier/Soldier_Attack01.png.meta"; fi
mkdir -p "$DEST/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Orc/Orc"
if [ -f "Assets/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Orc/Orc/Orc_Idle.png" ]; then mv "Assets/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Orc/Orc/Orc_Idle.png" "$DEST/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Orc/Orc/Orc_Idle.png"; fi
if [ -f "Assets/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Orc/Orc/Orc_Idle.png.meta" ]; then mv "Assets/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Orc/Orc/Orc_Idle.png.meta" "$DEST/ThirdParty/TinyRPG_01_SoldierOrc/Characters(100x100 split)/Orc/Orc/Orc_Idle.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Priest"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Heal(With magic effects).png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Heal(With magic effects).png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Heal(With magic effects).png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Heal(With magic effects).png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Heal(With magic effects).png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Heal(With magic effects).png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Necromancer"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Attack02_Effect.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Attack02_Effect.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Attack02_Effect.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Attack02_Effect.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Attack02_Effect.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Necromancer/Necromancer_Attack02_Effect.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster/Eyeball Monster_Attack03(No beam effects).png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster/Eyeball Monster_Attack03(No beam effects).png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster/Eyeball Monster_Attack03(No beam effects).png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster/Eyeball Monster_Attack03(No beam effects).png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster/Eyeball Monster_Attack03(No beam effects).png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Eyeball Monster/Eyeball Monster_Attack03(No beam effects).png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Idle.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Idle.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Idle.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Idle.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Idle.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Idle.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Resources/Vfx"
if [ -f "Assets/IdleRPG/Resources/Vfx/heavy_smash.png" ]; then mv "Assets/IdleRPG/Resources/Vfx/heavy_smash.png" "$DEST/IdleRPG/Resources/Vfx/heavy_smash.png"; fi
if [ -f "Assets/IdleRPG/Resources/Vfx/heavy_smash.png.meta" ]; then mv "Assets/IdleRPG/Resources/Vfx/heavy_smash.png.meta" "$DEST/IdleRPG/Resources/Vfx/heavy_smash.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Skeleton"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Skeleton/Skeleton_Summon.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Skeleton/Skeleton_Summon.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Skeleton/Skeleton_Summon.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Skeleton/Skeleton_Summon.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Skeleton/Skeleton_Summon.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Skeleton/Skeleton_Summon.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Priest"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Attack_effect.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Attack_effect.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Attack_effect.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Attack_effect.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Attack_effect.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Attack_effect.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Priest"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Heal_effect.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Heal_effect.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Heal_effect.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Heal_effect.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Heal_effect.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Priest/Priest_Heal_effect.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Hurt.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Hurt.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Hurt.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Hurt.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Hurt.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Hurt.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc"
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Death.png" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Death.png" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Death.png"; fi
if [ -f "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Death.png.meta" ]; then mv "Assets/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Death.png.meta" "$DEST/IdleRPG/Art/Characters/TinyRPG/Elite Orc/Elite Orc_Death.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_sword_short.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_sword_short.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_sword_short.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_sword_short.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_sword_short.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_sword_short.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_staff.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_staff.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_staff.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_staff.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_staff.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_staff.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_shield_wood.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_shield_wood.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_shield_wood.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_shield_wood.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_shield_wood.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_shield_wood.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_potion_red.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_potion_red.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_potion_red.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_potion_red.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_potion_red.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_potion_red.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_potion_blue.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_potion_blue.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_potion_blue.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_potion_blue.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_potion_blue.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_potion_blue.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_key.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_key.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_key.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_key.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_key.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_key.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_helmet_iron.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_helmet_iron.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_helmet_iron.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_helmet_iron.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_helmet_iron.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_helmet_iron.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_gem_ruby.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_gem_ruby.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_gem_ruby.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_gem_ruby.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_gem_ruby.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_gem_ruby.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_coin.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_coin.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_coin.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_coin.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_coin.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_coin.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_chest.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_chest.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_chest.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_chest.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_chest.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_chest.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_bow.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_bow.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_bow.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_bow.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_bow.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_bow.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_axe.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_axe.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_axe.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_axe.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_axe.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxi_axe.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_panel_wood.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_panel_wood.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_panel_wood.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_panel_wood.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_panel_wood.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_panel_wood.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_stun.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_stun.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_stun.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_stun.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_stun.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_stun.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_shield.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_shield.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_shield.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_shield.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_shield.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_shield.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_regen.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_regen.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_regen.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_regen.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_regen.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_regen.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_poison.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_poison.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_poison.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_poison.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_poison.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_poison.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_freeze.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_freeze.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_freeze.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_freeze.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_freeze.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_freeze.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_burn.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_burn.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_burn.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_burn.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_burn.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxst_burn.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_teleport.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_teleport.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_teleport.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_teleport.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_teleport.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_teleport.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_lightning.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_lightning.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_lightning.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_lightning.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_lightning.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_lightning.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_frost_bolt.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_frost_bolt.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_frost_bolt.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_frost_bolt.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_frost_bolt.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_frost_bolt.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_fireball.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_fireball.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_fireball.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_fireball.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_fireball.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_wiz_fireball.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_war_cry.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_war_cry.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_war_cry.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_war_cry.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_war_cry.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_war_cry.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_slash.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_slash.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_slash.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_slash.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_slash.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_slash.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_shield_bash.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_shield_bash.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_shield_bash.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_shield_bash.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_shield_bash.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_shield_bash.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_berserk.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_berserk.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_berserk.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_berserk.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_berserk.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_war_berserk.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_stealth.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_stealth.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_stealth.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_stealth.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_stealth.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_stealth.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_smoke_bomb.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_smoke_bomb.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_smoke_bomb.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_smoke_bomb.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_smoke_bomb.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_smoke_bomb.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_poison_blade.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_poison_blade.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_poison_blade.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_poison_blade.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_poison_blade.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_poison_blade.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_backstab.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_backstab.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_backstab.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_backstab.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_backstab.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rog_backstab.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_pet_wolf.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_pet_wolf.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_pet_wolf.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_pet_wolf.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_pet_wolf.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_pet_wolf.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_multi_shot.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_multi_shot.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_multi_shot.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_multi_shot.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_multi_shot.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_multi_shot.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_fire_arrow.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_fire_arrow.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_fire_arrow.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_fire_arrow.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_fire_arrow.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_fire_arrow.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_aimed_shot.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_aimed_shot.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_aimed_shot.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_aimed_shot.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_aimed_shot.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_rng_aimed_shot.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_summon_skeleton.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_summon_skeleton.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_summon_skeleton.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_summon_skeleton.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_summon_skeleton.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_summon_skeleton.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_raise_dead.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_raise_dead.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_raise_dead.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_raise_dead.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_raise_dead.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_raise_dead.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_life_drain.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_life_drain.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_life_drain.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_life_drain.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_life_drain.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_life_drain.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_curse.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_curse.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_curse.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_curse.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_curse.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_nec_curse.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_resurrect.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_resurrect.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_resurrect.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_resurrect.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_resurrect.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_resurrect.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_holy_light.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_holy_light.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_holy_light.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_holy_light.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_holy_light.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_holy_light.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_heal.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_heal.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_heal.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_heal.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_heal.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_heal.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_divine_shield.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_divine_shield.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_divine_shield.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_divine_shield.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_divine_shield.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/pxs_clr_divine_shield.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_werewolf.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_werewolf.png" "$DEST/IdleRPG/Art/Placeholder/enemy_werewolf.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_werewolf.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_werewolf.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_werewolf.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_werebear.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_werebear.png" "$DEST/IdleRPG/Art/Placeholder/enemy_werebear.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_werebear.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_werebear.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_werebear.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_warlock.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_warlock.png" "$DEST/IdleRPG/Art/Placeholder/enemy_warlock.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_warlock.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_warlock.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_warlock.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_swordsman.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_swordsman.png" "$DEST/IdleRPG/Art/Placeholder/enemy_swordsman.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_swordsman.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_swordsman.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_swordsman.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_skeleton_archer.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_skeleton_archer.png" "$DEST/IdleRPG/Art/Placeholder/enemy_skeleton_archer.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_skeleton_archer.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_skeleton_archer.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_skeleton_archer.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_skeleton.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_skeleton.png" "$DEST/IdleRPG/Art/Placeholder/enemy_skeleton.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_skeleton.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_skeleton.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_skeleton.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_priest.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_priest.png" "$DEST/IdleRPG/Art/Placeholder/enemy_priest.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_priest.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_priest.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_priest.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_orc_rider.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_orc_rider.png" "$DEST/IdleRPG/Art/Placeholder/enemy_orc_rider.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_orc_rider.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_orc_rider.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_orc_rider.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_orc.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_orc.png" "$DEST/IdleRPG/Art/Placeholder/enemy_orc.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_orc.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_orc.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_orc.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_lava_slime.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_lava_slime.png" "$DEST/IdleRPG/Art/Placeholder/enemy_lava_slime.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_lava_slime.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_lava_slime.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_lava_slime.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_lancer.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_lancer.png" "$DEST/IdleRPG/Art/Placeholder/enemy_lancer.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_lancer.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_lancer.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_lancer.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_hellhound.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_hellhound.png" "$DEST/IdleRPG/Art/Placeholder/enemy_hellhound.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_hellhound.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_hellhound.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_hellhound.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_hellbat.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_hellbat.png" "$DEST/IdleRPG/Art/Placeholder/enemy_hellbat.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_hellbat.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_hellbat.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_hellbat.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_greatsword_skeleton.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_greatsword_skeleton.png" "$DEST/IdleRPG/Art/Placeholder/enemy_greatsword_skeleton.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_greatsword_skeleton.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_greatsword_skeleton.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_greatsword_skeleton.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_ghostfire.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_ghostfire.png" "$DEST/IdleRPG/Art/Placeholder/enemy_ghostfire.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_ghostfire.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_ghostfire.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_ghostfire.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_flame_golem.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_flame_golem.png" "$DEST/IdleRPG/Art/Placeholder/enemy_flame_golem.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_flame_golem.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_flame_golem.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_flame_golem.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_eyeball_monster.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_eyeball_monster.png" "$DEST/IdleRPG/Art/Placeholder/enemy_eyeball_monster.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_eyeball_monster.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_eyeball_monster.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_eyeball_monster.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_werewolf.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_werewolf.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_werewolf.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_werewolf.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_werewolf.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_werewolf.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_slime.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_slime.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_slime.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_slime.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_slime.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_slime.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_skeleton_champion.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_skeleton_champion.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_skeleton_champion.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_skeleton_champion.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_skeleton_champion.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_skeleton_champion.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_skeleton.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_skeleton.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_skeleton.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_skeleton.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_skeleton.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_skeleton.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_orc_rider.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_orc_rider.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_orc_rider.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_orc_rider.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_orc_rider.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_orc_rider.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_hellhound.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_hellhound.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_hellhound.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_hellhound.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_hellhound.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_hellhound.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_hellbat.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_hellbat.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_hellbat.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_hellbat.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_hellbat.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_hellbat.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_greatsword.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_greatsword.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_greatsword.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_greatsword.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_greatsword.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_greatsword.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_eyeball.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_eyeball.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_eyeball.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_eyeball.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_eyeball.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_eyeball.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_demon_d.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_demon_d.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_demon_d.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_demon_d.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_demon_d.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_demon_d.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_demon_b.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_demon_b.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_demon_b.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_demon_b.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_demon_b.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_demon_b.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_blood_monster_b.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_blood_monster_b.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_blood_monster_b.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_blood_monster_b.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_blood_monster_b.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_blood_monster_b.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_blood_monster.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_blood_monster.png" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_blood_monster.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_elite_blood_monster.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_elite_blood_monster.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_elite_blood_monster.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demoness_b.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demoness_b.png" "$DEST/IdleRPG/Art/Placeholder/enemy_demoness_b.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demoness_b.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demoness_b.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_demoness_b.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demoness_a.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demoness_a.png" "$DEST/IdleRPG/Art/Placeholder/enemy_demoness_a.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demoness_a.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demoness_a.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_demoness_a.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demon_e.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demon_e.png" "$DEST/IdleRPG/Art/Placeholder/enemy_demon_e.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demon_e.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demon_e.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_demon_e.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demon_d.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demon_d.png" "$DEST/IdleRPG/Art/Placeholder/enemy_demon_d.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demon_d.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demon_d.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_demon_d.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demon_c.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demon_c.png" "$DEST/IdleRPG/Art/Placeholder/enemy_demon_c.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demon_c.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demon_c.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_demon_c.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demon_b.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demon_b.png" "$DEST/IdleRPG/Art/Placeholder/enemy_demon_b.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demon_b.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demon_b.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_demon_b.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demon_a.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demon_a.png" "$DEST/IdleRPG/Art/Placeholder/enemy_demon_a.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_demon_a.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_demon_a.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_demon_a.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_blood_monster_b.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_blood_monster_b.png" "$DEST/IdleRPG/Art/Placeholder/enemy_blood_monster_b.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_blood_monster_b.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_blood_monster_b.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_blood_monster_b.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_blood_monster_a.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_blood_monster_a.png" "$DEST/IdleRPG/Art/Placeholder/enemy_blood_monster_a.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_blood_monster_a.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_blood_monster_a.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_blood_monster_a.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_black_knight_a.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_black_knight_a.png" "$DEST/IdleRPG/Art/Placeholder/enemy_black_knight_a.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_black_knight_a.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_black_knight_a.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_black_knight_a.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_armored_skeleton.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_armored_skeleton.png" "$DEST/IdleRPG/Art/Placeholder/enemy_armored_skeleton.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_armored_skeleton.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_armored_skeleton.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_armored_skeleton.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_armored_orc.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_armored_orc.png" "$DEST/IdleRPG/Art/Placeholder/enemy_armored_orc.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_armored_orc.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_armored_orc.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_armored_orc.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_armored_axeman.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_armored_axeman.png" "$DEST/IdleRPG/Art/Placeholder/enemy_armored_axeman.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_armored_axeman.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_armored_axeman.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_armored_axeman.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_archer.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_archer.png" "$DEST/IdleRPG/Art/Placeholder/enemy_archer.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/enemy_archer.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/enemy_archer.png.meta" "$DEST/IdleRPG/Art/Placeholder/enemy_archer.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_wolf_alpha.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_wolf_alpha.png" "$DEST/IdleRPG/Art/Placeholder/boss_wolf_alpha.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_wolf_alpha.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_wolf_alpha.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_wolf_alpha.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_werebear_titan.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_werebear_titan.png" "$DEST/IdleRPG/Art/Placeholder/boss_werebear_titan.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_werebear_titan.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_werebear_titan.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_werebear_titan.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_warlock_archmage.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_warlock_archmage.png" "$DEST/IdleRPG/Art/Placeholder/boss_warlock_archmage.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_warlock_archmage.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_warlock_archmage.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_warlock_archmage.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_warlock_adept.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_warlock_adept.png" "$DEST/IdleRPG/Art/Placeholder/boss_warlock_adept.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_warlock_adept.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_warlock_adept.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_warlock_adept.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_warlock.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_warlock.png" "$DEST/IdleRPG/Art/Placeholder/boss_warlock.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_warlock.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_warlock.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_warlock.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_swordsman_champion.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_swordsman_champion.png" "$DEST/IdleRPG/Art/Placeholder/boss_swordsman_champion.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_swordsman_champion.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_swordsman_champion.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_swordsman_champion.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_slime_monarch.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_slime_monarch.png" "$DEST/IdleRPG/Art/Placeholder/boss_slime_monarch.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_slime_monarch.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_slime_monarch.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_slime_monarch.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_slime_king.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_slime_king.png" "$DEST/IdleRPG/Art/Placeholder/boss_slime_king.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_slime_king.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_slime_king.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_slime_king.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_skeleton_marksman.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_skeleton_marksman.png" "$DEST/IdleRPG/Art/Placeholder/boss_skeleton_marksman.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_skeleton_marksman.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_skeleton_marksman.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_skeleton_marksman.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_priest_zealot.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_priest_zealot.png" "$DEST/IdleRPG/Art/Placeholder/boss_priest_zealot.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_priest_zealot.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_priest_zealot.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_priest_zealot.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_orc_skirmisher.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_orc_skirmisher.png" "$DEST/IdleRPG/Art/Placeholder/boss_orc_skirmisher.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_orc_skirmisher.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_orc_skirmisher.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_orc_skirmisher.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_orc_rider_milestone.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_orc_rider_milestone.png" "$DEST/IdleRPG/Art/Placeholder/boss_orc_rider_milestone.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_orc_rider_milestone.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_orc_rider_milestone.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_orc_rider_milestone.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_orc_raider.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_orc_raider.png" "$DEST/IdleRPG/Art/Placeholder/boss_orc_raider.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_orc_raider.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_orc_raider.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_orc_raider.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_orc_chieftain.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_orc_chieftain.png" "$DEST/IdleRPG/Art/Placeholder/boss_orc_chieftain.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_orc_chieftain.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_orc_chieftain.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_orc_chieftain.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_ogre.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_ogre.png" "$DEST/IdleRPG/Art/Placeholder/boss_ogre.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_ogre.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_ogre.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_ogre.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_necromancer.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_necromancer.png" "$DEST/IdleRPG/Art/Placeholder/boss_necromancer.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_necromancer.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_necromancer.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_necromancer.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_minotaur.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_minotaur.png" "$DEST/IdleRPG/Art/Placeholder/boss_minotaur.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_minotaur.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_minotaur.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_minotaur.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_lava_behemoth.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_lava_behemoth.png" "$DEST/IdleRPG/Art/Placeholder/boss_lava_behemoth.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_lava_behemoth.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_lava_behemoth.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_lava_behemoth.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_lancer_vanguard.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_lancer_vanguard.png" "$DEST/IdleRPG/Art/Placeholder/boss_lancer_vanguard.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_lancer_vanguard.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_lancer_vanguard.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_lancer_vanguard.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_hellhound_alpha.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_hellhound_alpha.png" "$DEST/IdleRPG/Art/Placeholder/boss_hellhound_alpha.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_hellhound_alpha.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_hellhound_alpha.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_hellhound_alpha.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_hellbat_swarmlord.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_hellbat_swarmlord.png" "$DEST/IdleRPG/Art/Placeholder/boss_hellbat_swarmlord.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_hellbat_swarmlord.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_hellbat_swarmlord.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_hellbat_swarmlord.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_hellbat_ravager.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_hellbat_ravager.png" "$DEST/IdleRPG/Art/Placeholder/boss_hellbat_ravager.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_hellbat_ravager.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_hellbat_ravager.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_hellbat_ravager.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_greatsword_warden.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_greatsword_warden.png" "$DEST/IdleRPG/Art/Placeholder/boss_greatsword_warden.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_greatsword_warden.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_greatsword_warden.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_greatsword_warden.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_grand_templar.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_grand_templar.png" "$DEST/IdleRPG/Art/Placeholder/boss_grand_templar.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_grand_templar.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_grand_templar.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_grand_templar.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_ghostfire_wraith.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_ghostfire_wraith.png" "$DEST/IdleRPG/Art/Placeholder/boss_ghostfire_wraith.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_ghostfire_wraith.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_ghostfire_wraith.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_ghostfire_wraith.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_flame_warden.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_flame_warden.png" "$DEST/IdleRPG/Art/Placeholder/boss_flame_warden.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_flame_warden.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_flame_warden.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_flame_warden.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_flame_golem.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_flame_golem.png" "$DEST/IdleRPG/Art/Placeholder/boss_flame_golem.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_flame_golem.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_flame_golem.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_flame_golem.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_flame_colossus.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_flame_colossus.png" "$DEST/IdleRPG/Art/Placeholder/boss_flame_colossus.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_flame_colossus.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_flame_colossus.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_flame_colossus.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_eyeball_watcher.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_eyeball_watcher.png" "$DEST/IdleRPG/Art/Placeholder/boss_eyeball_watcher.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_eyeball_watcher.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_eyeball_watcher.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_eyeball_watcher.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_eyeball_beholder.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_eyeball_beholder.png" "$DEST/IdleRPG/Art/Placeholder/boss_eyeball_beholder.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_eyeball_beholder.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_eyeball_beholder.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_eyeball_beholder.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_warlock.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_warlock.png" "$DEST/IdleRPG/Art/Placeholder/boss_elite_warlock.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_warlock.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_warlock.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_elite_warlock.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_orc_milestone.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_orc_milestone.png" "$DEST/IdleRPG/Art/Placeholder/boss_elite_orc_milestone.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_orc_milestone.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_orc_milestone.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_elite_orc_milestone.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_minotaur.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_minotaur.png" "$DEST/IdleRPG/Art/Placeholder/boss_elite_minotaur.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_minotaur.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_minotaur.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_elite_minotaur.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_flame_golem.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_flame_golem.png" "$DEST/IdleRPG/Art/Placeholder/boss_elite_flame_golem.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_flame_golem.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_flame_golem.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_elite_flame_golem.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_demoness.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_demoness.png" "$DEST/IdleRPG/Art/Placeholder/boss_elite_demoness.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_demoness.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_demoness.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_elite_demoness.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_demon.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_demon.png" "$DEST/IdleRPG/Art/Placeholder/boss_elite_demon.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_demon.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_demon.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_elite_demon.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_black_knight.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_black_knight.png" "$DEST/IdleRPG/Art/Placeholder/boss_elite_black_knight.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_elite_black_knight.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_elite_black_knight.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_elite_black_knight.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_dire_werewolf.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_dire_werewolf.png" "$DEST/IdleRPG/Art/Placeholder/boss_dire_werewolf.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_dire_werewolf.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_dire_werewolf.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_dire_werewolf.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demoness_sorceress.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demoness_sorceress.png" "$DEST/IdleRPG/Art/Placeholder/boss_demoness_sorceress.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demoness_sorceress.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demoness_sorceress.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_demoness_sorceress.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demoness_blade.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demoness_blade.png" "$DEST/IdleRPG/Art/Placeholder/boss_demoness_blade.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demoness_blade.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demoness_blade.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_demoness_blade.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demoness_a_milestone.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demoness_a_milestone.png" "$DEST/IdleRPG/Art/Placeholder/boss_demoness_a_milestone.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demoness_a_milestone.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demoness_a_milestone.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_demoness_a_milestone.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demon_ravager.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demon_ravager.png" "$DEST/IdleRPG/Art/Placeholder/boss_demon_ravager.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demon_ravager.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demon_ravager.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_demon_ravager.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demon_prince.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demon_prince.png" "$DEST/IdleRPG/Art/Placeholder/boss_demon_prince.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demon_prince.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demon_prince.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_demon_prince.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demon_overlord.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demon_overlord.png" "$DEST/IdleRPG/Art/Placeholder/boss_demon_overlord.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demon_overlord.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demon_overlord.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_demon_overlord.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demon_captain.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demon_captain.png" "$DEST/IdleRPG/Art/Placeholder/boss_demon_captain.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demon_captain.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demon_captain.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_demon_captain.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demon_brute.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demon_brute.png" "$DEST/IdleRPG/Art/Placeholder/boss_demon_brute.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_demon_brute.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_demon_brute.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_demon_brute.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_bone_captain.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_bone_captain.png" "$DEST/IdleRPG/Art/Placeholder/boss_bone_captain.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_bone_captain.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_bone_captain.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_bone_captain.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_blood_horror.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_blood_horror.png" "$DEST/IdleRPG/Art/Placeholder/boss_blood_horror.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_blood_horror.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_blood_horror.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_blood_horror.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_blood_elder.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_blood_elder.png" "$DEST/IdleRPG/Art/Placeholder/boss_blood_elder.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_blood_elder.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_blood_elder.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_blood_elder.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_blood_abomination.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_blood_abomination.png" "$DEST/IdleRPG/Art/Placeholder/boss_blood_abomination.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_blood_abomination.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_blood_abomination.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_blood_abomination.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_black_knight_sovereign.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_black_knight_sovereign.png" "$DEST/IdleRPG/Art/Placeholder/boss_black_knight_sovereign.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_black_knight_sovereign.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_black_knight_sovereign.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_black_knight_sovereign.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_black_knight_champion.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_black_knight_champion.png" "$DEST/IdleRPG/Art/Placeholder/boss_black_knight_champion.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_black_knight_champion.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_black_knight_champion.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_black_knight_champion.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_black_knight_c.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_black_knight_c.png" "$DEST/IdleRPG/Art/Placeholder/boss_black_knight_c.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_black_knight_c.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_black_knight_c.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_black_knight_c.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_black_knight_b.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_black_knight_b.png" "$DEST/IdleRPG/Art/Placeholder/boss_black_knight_b.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_black_knight_b.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_black_knight_b.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_black_knight_b.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_black_knight_a.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_black_knight_a.png" "$DEST/IdleRPG/Art/Placeholder/boss_black_knight_a.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_black_knight_a.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_black_knight_a.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_black_knight_a.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_bat_swarmlord.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_bat_swarmlord.png" "$DEST/IdleRPG/Art/Placeholder/boss_bat_swarmlord.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_bat_swarmlord.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_bat_swarmlord.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_bat_swarmlord.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_axeman_brute.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_axeman_brute.png" "$DEST/IdleRPG/Art/Placeholder/boss_axeman_brute.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_axeman_brute.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_axeman_brute.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_axeman_brute.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_archfiend.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_archfiend.png" "$DEST/IdleRPG/Art/Placeholder/boss_archfiend.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_archfiend.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_archfiend.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_archfiend.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_archer_captain.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_archer_captain.png" "$DEST/IdleRPG/Art/Placeholder/boss_archer_captain.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/boss_archer_captain.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/boss_archer_captain.png.meta" "$DEST/IdleRPG/Art/Placeholder/boss_archer_captain.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/torch.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/torch.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/torch.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/torch.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/torch.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/torch.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/staff_wizard.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/staff_wizard.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/staff_wizard.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/staff_wizard.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/staff_wizard.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/staff_wizard.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/spell_fireball.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/spell_fireball.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/spell_fireball.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/spell_fireball.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/spell_fireball.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/spell_fireball.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/skill_slash.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/skill_slash.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/skill_slash.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/skill_slash.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/skill_slash.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/skill_slash.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/potion_poison.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/potion_poison.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/potion_poison.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/potion_poison.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/potion_poison.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/potion_poison.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/potion_mana.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/potion_mana.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/potion_mana.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/potion_mana.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/potion_mana.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/potion_mana.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/key_brass.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/key_brass.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/key_brass.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/key_brass.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/key_brass.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/key_brass.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/helmet_iron.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/helmet_iron.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/helmet_iron.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/helmet_iron.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/helmet_iron.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/helmet_iron.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/gem_ruby.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/gem_ruby.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/gem_ruby.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/gem_ruby.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/gem_ruby.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/gem_ruby.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/dagger.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/dagger.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/dagger.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/dagger.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/dagger.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/dagger.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/bow_wood.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/bow_wood.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/bow_wood.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/bow_wood.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/bow_wood.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/bow_wood.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/axe_battle.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/axe_battle.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/axe_battle.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/axe_battle.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/axe_battle.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/axe_battle.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/armor_leather.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/armor_leather.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/armor_leather.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/armor_leather.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/armor_leather.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/armor_leather.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128"
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/ale_mug.png" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/ale_mug.png" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/ale_mug.png"; fi
if [ -f "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/ale_mug.png.meta" ]; then mv "Assets/IdleRPG/Art/FantasyRPGIcons_Free24/128/ale_mug.png.meta" "$DEST/IdleRPG/Art/FantasyRPGIcons_Free24/128/ale_mug.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_bar_mana.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_bar_mana.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_bar_mana.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_bar_mana.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_bar_mana.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_bar_mana.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_bar_health.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_bar_health.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_bar_health.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_bar_health.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_bar_health.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_bar_health.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Resources"
if [ -f "Assets/IdleRPG/Resources/ui_frame.png" ]; then mv "Assets/IdleRPG/Resources/ui_frame.png" "$DEST/IdleRPG/Resources/ui_frame.png"; fi
if [ -f "Assets/IdleRPG/Resources/ui_frame.png.meta" ]; then mv "Assets/IdleRPG/Resources/ui_frame.png.meta" "$DEST/IdleRPG/Resources/ui_frame.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_slot.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_slot.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_slot.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_slot.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_slot.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_slot.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_sword_short.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_sword_short.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_sword_short.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_sword_short.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_sword_short.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_sword_short.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_staff.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_staff.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_staff.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_staff.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_staff.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_staff.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_shield_wood.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_shield_wood.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_shield_wood.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_shield_wood.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_shield_wood.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_shield_wood.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_potion_red.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_potion_red.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_potion_red.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_potion_red.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_potion_red.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_potion_red.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_potion_blue.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_potion_blue.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_potion_blue.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_potion_blue.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_potion_blue.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_potion_blue.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_key.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_key.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_key.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_key.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_key.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_key.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_helmet_iron.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_helmet_iron.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_helmet_iron.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_helmet_iron.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_helmet_iron.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_helmet_iron.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_gem_ruby.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_gem_ruby.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_gem_ruby.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_gem_ruby.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_gem_ruby.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_gem_ruby.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_coin.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_coin.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_coin.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_coin.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_coin.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_coin.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_chest.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_chest.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_chest.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_chest.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_chest.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_chest.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_bow.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_bow.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_bow.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_bow.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_bow.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_bow.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_axe.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_axe.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_axe.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_axe.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_axe.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxi_axe.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_counter_coin.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_counter_coin.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_counter_coin.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_counter_coin.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_counter_coin.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_counter_coin.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_btn_wood_pressed.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_btn_wood_pressed.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_btn_wood_pressed.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_btn_wood_pressed.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_btn_wood_pressed.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_btn_wood_pressed.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_btn_wood.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_btn_wood.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_btn_wood.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_btn_wood.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_btn_wood.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_btn_wood.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_half.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_half.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_half.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_half.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_half.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_half.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_full.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_full.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_full.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_full.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_full.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_full.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_empty.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_empty.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_empty.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_empty.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_empty.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/4x/px_heart_empty.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_panel_wood.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_panel_wood.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_panel_wood.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_panel_wood.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_panel_wood.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_panel_wood.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_stun.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_stun.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_stun.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_stun.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_stun.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_stun.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_shield.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_shield.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_shield.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_shield.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_shield.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_shield.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_regen.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_regen.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_regen.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_regen.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_regen.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_regen.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_poison.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_poison.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_poison.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_poison.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_poison.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_poison.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_freeze.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_freeze.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_freeze.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_freeze.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_freeze.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_freeze.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_burn.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_burn.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_burn.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_burn.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_burn.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxst_burn.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_teleport.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_teleport.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_teleport.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_teleport.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_teleport.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_teleport.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_lightning.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_lightning.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_lightning.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_lightning.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_lightning.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_lightning.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_frost_bolt.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_frost_bolt.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_frost_bolt.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_frost_bolt.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_frost_bolt.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_frost_bolt.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_fireball.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_fireball.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_fireball.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_fireball.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_fireball.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_wiz_fireball.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_war_cry.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_war_cry.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_war_cry.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_war_cry.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_war_cry.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_war_cry.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_slash.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_slash.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_slash.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_slash.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_slash.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_slash.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_shield_bash.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_shield_bash.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_shield_bash.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_shield_bash.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_shield_bash.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_shield_bash.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_berserk.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_berserk.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_berserk.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_berserk.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_berserk.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_war_berserk.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_stealth.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_stealth.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_stealth.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_stealth.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_stealth.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_stealth.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_smoke_bomb.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_smoke_bomb.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_smoke_bomb.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_smoke_bomb.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_smoke_bomb.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_smoke_bomb.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_poison_blade.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_poison_blade.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_poison_blade.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_poison_blade.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_poison_blade.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_poison_blade.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_backstab.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_backstab.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_backstab.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_backstab.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_backstab.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rog_backstab.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_pet_wolf.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_pet_wolf.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_pet_wolf.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_pet_wolf.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_pet_wolf.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_pet_wolf.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_multi_shot.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_multi_shot.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_multi_shot.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_multi_shot.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_multi_shot.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_multi_shot.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_fire_arrow.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_fire_arrow.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_fire_arrow.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_fire_arrow.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_fire_arrow.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_fire_arrow.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_aimed_shot.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_aimed_shot.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_aimed_shot.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_aimed_shot.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_aimed_shot.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_rng_aimed_shot.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_summon_skeleton.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_summon_skeleton.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_summon_skeleton.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_summon_skeleton.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_summon_skeleton.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_summon_skeleton.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_raise_dead.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_raise_dead.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_raise_dead.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_raise_dead.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_raise_dead.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_raise_dead.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_life_drain.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_life_drain.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_life_drain.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_life_drain.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_life_drain.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_life_drain.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_curse.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_curse.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_curse.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_curse.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_curse.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_nec_curse.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_resurrect.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_resurrect.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_resurrect.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_resurrect.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_resurrect.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_resurrect.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_holy_light.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_holy_light.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_holy_light.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_holy_light.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_holy_light.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_holy_light.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_heal.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_heal.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_heal.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_heal.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_heal.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_heal.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_divine_shield.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_divine_shield.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_divine_shield.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_divine_shield.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_divine_shield.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/pxs_clr_divine_shield.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_tab_on.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_tab_on.png" "$DEST/IdleRPG/Art/Placeholder/ui_tab_on.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_tab_on.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_tab_on.png.meta" "$DEST/IdleRPG/Art/Placeholder/ui_tab_on.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_tab_off.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_tab_off.png" "$DEST/IdleRPG/Art/Placeholder/ui_tab_off.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_tab_off.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_tab_off.png.meta" "$DEST/IdleRPG/Art/Placeholder/ui_tab_off.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_icon_shard.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_icon_shard.png" "$DEST/IdleRPG/Art/Placeholder/ui_icon_shard.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_icon_shard.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_icon_shard.png.meta" "$DEST/IdleRPG/Art/Placeholder/ui_icon_shard.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_icon_scroll.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_icon_scroll.png" "$DEST/IdleRPG/Art/Placeholder/ui_icon_scroll.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_icon_scroll.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_icon_scroll.png.meta" "$DEST/IdleRPG/Art/Placeholder/ui_icon_scroll.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_icon_material.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_icon_material.png" "$DEST/IdleRPG/Art/Placeholder/ui_icon_material.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_icon_material.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_icon_material.png.meta" "$DEST/IdleRPG/Art/Placeholder/ui_icon_material.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_icon_essence.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_icon_essence.png" "$DEST/IdleRPG/Art/Placeholder/ui_icon_essence.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_icon_essence.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_icon_essence.png.meta" "$DEST/IdleRPG/Art/Placeholder/ui_icon_essence.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_icon_ad.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_icon_ad.png" "$DEST/IdleRPG/Art/Placeholder/ui_icon_ad.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_icon_ad.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_icon_ad.png.meta" "$DEST/IdleRPG/Art/Placeholder/ui_icon_ad.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_glow_soft.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_glow_soft.png" "$DEST/IdleRPG/Art/Placeholder/ui_glow_soft.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_glow_soft.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_glow_soft.png.meta" "$DEST/IdleRPG/Art/Placeholder/ui_glow_soft.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/Placeholder"
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_glow_burst.png" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_glow_burst.png" "$DEST/IdleRPG/Art/Placeholder/ui_glow_burst.png"; fi
if [ -f "Assets/IdleRPG/Art/Placeholder/ui_glow_burst.png.meta" ]; then mv "Assets/IdleRPG/Art/Placeholder/ui_glow_burst.png.meta" "$DEST/IdleRPG/Art/Placeholder/ui_glow_burst.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_bar_mana.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_bar_mana.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_bar_mana.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_bar_mana.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_bar_mana.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_bar_mana.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_bar_health.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_bar_health.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_bar_health.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_bar_health.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_bar_health.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_bar_health.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_slot.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_slot.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_slot.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_slot.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_slot.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_slot.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_potion_blue.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_potion_blue.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_potion_blue.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_potion_blue.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_potion_blue.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_potion_blue.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_helmet_iron.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_helmet_iron.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_helmet_iron.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_helmet_iron.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_helmet_iron.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_helmet_iron.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_bow.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_bow.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_bow.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_bow.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_bow.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxi_bow.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_counter_coin.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_counter_coin.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_counter_coin.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_counter_coin.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_counter_coin.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_counter_coin.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_btn_wood_pressed.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_btn_wood_pressed.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_btn_wood_pressed.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_btn_wood_pressed.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_btn_wood_pressed.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_btn_wood_pressed.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_btn_wood.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_btn_wood.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_btn_wood.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_btn_wood.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_btn_wood.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_btn_wood.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_half.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_half.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_half.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_half.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_half.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_half.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_full.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_full.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_full.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_full.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_full.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_full.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_empty.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_empty.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_empty.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_empty.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_empty.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/2x/px_heart_empty.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_panel_wood.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_panel_wood.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_panel_wood.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_panel_wood.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_panel_wood.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_panel_wood.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_stun.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_stun.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_stun.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_stun.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_stun.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_stun.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_regen.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_regen.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_regen.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_regen.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_regen.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_regen.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_freeze.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_freeze.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_freeze.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_freeze.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_freeze.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_freeze.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_burn.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_burn.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_burn.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_burn.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_burn.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxst_burn.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_wiz_teleport.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_wiz_teleport.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_wiz_teleport.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_wiz_teleport.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_wiz_teleport.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_wiz_teleport.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_stealth.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_stealth.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_stealth.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_stealth.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_stealth.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_stealth.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_smoke_bomb.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_smoke_bomb.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_smoke_bomb.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_smoke_bomb.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_smoke_bomb.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_smoke_bomb.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_backstab.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_backstab.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_backstab.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_backstab.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_backstab.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rog_backstab.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rng_pet_wolf.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rng_pet_wolf.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rng_pet_wolf.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rng_pet_wolf.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rng_pet_wolf.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_rng_pet_wolf.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_summon_skeleton.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_summon_skeleton.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_summon_skeleton.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_summon_skeleton.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_summon_skeleton.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_summon_skeleton.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_raise_dead.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_raise_dead.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_raise_dead.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_raise_dead.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_raise_dead.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_raise_dead.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_curse.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_curse.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_curse.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_curse.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_curse.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_nec_curse.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_resurrect.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_resurrect.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_resurrect.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_resurrect.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_resurrect.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_resurrect.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_holy_light.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_holy_light.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_holy_light.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_holy_light.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_holy_light.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_holy_light.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_heal.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_heal.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_heal.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_heal.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_heal.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_heal.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_divine_shield.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_divine_shield.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_divine_shield.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_divine_shield.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_divine_shield.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/pxs_clr_divine_shield.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_bar_mana.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_bar_mana.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_bar_mana.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_bar_mana.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_bar_mana.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_bar_mana.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_bar_health.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_bar_health.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_bar_health.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_bar_health.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_bar_health.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_bar_health.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_slot.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_slot.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_slot.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_slot.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_slot.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_slot.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_counter_coin.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_counter_coin.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_counter_coin.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_counter_coin.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_counter_coin.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_counter_coin.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_btn_wood_pressed.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_btn_wood_pressed.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_btn_wood_pressed.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_btn_wood_pressed.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_btn_wood_pressed.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_btn_wood_pressed.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_btn_wood.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_btn_wood.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_btn_wood.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_btn_wood.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_btn_wood.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_btn_wood.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_half.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_half.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_half.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_half.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_half.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_half.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_full.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_full.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_full.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_full.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_full.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_full.png.meta"; fi
mkdir -p "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x"
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_empty.png" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_empty.png" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_empty.png"; fi
if [ -f "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_empty.png.meta" ]; then mv "Assets/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_empty.png.meta" "$DEST/IdleRPG/Art/PixelFantasy_FreeStarter/1x/px_heart_empty.png.meta"; fi

echo "Moved. Open Unity, let it reimport, then run Tools > Idle RPG > Art > Validate Art Pipeline."
