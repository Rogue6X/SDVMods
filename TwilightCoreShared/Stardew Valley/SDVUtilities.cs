﻿using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using System;
using System.Linq;
using System.Collections.Generic;
using TwilightShards.Common;
using xTile.Dimensions;
using SObject = StardewValley.Object;

namespace TwilightShards.Stardew.Common
{
    public static class SDVUtilities
    {
        public static bool TileIsClearForSpawning(GameLocation checkLoc, Vector2 tileVector,  StardewValley.Object tile)
        {
            if (tile == null && checkLoc.doesTileHaveProperty((int)tileVector.X, (int)tileVector.Y, "Diggable", "Back") != null && (checkLoc.isTileLocationOpen(new Location((int)tileVector.X * Game1.tileSize, (int)tileVector.Y * Game1.tileSize)) && !checkLoc.isTileOccupied(tileVector, "")) && checkLoc.doesTileHaveProperty((int)tileVector.X, (int)tileVector.Y, "Water", "Back") == null)
            {
                string PropCheck = checkLoc.doesTileHaveProperty((int)tileVector.X, (int)tileVector.Y, "NoSpawn", "Back");

                if (PropCheck == null || !PropCheck.Equals("Grass") && !PropCheck.Equals("All") && !PropCheck.Equals("True"))
                {
                    return true;
                }
            }
            return false;
        }

        public static void ShakeScreenOnLowStamina()
        {
            Game1.staminaShakeTimer = 1000;
            for (int i = 0; i < 4; i++)
            {
                Game1.screenOverlayTempSprites.Add(new TemporaryAnimatedSprite(Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(366, 412, 5, 6), new Vector2((Game1.random.Next(Game1.tileSize / 2) + Game1.viewport.Width - (48 + Game1.tileSize / 8)), (Game1.viewport.Height - 224 - Game1.tileSize / 4 - (int)((Game1.player.MaxStamina - 270) * 0.715))), false, 0.012f, Color.SkyBlue)
                {
                    motion = new Vector2(-2f, -10f),
                    acceleration = new Vector2(0f, 0.5f),
                    local = true,
                    scale = (Game1.pixelZoom + Game1.random.Next(-1, 0)),
                    delayBeforeAnimationStart = i * 30
                });
            }
        }

        public static string GetFestivalName() => GetFestivalName(Game1.dayOfMonth, Game1.currentSeason);
        public static string GetTomorrowFestivalName() => GetFestivalName(Game1.dayOfMonth + 1, Game1.currentSeason);
        
        public static string PrintStringArray(string[] array)
        {
            string s = "";
            for (int i = 0; i < array.Length; i++)
            {
                s = s + $"Command {i} is {array[i]}";
            }

            return s;
        }

        public static string PrintCurrentWeatherStatus()
        {
            return $"Printing current weather status:" +
                    $"It is Raining: {Game1.isRaining} {Environment.NewLine}" +
                    $"It is Stormy: {Game1.isLightning} {Environment.NewLine}" +
                    $"It is Snowy: {Game1.isSnowing} {Environment.NewLine}" +
                    $"It is Debris Weather: {Game1.isDebrisWeather} {Environment.NewLine}";
        }

        internal static string GetFestivalName(SDate date) => SDVUtilities.GetFestivalName(date.Day, date.Season);

        private static string GetFestivalName(int dayOfMonth, string currentSeason)
        {
            switch (currentSeason)
            {
                case ("spring"):
                    if (dayOfMonth == 13) return "Egg Festival";
                    if (dayOfMonth == 24) return "Flower Dance";
                    break;
                case ("winter"):
                    if (dayOfMonth == 8) return "Festival of Ice";
                    if (dayOfMonth == 25) return "Feast of the Winter Star";
                    break;
                case ("fall"):
                    if (dayOfMonth == 16) return "Stardew Valley Fair";
                    if (dayOfMonth == 27) return "Spirit's Eve";
                    break;
                case ("summer"):
                    if (dayOfMonth == 11) return "Luau";
                    if (dayOfMonth == 28) return "Dance of the Moonlight Jellies";
                    break;
                default:
                    return $"";
            }

            return $"";

        }

        public static void ShowMessage(string msg)
        {
            var hudmsg = new HUDMessage(msg, Color.SeaGreen, 5250f, true)
            {
                whatType = 2
            };
            Game1.addHUDMessage(hudmsg);
        }

        public static void FaintPlayer()
        {
            Game1.player.Stamina = 0;
            Game1.player.doEmote(36);
            Game1.farmerShouldPassOut = true;
        }

        public static int CropCountInFarm(Farm f)
        {
            return f.terrainFeatures.Values.Where(c => c is HoeDirt curr && curr.crop != null).Count();
        }

