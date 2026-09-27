using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using IdleRPG.Combat;
using IdleRPG.Data;
using IdleRPG.Progression;
using IdleRPG.Services;

namespace IdleRPG.EditorTools.Content
{
    /// <summary>
    /// Card pictures: every card must have a drawing (B8' tool 2).
    /// </summary>
    public static partial class ContentValidator
    {

        // ------------------------------------------------------------------
        // Card pictures (B8' tool 2)
        // ------------------------------------------------------------------
        /// <summary>
        /// Every hero and monster card must have a picture, and the picture must be imported as a sprite - otherwise
        /// the fight shows an empty space and nothing else complains. The file name and the family word are read
        /// exactly like the drawing tool reads them, so the check and the tool can never disagree.
        /// </summary>
        private static void CheckCardPictures(HeroSpecFile heroFile, EnemySpecFile enemyFile)
        {
            int checkedCards = 0;
            int missing = 0;

            if (heroFile != null)
            {
                for (int i = 0; i < heroFile.heroes.Count; i++)
                {
                    HeroSpec hero = heroFile.heroes[i];
                    checkedCards++;
                    missing += CheckOneCardPicture("heroes", hero.id, hero.icon, false);
                }
            }

            if (enemyFile != null)
            {
                for (int i = 0; i < enemyFile.enemies.Count; i++)
                {
                    EnemySpec enemy = enemyFile.enemies[i];
                    checkedCards++;
                    missing += CheckOneCardPicture("enemies", enemy.id, enemy.sprite, enemy.isBoss);
                }
            }

            Add(Severity.Info, "pictures", $"{checkedCards} card picture(s) checked, {missing} missing.");
        }

        /// <summary>
        /// Checks one card's picture. Returns 1 when the picture file is missing. Empty ids are skipped here because
        /// they are already reported by the per-area checks.
        /// </summary>
        private static int CheckOneCardPicture(string area, string cardId, string pictureName, bool isBoss)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                return 0;
            }

            string name = string.IsNullOrEmpty(pictureName) ? cardId : pictureName;
            string path = PlaceholderSpriteGenerator.ArtFolder + "/" + name + ".png";

            if (!File.Exists(path))
            {
                Add(Severity.Error, area,
                    $"{cardId}: no picture at {path}. Fix: point the card's picture field at an existing file, or " +
                    $"press Tools > Idle RPG > Art > Generate Placeholder Sprites to draw one.");
                return 1;
            }

            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
            {
                Add(Severity.Error, area,
                    $"{cardId}: {path} exists but is not imported as a sprite, so the fight will show an empty space.");
            }

            if (!isBoss && PlaceholderSpriteGenerator.FamilyOf(cardId, false) == "unknown")
            {
                Add(Severity.Warning, area,
                    $"{cardId}: the id carries no family word, so its drawn picture falls back to a plain blob. " +
                    $"Put a family word in the id (slime / bat / goblin / knight / mage / archer / gold ...).");
            }

            return 0;
        }
    }
}
