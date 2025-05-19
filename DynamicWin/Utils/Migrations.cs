using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicWin.Main;

/*
*   Overview:
*    - Allows easier migration of existing widgets.
*    - If a widget exists in a user's configuration that has a naming convention changed, allow handling with this implementation.
*    
*   Author:                 Megan Park
*   GitHub:                 https://github.com/59xa
*   Implementation Date:    18 May 2025
*   Last Modified:          18 May 2025 18:19 KST (UTC+9)
*   
*/

namespace DynamicWin.Utils
{
    public static class Migrations
    {
        private static readonly List<WidgetMigration> SmallWidgetMigrations = new()
        {
            /*
             *      USAGE:
             *          new WidgetMigration
             *          {
             *              OldName = "DynamicWin.UI.Widgets.Small.Register{oldWidgetName},
             *              NewName = "DynamicWin.UI.Widgets.Small.Register{newWidgetName}
             *          }
             */

            new WidgetMigration
            {
                OldName = "DynamicWin.UI.Widgets.Small.RegisterSmallVisualizerWidget",
                NewName = "DynamicWin.UI.Widgets.Small.RegisterSmallVisualiserWidget"
            },
        };

        private static readonly List<WidgetMigration> BigWidgetMigrations = new()
        {
            /*
             *      USAGE:
             *          new WidgetMigration
             *          {
             *              OldName = "DynamicWin.UI.Widgets.Big.Register{oldWidgetName},
             *              NewName = "DynamicWin.UI.Widgets.Big.Register{newWidgetName}
             *          }
             */
        };

        /// <summary>
        /// Void function that allows migration of existing small widgets without removing them from where they are currently placed in the interface.
        /// </summary>
        public static void MakeSmallWidgetMigrations()
        {
            bool changed = false;

            foreach (var migration in SmallWidgetMigrations)
            {
                changed |= ReplaceInList(Settings.smallWidgetsLeft, migration, "SmallWidgets.Left");
                changed |= ReplaceInList(Settings.smallWidgetsMiddle, migration, "SmallWidgets.Middle");
                changed |= ReplaceInList(Settings.smallWidgetsRight, migration, "SmallWidgets.Right");
            }

            if (changed)
            {
                Debug.WriteLine("[MIGRATION] Small widget changes detected. Saving...");
                Settings.Save();
            }
            else
            {
                Debug.WriteLine("[MIGRATION] No small widget changes.");
            }
        }

        /// <summary>
        /// Helper to handle migrations depending on specified values given
        /// </summary>
        /// <param name="list">The list containing the widget</param>
        /// <param name="migration">Values that hold both legacy and new naming conventions</param>
        /// <param name="listName">The name of the list in string format</param>
        /// <returns></returns>
        private static bool ReplaceInList(List<string> list, WidgetMigration migration, string listName)
        {
            bool replaced = false;

            Debug.WriteLine($"[MIGRATION] --- Contents of {listName} ---");
            foreach (var item in list)
                Debug.WriteLine($"  {item}");

            // Iterates over the list to look for the legacy widget
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Contains(migration.OldName))
                {
                    // Ensure widget does not duplicate if user downgrades the application
                    if (list.Contains(migration.NewName))
                    {
                        Debug.WriteLine($"[MIGRATION] ({listName}) '{migration.NewName}' already exists. Removing duplicate '{list[i]}'");
                        list.RemoveAt(i);
                        i--;
                    }
                    else // Replace the legacy widget with the new widget
                    {
                        Debug.WriteLine($"[MIGRATION] ({listName}) Replacing:");
                        Debug.WriteLine($"  {list[i]}");
                        list[i] = list[i].Replace(migration.OldName, migration.NewName);
                        Debug.WriteLine($"  → {list[i]}");
                        replaced = true;
                    }
                }
            }

            // Display in debug that no changes were made
            if (!replaced)
            {
                Debug.WriteLine($"[MIGRATION] ({listName}) No match for: {migration.OldName}");
            }

            Debug.WriteLine($"[MIGRATION] --- End of {listName} ---\n");
            return replaced;
        }
    }

    // Class method to store both legacy and new naming conventions
    public class WidgetMigration
    {
        public string OldName { get; set; }
        public string NewName { get; set; }
    }
}