        public static void SpawnGhostOffScreen(MersenneTwister Dice)
        {
            Vector2 zero = Vector2.Zero;
            if (Game1.getFarm() is Farm ourFarm)
            {
                switch (Game1.random.Next(4))
                {
                    case 0:
                        zero.X = (float)Dice.Next(ourFarm.map.Layers[0].LayerWidth);
                        break;
                    case 1:
                        zero.X = (float)(ourFarm.map.Layers[0].LayerWidth - 1);
                        zero.Y = (float)Dice.Next(ourFarm.map.Layers[0].LayerHeight);
                        break;
                    case 2:
                        zero.Y = (float)(ourFarm.map.Layers[0].LayerHeight - 1);
                        zero.X = (float)Dice.Next(ourFarm.map.Layers[0].LayerWidth);
                        break;
                    case 3:
                        zero.Y = (float)Game1.random.Next(ourFarm.map.Layers[0].LayerHeight);
                        break;
                }

                if (Utility.isOnScreen(zero * (float)Game1.tileSize, Game1.tileSize))
                    zero.X -= (float)Game1.viewport.Width;

                List<NPC> characters = ourFarm.characters;
                Ghost ghost = new Ghost(zero * Game1.tileSize)
                {
                    focusedOnFarmers = true,
                    wildernessFarmMonster = true
                };
                ghost.reloadSprite();
                characters.Add(ghost);
            }
        }

        public static double GetDistance(Vector2 alpha, Vector2 beta)
        {
            return Math.Sqrt(Math.Pow(beta.X - alpha.X, 2) + Math.Pow(beta.Y - alpha.Y, 2));
        }

        public static void SpawnMonster(GameLocation location)
        {
            Vector2 zero = Vector2.Zero;
            Vector2 randomTile = Vector2.Zero;
            int numTries = 0;
            do
            {
                randomTile = location.getRandomTile();
                numTries++;
            } while (GetDistance(randomTile, Game1.player.position) > 45 && numTries < 10000);            

            if (Utility.isOnScreen(Utility.Vector2ToPoint(randomTile), Game1.tileSize, location))
                randomTile.X -= (Game1.viewport.Width / Game1.tileSize);

            if (location.isTileLocationTotallyClearAndPlaceable(randomTile))
            {
                if (Game1.player.CombatLevel >= 10 && Game1.random.NextDouble() < .05)
                {
                    Skeleton skeleton = new Skeleton(randomTile * Game1.tileSize)
                    {
                        focusedOnFarmers = true
                    };
                    location.characters.Add(skeleton);
                }

                if (Game1.player.CombatLevel >= 8 && Game1.random.NextDouble() < 0.15)
                {
                    ShadowBrute shadowBrute = new ShadowBrute(randomTile * Game1.tileSize)
                    {
                        focusedOnFarmers = true
                    };
                    location.characters.Add(shadowBrute);
                }
                else if (Game1.random.NextDouble() < 0.65 && location.isTileLocationTotallyClearAndPlaceable(randomTile))
                {
                    RockGolem rockGolem = new RockGolem(randomTile * (float)Game1.tileSize, Game1.player.CombatLevel)
                    {
                        focusedOnFarmers = true
                    };
                    location.characters.Add(rockGolem);
                }
                else
                {
                    int mineLevel = 1;
                    if (Game1.player.CombatLevel >= 10)
                        mineLevel = 140;
                    else if (Game1.player.CombatLevel >= 8)
                        mineLevel = 100;
                    else if (Game1.player.CombatLevel >= 4)
                        mineLevel = 41;

                    GreenSlime greenSlime = new GreenSlime(randomTile * Game1.tileSize, mineLevel);
                    location.characters.Add(greenSlime);
                }
            }
        }

        public static void AdvanceCropOneStep(GameLocation loc, HoeDirt h)
        {
            


        }

        public static void AdvanceArbitrarySteps(GameLocation loc, HoeDirt h, int numDays)
        {
            for (int i = 0; i < numDays; i++)
                AdvanceCropOneStep(loc, h);
        }

        public static int CreateWeeds(GameLocation spawnLoc, int numOfWeeds)
        {
            if (spawnLoc == null)
                throw new Exception("The passed spawn location cannot be null!");

            int CreatedWeeds = 0;

            for (int i = 0; i <= numOfWeeds; i++)
            {
                //limit number of attempts per attempt to 10.
                int numberOfAttempts = 0;
                while (numberOfAttempts < 3)
                {
                    //get a random tile.
                    int xTile = Game1.random.Next(spawnLoc.map.DisplayWidth / Game1.tileSize);
                    int yTile = Game1.random.Next(spawnLoc.map.DisplayHeight / Game1.tileSize);
                    Vector2 randomVector = new Vector2((float)xTile, (float)yTile);
                    spawnLoc.objects.TryGetValue(randomVector, out SObject @object);

                    if (SDVUtilities.TileIsClearForSpawning(spawnLoc, randomVector, @object))
                    {
                        //for now, don't spawn in winter.
                        if (Game1.currentSeason != "winter")
                        {
                            //spawn the weed
                            spawnLoc.objects.Add(randomVector, new SObject(randomVector, GameLocation.getWeedForSeason(Game1.random, Game1.currentSeason), 1));
                            CreatedWeeds++;
                        }
                    }
                    numberOfAttempts++; // this might have been more useful INSIDE the while loop.
                }
            }
            return CreatedWeeds;
        }
    }
}
