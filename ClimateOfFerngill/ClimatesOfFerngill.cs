﻿using System;
using System.Reflection;

//3P
using NPack;
using StardewModdingAPI;
using StardewValley.Objects;
using StardewModdingAPI.Events;
using StardewValley.Locations;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using StardewValley;

namespace ClimateOfFerngill
{
    public class ClimatesOfFerngill : Mod
    {
        public ClimateConfig Config { get; private set; }
        int WeatherAtStartOfDay { get; set; }
        FerngillWeather CurrWeather { get; set; }
        FerngillWeather TomorrowWeather { get; set; }
        private int LastTime;
        private bool GameLoaded;
        MersenneTwister dice;
        private bool ModRan { get; set; } = false;

        private double windChance;
        private double stormChance;
        private double rainChance;

        //tv overloading
        private static FieldInfo Field = typeof(GameLocation).GetField("afterQuestion", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo TVChannel = typeof(TV).GetField("currentChannel", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo TVScreen = typeof(TV).GetField("screen", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo TVScreenOverlay = typeof(TV).GetField("screenOverlay", BindingFlags.Instance | BindingFlags.NonPublic);
        private static MethodInfo TVMethod = typeof(TV).GetMethod("getWeatherChannelOpening", BindingFlags.Instance | BindingFlags.NonPublic);
        private static MethodInfo TVMethodOverlay = typeof(TV).GetMethod("setWeatherOverlay", BindingFlags.Instance | BindingFlags.NonPublic);
        private static GameLocation.afterQuestionBehavior Callback;
        private static TV Target;

        /// <summary>Initialise the mod.</summary>
        /// <param name="helper">Provides methods for interacting with the mod directory, such as read/writing a config file or custom JSON files.</param>
        public override void Entry(IModHelper helper)
        {
            dice = new MersenneTwister();
            Config = helper.ReadConfig<ClimateConfig>();
            CurrWeather = new FerngillWeather();
            
            //set variables
            rainChance = 0;
            stormChance = 0;
            windChance = 0;

            //register debug commands
            Command.RegisterCommand("world_setweather", "Sets the world's weather | world_setweather <string>", new[] { "(String)<value> The new weather" }).CommandFired += this.HandleSetWeather;
            Command.RegisterCommand("world_settemp", "Sets the world's temperature | world_settemp <int>", new[] { "(Int32)<value> The new temperature" }).CommandFired += this.HandleSetTemperature;
            Command.RegisterCommand("player_faint", "Causes the player to faint.").CommandFired += this.FaintPlayer;

            //register event handlers
            TimeEvents.DayOfMonthChanged += TimeEvents_DayOfMonthChanged;
            MenuEvents.MenuChanged += MenuEvents_MenuChanged;
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            GameEvents.UpdateTick += GameEvents_UpdateTick;
            TimeEvents.TimeOfDayChanged += TimeEvents_TimeOfDayChanged;
            SaveEvents.BeforeSave += SaveEvents_BeforeSave;
        }

        private void SaveEvents_BeforeSave(object sender, EventArgs e)
        {
            //run all night time processing.

        }

        private void HandleSetTemperature(object sender, EventArgsCommand e)
        {
            if (e.Command.CalledArgs.Length > 0)
            {
                int temp = Convert.ToInt32(e.Command.CalledArgs[0]);
                CurrWeather.todayHigh = temp;
            }
        }

        private void FaintPlayer(object sender, EventArgsCommand e)
        {
            Game1.player.Stamina = 0;
            Game1.player.doEmote(36);
            Game1.farmerShouldPassOut = true;
        }

        private void HandleSetWeather(object sender, EventArgsCommand e)
        {
            if (e.Command.CalledArgs.Length > 0)
            {
                string newWeather = e.Command.CalledArgs[0];
                switch (newWeather)
                {
                    case "sunny":
                        Game1.isRaining = false;
                        Game1.isDebrisWeather = false;
                        Game1.isSnowing = false;
                        Game1.isLightning = false;
                        Monitor.Log("The weather is set to sunny", LogLevel.Info);
                        break;
                    case "stormy":
                        Game1.isRaining = true;
                        Game1.isDebrisWeather = false;
                        Game1.isSnowing = false;
                        Game1.isLightning = true;
                        Monitor.Log("The weather is set to stormy", LogLevel.Info);
                        break;
                    case "snowy":
                        Game1.isRaining = false;
                        Game1.isDebrisWeather = false;
                        Game1.isSnowing = true;
                        Game1.isLightning = false;
                        Monitor.Log("The weather is set to snowy", LogLevel.Info);
                        break;
                    case "rainy":
                        Game1.isRaining = true;
                        Game1.isDebrisWeather = false;
                        Game1.isSnowing = false;
                        Game1.isLightning = false;
                        Monitor.Log("The weather is set to rainy", LogLevel.Info);
                        break;
                    case "windy":
                        Game1.isRaining = false;
                        Game1.isDebrisWeather = true;
                        Game1.isSnowing = false;
                        Game1.isLightning = false;
                        Monitor.Log("The weather is set to windy", LogLevel.Info);
                        break;
                    default:
                        this.Monitor.Log("Invalid input for world_setweather", LogLevel.Info);
                        break;
                }
            }
        }

        private void TimeEvents_TimeOfDayChanged(object sender, EventArgsIntChanged e)
        {
            StormyWeather.CheckForStaminaPenalty(LogEvent, Config.tooMuchInfo);

            // have the stamina meter shake to make sure people are paying attention.
            if (Game1.player.Stamina <= 20f)
            {
                Game1.staminaShakeTimer = 1000;
                for (int i = 0; i < 4; i++)
                {
                    Game1.screenOverlayTempSprites.Add(new TemporaryAnimatedSprite(Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(366, 412, 5, 6), new Vector2((float)(Game1.random.Next(Game1.tileSize / 2) + Game1.viewport.Width - (48 + Game1.tileSize / 8)), (float)(Game1.viewport.Height - 224 - Game1.tileSize / 4 - (int)((double)(Game1.player.MaxStamina - 270) * 0.715))), false, 0.012f, Color.SkyBlue)
                    {
                        motion = new Vector2(-2f, -10f),
                        acceleration = new Vector2(0f, 0.5f),
                        local = true,
                        scale = (float)(Game1.pixelZoom + Game1.random.Next(-1, 0)),
                        delayBeforeAnimationStart = i * 30
                    });
                }
            }

            if (Game1.player.Stamina <= 0f)
            {
                Game1.player.doEmote(36);
                Game1.farmerShouldPassOut = true;
            }


        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {

            if (GameLoaded)
            {
                if (Game1.currentLocation.isOutdoors)
                    StormyWeather.TicksOutside++;

                if (Game1.timeOfDay != this.LastTime)
                    LastTime = Game1.timeOfDay;
                else
                    StormyWeather.TickPerSpan++;
            }
        }

        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            GameLoaded = true;
            StormyWeather.InitiateVariables(Config);
            UpdateWeather();
        }

        private void MenuEvents_MenuChanged(object sender, EventArgsClickableMenuChanged e)
        {
            TryHookTelevision();
        }

        public void TryHookTelevision()
        {
            if (Game1.currentLocation != null && Game1.currentLocation is DecoratableLocation && Game1.activeClickableMenu != null && Game1.activeClickableMenu is DialogueBox)
            {
                Callback = (GameLocation.afterQuestionBehavior)Field.GetValue(Game1.currentLocation);
                if (Callback != null && Callback.Target.GetType() == typeof(TV))
                {
                    Field.SetValue(Game1.currentLocation, new GameLocation.afterQuestionBehavior(InterceptCallback));
                    Target = (TV)Callback.Target;
                }
            }
        }
    
        public void InterceptCallback(Farmer who, string answer)
        {
            if (answer != "Weather")
            {
                Callback(who, answer);
                return;
            }
            TVChannel.SetValue(Target, 2);
            TVScreen.SetValue(Target, new TemporaryAnimatedSprite(Game1.mouseCursors, new Rectangle(413, 305, 42, 28), 150f, 2, 999999, Target.getScreenPosition(), false, false, (float)((double)(Target.boundingBox.Bottom - 1) / 10000.0 + 9.99999974737875E-06), 0.0f, Color.White, Target.getScreenSizeModifier(), 0.0f, 0.0f, 0.0f, false));
            Game1.drawObjectDialogue(Game1.parseText((string)TVMethod.Invoke(Target, null)));
            Game1.afterDialogues = NextScene;
        }
        public void NextScene()
        {
            TVScreen.SetValue(Target, new TemporaryAnimatedSprite(Game1.mouseCursors, new Rectangle(497, 305, 42, 28), 9999f, 1, 999999, Target.getScreenPosition(), false, false, (float)((double)(Target.boundingBox.Bottom - 1) / 10000.0 + 9.99999974737875E-06), 0.0f, Color.White, Target.getScreenSizeModifier(), 0.0f, 0.0f, 0.0f, false));
            Game1.drawObjectDialogue(Game1.parseText(GetWeatherForecast()));
            TVMethodOverlay.Invoke(Target, null);
            Game1.afterDialogues = Target.proceedToNextScene;
        }

        public string GetWeatherForecast()
        {
            string tvText = " ";

            //prevent null from being true.
            if (CurrWeather == null)
            {
                CurrWeather = new FerngillWeather();
                UpdateWeather();
            }

            int noLonger = VerifyValidTime(Config.NoLongerDisplayToday) ? Config.NoLongerDisplayToday : 1700;

            if (Game1.timeOfDay < noLonger) //don't display today's weather 
            {
                tvText = "The high for today is ";
                if (!Config.DisplaySecondScale)
                    tvText += WeatherHelper.DisplayTemperature(CurrWeather.todayHigh, Config.TempGauge) + ", with the low being " + WeatherHelper.DisplayTemperature(CurrWeather.todayLow, Config.TempGauge) + ". ";
                else
                {
                    tvText += WeatherHelper.DisplayTemperature(CurrWeather.todayHigh, Config.TempGauge) +  "(  " + WeatherHelper.DisplayTemperature(CurrWeather.todayHigh, Config.SecondScaleGauge) + " ), with the low being " + WeatherHelper.DisplayTemperature(CurrWeather.todayLow, Config.TempGauge) + " ( " + WeatherHelper.DisplayTemperature(CurrWeather.todayLow, Config.SecondScaleGauge) + "). ";
                }


                //temp warnings 
                if (CurrWeather.todayHigh > Config.HeatwaveWarning && Game1.timeOfDay < 1900)
                    tvText = tvText + "It will be unusually hot outside. Stay hydrated and be careful not to stay too long in the sun. ";
                if (CurrWeather.todayHigh < -5)
                    tvText = tvText + "There's an extreme cold snap passing through. Stay warm. ";

                //today weather
                tvText = tvText + WeatherHelper.GetWeatherDesc(dice, WeatherHelper.GetTodayWeather());

                //get WeatherForTommorow and set text
                tvText = tvText + "Tommorow, ";
            }

            if (CurrWeather.todayHigh > Config.HeatwaveWarning && Game1.timeOfDay < 1900)
                tvText = "It will be unusually hot outside. Stay hydrated and be careful not to stay too long in the sun. ";
            if (CurrWeather.todayHigh < -5)
                tvText = "There's an extreme cold snap passing through. Stay warm. ";

            //tommorow weather
            tvText = tvText + WeatherHelper.GetWeatherDesc(dice, (SDVWeather)Game1.weatherForTomorrow);

            return tvText;
        }

        private bool VerifyValidTime(int time)
        {
            //basic bounds first
            if (time >= 0600 && time <= 2600)
                return false;
            if ((time % 100) > 50)
                return false;

            return true;
        }
        
        private void LogEvent(string msg, bool important=false)
        {
            if (!important)
                Monitor.Log(msg, LogLevel.Trace);
            else
                Monitor.Log(msg, LogLevel.Info);            
        }

        public void checkForDangerousWeather(bool hud = true)
        {
            if (CurrWeather.status == FerngillWeather.BLIZZARD) {
                Game1.hudMessages.Add(new HUDMessage("There's a dangerous blizard out today. Be careful!"));
                return;
            }

            if (CurrWeather.status == FerngillWeather.HEATWAVE)
            {
                Game1.hudMessages.Add(new HUDMessage("A massive heatwave is sweeping the valley. Stay hydrated!"));
                return;
            }
        }

        public void EarlyFrost()
        {
            //this function is called if it's too cold to grow crops. Must be enabled.
            
        }

        public void TimeEvents_DayOfMonthChanged(object sender, EventArgsIntChanged e)
        {
            UpdateWeather();
        }

        void UpdateWeather(){
            //sanity checks.
            
            #region WeatherChecks
            //sanity check - wedding
            if (Game1.weatherForTomorrow == Game1.weather_wedding)
            {
                LogEvent("There is no Alanis Morissetting here. Enjoy your wedding.");
                return;
            }

            //sanity check - festival
            if (Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.currentSeason))
            {
                Game1.weatherForTomorrow = Game1.weather_festival;
                return;
            }
            //catch call.
            if (WeatherAtStartOfDay != Game1.weatherForTomorrow && ModRan)
            {
                if (WeatherHelper.WeatherForceDay(Game1.currentSeason, Game1.dayOfMonth + 1, Game1.year))
                {
                    if (Config.tooMuchInfo) LogEvent("The game will force weather. Aborting.");
                    return;
                }

                if (Config.tooMuchInfo)
                    LogEvent("Change detected. Debug Info: DAY " + Game1.dayOfMonth + " Season: " + Game1.currentSeason + " with prev weather: " + WeatherHelper.DescWeather(WeatherAtStartOfDay) + " and new weather: " + WeatherHelper.DescWeather(Game1.weatherForTomorrow) + ". Aborting.");

                return;
            }

            //TV forces.
            bool forceTomorrow = fixTV();
            if (forceTomorrow)
            {
                LogEvent("Tommorow, there will be forced weather.");
                return;
            }
            #endregion

            /* Since we have multiple different climates, maybe a more .. clearer? method */
            CurrWeather.Reset();

            LogEvent("CurrWeather is " + CurrWeather, true);
            //reset the variables
            rainChance = stormChance = windChance = 0;

            //set the variables
            if (Game1.currentSeason == "spring")
                HandleSpringWeather();
            else if (Game1.currentSeason == "summer")
                HandleSummerWeather();
            else if (Game1.currentSeason == "fall")
                HandleAutumnWeather();
            else if (Game1.currentSeason == "winter")
                HandleWinterWeather();

            //handle calcs here for odds.
            double chance = dice.NextDouble();
            if (Config.tooMuchInfo)
                LogEvent("Rain Chance is: " + rainChance + " with the rng being " + chance);

            //override for the first spring.
            if (!CanWeStorm())
                stormChance = 0;

            //sequence - rain (Storm), wind, sun
            //this also contains the notes - certain seasons don't have certain weathers.
            if (chance < rainChance || Game1.currentSeason != "winter")
            {
                chance = dice.NextDouble();
                if (chance < stormChance && stormChance != 0)
                {
                    if (Config.tooMuchInfo) LogEvent("Storm is selected, with roll " + chance + " and TP " + stormChance);
                    Game1.weatherForTomorrow = Game1.weather_lightning;
                }
                else
                {
                    Game1.weatherForTomorrow = Game1.weather_rain;
                    if (Config.tooMuchInfo) LogEvent("Raining selected");
                }
            }
            else if (Game1.currentSeason != "winter")
            {
                if (chance < (windChance + rainChance) && (Game1.currentSeason == "spring" || Game1.currentSeason == "fall") && windChance != 0)
                {
                    Game1.weatherForTomorrow = Game1.weather_debris;
                    if (Config.tooMuchInfo) LogEvent("It's windy today, with roll " + chance + " and wind odds " + windChance);
                }
                else
                    Game1.weatherForTomorrow = Game1.weather_sunny;
            }

            if (Game1.currentSeason == "winter")
            {
                if (chance < rainChance)
                    Game1.weatherForTomorrow = Game1.weather_snow;
                else
                    Game1.weatherForTomorrow = Game1.weather_sunny;
            }

            if (Game1.dayOfMonth == 28 && Game1.currentSeason == "fall" && Config.AllowSnowOnFall28)
            {
                CurrWeather.todayHigh = 2;
                CurrWeather.todayLow = -1;
                Game1.weatherForTomorrow = Game1.weather_snow; //it now snows on Fall 28.
            }

            WeatherAtStartOfDay = Game1.weatherForTomorrow;
            Game1.chanceToRainTomorrow = rainChance; //set for various events.
            LogEvent("We've set the weather for tommorow . It is: " + WeatherHelper.DescWeather(Game1.weatherForTomorrow));
            ModRan = true;
        }

        private void HandleSpringWeather()
        {
            stormChance = .15;
            windChance = .25;
            rainChance = .3 + (Game1.dayOfMonth * .0278);

            CurrWeather.todayHigh = dice.Next(1, 8) + 8;
            CurrWeather.GetLowFromHigh(dice.Next(1, 3) + 3);

            if (Game1.dayOfMonth > 9 && Game1.dayOfMonth < 19)
            {
                stormChance = .2;
                windChance = .15 + (Game1.dayOfMonth * .01);
                rainChance = .2 + (Game1.dayOfMonth * .01);            

                CurrWeather.todayHigh = dice.Next(1, 6) + 14;
                CurrWeather.GetLowFromHigh(dice.Next(1, 3) + 3);
            }

            if (Game1.dayOfMonth > 18)
            {
                stormChance = .3;
                windChance = .05 + (Game1.dayOfMonth * .01);
                rainChance = .2;

                CurrWeather.todayHigh = dice.Next(1, 6) + 20;
                CurrWeather.GetLowFromHigh(dice.Next(1, 3) + 3);
            }

            //override the rain chance - it's the same no matter what day, so pulling it out of the if statements.
            switch (Config.ClimateType)
            {
                case "arid":
                    CurrWeather.AlterTemps(5);
                    rainChance = .25;
                    break;
                case "dry":
                    rainChance = .3;
                    break;
                case "wet":
                    rainChance = rainChance + .05;
                    break;
                case "monsoon":
                    rainChance = .95;
                    break;
                default:
                    break;
            }
        }

        private void HandleSummerWeather()
        {
            if (Game1.dayOfMonth < 10)
            {
                //rain, snow, windy chances
                stormChance = .45;
                windChance = 0; //cannot wind during summer
                rainChance = .15;

                CurrWeather.todayHigh = dice.Next(1, 8) + 26;
                CurrWeather.GetLowFromHigh(dice.Next(1, 3) + 3);
            }
            if (Game1.dayOfMonth > 9 && Game1.dayOfMonth < 19)
            {
                //rain, snow, windy chances
                stormChance = .6;
                windChance = 0; //cannot wind during summer
                rainChance = .15;

                CurrWeather.todayHigh = 22 + (int)Math.Floor(Game1.dayOfMonth * 1.56) + dice.Next(0, 3);
                CurrWeather.GetLowFromHigh(dice.Next(1, 3) + 3);
            }
            if (Game1.dayOfMonth > 18)
            {
                //temperature
                CurrWeather.todayHigh = 22 + (int)Math.Floor(Game1.dayOfMonth * 1.56) + dice.Next(0, 3);
                CurrWeather.GetLowFromHigh(dice.Next(1, 3) + 3);

                stormChance = .45;
                windChance = 0; 
                rainChance = .3;
            }

            //summer alterations
            CurrWeather.AlterTemps(dice.Next(0, 6));
            if (Config.ClimateType == "arid" || Config.ClimateType == "monsoon")  CurrWeather.AlterTemps(6);

            switch (Config.ClimateType)
            {
                case "arid":
                    rainChance = .05;
                    break;
                case "dry":
                    rainChance = .20;
                    break;
                case "wet":
                    rainChance = rainChance + .05;
                    break;
                case "monsoon":
                    rainChance = rainChance - .1;
                    stormChance = .8;
                    break;
                default:
                    break;
            }
        }

        private void HandleAutumnWeather()
        {
            stormChance = .33;
            CurrWeather.todayHigh = 22 - (int)Math.Floor(Game1.dayOfMonth * .667) + dice.Next(0, 2); 

            if (Game1.dayOfMonth < 10)
            {
                windChance = 0 + (Game1.dayOfMonth * .044);
                rainChance = .3 + (Game1.dayOfMonth * .01111);
                CurrWeather.GetLowFromHigh(dice.Next(1, 6) + 4);
            }

            if (Game1.dayOfMonth > 9 && Game1.dayOfMonth < 19)
            {
                windChance = .4 + (Game1.dayOfMonth * .022);
                rainChance = 1 - windChance;
                CurrWeather.GetLowFromHigh(dice.Next(1, 6) + 3);
            }

            if (Game1.dayOfMonth > 18)
            {
                windChance = .1 + Game1.dayOfMonth * .044; 
                rainChance = .5;
                if (!Config.SetLowCap)
                    CurrWeather.GetLowFromHigh(dice.Next(1, 3) + 3);
                else
                    CurrWeather.GetLowFromHigh(dice.Next(1, 3) + 3, Config.LowCap);
            }


            //climate changes to the rain.
            switch (Config.ClimateType)
            {
                case "arid":
                    rainChance = .05;
                    CurrWeather.AlterTemps(2);
                    if (Game1.dayOfMonth < 10 && Game1.dayOfMonth > 18) windChance = .3;
                    break;
                case "dry":
                    if (Game1.dayOfMonth > 10) rainChance = .25;
                    else rainChance = .2;
                    windChance = .3;
                    break;
                case "wet":
                    rainChance = rainChance + .05;
                    break;
                case "monsoon":
                    if (Game1.dayOfMonth > 18) rainChance = .9;
                    else rainChance = .1 + (Game1.dayOfMonth * .08889);
                    break;
                default:
                    break;
            }

    }

        private void HandleWinterWeather()
        {
            stormChance = 0;
            windChance = 0;
            rainChance = .6;

            if (Game1.dayOfMonth < 10)
            {
                CurrWeather.todayHigh = -2 + (int)Math.Floor(Game1.dayOfMonth * .889) + dice.Next(0, 3);
                CurrWeather.GetLowFromHigh(dice.Next(1, 4));
            }

            if (Game1.dayOfMonth > 9 && Game1.dayOfMonth < 19)
            {
                CurrWeather.todayHigh = -12 + (int)Math.Floor(Game1.dayOfMonth * 1.111) + dice.Next(0, 3);
                CurrWeather.GetLowFromHigh(dice.Next(1, 4));
                rainChance = .75;
            }

            if (Game1.dayOfMonth > 18)
            {
                CurrWeather.todayHigh = -12 + (int)Math.Floor(Game1.dayOfMonth * 1.222) + dice.Next(0, 3);
                CurrWeather.GetLowFromHigh(dice.Next(1, 4));
            } 

            switch (Config.ClimateType)
            {
                case "arid":
                    rainChance = .25;
                    break;
                case "dry":
                    rainChance = .30;
                    break;
                case "wet":
                    rainChance = rainChance + .05;
                    break;
                case "monsoon":
                    rainChance = 1;
                    break;
                default:
                    break;
            }

        }

        private bool CanWeStorm()
        {
            if (Game1.year == 1 && Game1.currentSeason == "spring") return Config.AllowStormsFirstSpring;
            else return true;
        }

        private bool fixTV()
        {
            if (Game1.currentSeason == "spring" && Game1.year == 1 && Game1.dayOfMonth == 1)
            {
                Game1.weatherForTomorrow = Game1.weather_sunny;
                return true;
            }

            if (Game1.currentSeason == "spring" && Game1.year == 1 && Game1.dayOfMonth == 3)
            {
                Game1.weatherForTomorrow = Game1.weather_rain;
                return true;
            }

            if (Game1.currentSeason == "spring" && Game1.year == 1 && Game1.dayOfMonth == 4)
            {
                Game1.weatherForTomorrow = Game1.weather_sunny;
                return true;
            }

            if (Game1.currentSeason == "summer" && Game1.dayOfMonth == 12)
            {
                Game1.weatherForTomorrow = Game1.weather_lightning;
                return true;
            }

            if (Game1.currentSeason == "summer" && Game1.dayOfMonth == 25)
            {
                Game1.weatherForTomorrow = Game1.weather_lightning;
                return true;
            }

            return false;
        }

    }    
}

