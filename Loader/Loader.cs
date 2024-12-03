﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Loader;
using System.Threading.Tasks;

namespace Doorstop
{
    public class Entrypoint
    {


        public static void Start()
        {
            Logging.Info("Loaded CrystalMarble");

            foreach (string modFolder in Directory.GetDirectories("Mods"))
            {
                Logging.Info($"Loading mod directory: ");
                foreach (string file in Directory.GetFiles(modFolder))
                {
                    if (!file.EndsWith(".dll")) continue;
                    Logging.Info("Loading: " + file);
                    try
                    {
                        Type type = Enumerable.FirstOrDefault<Type>(Assembly.LoadFrom(file).GetTypes(), (Type t) => t.Name == "PatchEntryPoint");
                        if (type == null)
                        {
                            Logging.Info("Entrypoint type not found");
                        }
                        else
                        {
                            MethodInfo method = type.GetMethod("Start", BindingFlags.Static | BindingFlags.Public);
                            if (method == null)
                            {
                                Logging.Warn("Start method not found for " + modFolder);
                            }
                            else
                            {
                                Logging.Info("Invoking entrypoint");
                                method.Invoke(null, new object[0]);
                                Logging.Info($"{modFolder} successfully loaded");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Warn("Error while loading CrystalMarble: " + ex.ToString());
                    }
                }
                Logging.Info("Done loading mod");
            }
        }
    }
}
